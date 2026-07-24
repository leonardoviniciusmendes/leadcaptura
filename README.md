# LeadEngine

## Endurecimento Google Ads para conta de teste

A publicacao controlada usa `GoogleAdsPlanoPublicacao` valido como fonte unica e foi reforcada para validacao em conta de teste real. `GET /api/googleads/ambiente` informa modo, CustomerId mascarado, pendencias e se a publicacao real esta permitida. `POST /api/googleads/publicacoes/preview/{previewId}/dry-run` monta as operacoes tipadas e nao chama o Google Ads. `validateOnly=true` e `partialFailure=false` usam a mesma lista tipada da publicacao.

Nesta fase, publicacao real exige `GoogleAds.EnableRealPublishing=true`, `GoogleAds.UseTestAccount=true` e `GoogleAds.TestCustomerId` igual a conta selecionada. Publicacao em producao continua bloqueada. As operacoes sao montadas por `GoogleAdsTypedOperationFactory` com tipos do SDK `Google.Ads.GoogleAds 26.0.1`; o transporte REST fica isolado em `GoogleAdsRestMutateTransport`.

## Callback OAuth Google Ads

Redirect URI local:

```text
http://localhost:5173/leadcaptura/configuracoes?googleAdsCallback=1
```

Quando o Google retorna `code` e `state`, o frontend chama:

```http
POST /api/googleads/oauth/callback
```

```json
{
  "code": "codigo-retornado-pelo-google",
  "state": "state-retornado-pelo-google"
}
```

O backend valida `state` persistido com expiracao e uso unico, troca o code por tokens, protege os tokens e grava as contas encontradas. A URL e limpa para `/leadcaptura/configuracoes` apos sucesso ou erro. Apos OAuth sem conta padrao selecionada, o status esperado e:

```json
{
  "conectado": true,
  "status": "Conectado sem conta padrao",
  "contaPadraoId": null,
  "customerId": null,
  "nome": null
}
```

Assistente para gerar campanhas de Google Ads para planos de saúde sem exigir conhecimento técnico de mídia paga.

O usuário informa:

- público;
- cidade;
- estado;
- bairro ou região, opcional;
- operadora;
- orçamento diário;
- objetivo ou observação, opcional.

O sistema gera a campanha, salva no MySQL e exibe o resultado no painel. A geração pode usar o provider `Fake` para desenvolvimento/testes ou `OpenRouter` para IA real.

## Fora do escopo atual

Não implementar nesta fase:

- login;
- cadastro de usuários;
- cadastro de corretor;
- múltiplas empresas;
- cobrança;
- CRM;
- propostas;
- contratos;
- Google Ads real em producao;
- landing pública;
- leads;
- métricas.

Os módulos antigos de captura de leads ainda existem no código, mas não são o fluxo principal do produto nesta etapa.

## Arquitetura

```text
src/
  LeadEngine.Api             Controllers, Swagger, middlewares, CORS e health check
  LeadEngine.Application     DTOs, validações, interfaces, prompt, parser e casos de uso
  LeadEngine.Domain          Entidades e enums de domínio
  LeadEngine.Infrastructure  EF Core, MySQL, repositories, OpenRouter, migrations e integrações
  LeadEngine.Web             Vue 3, TypeScript e telas do painel
tests/
  LeadEngine.Application.Tests
```

Mais detalhes em `docs/architecture.md`.

## Fluxo implementado

```text
Briefing simples
-> ICampaignGenerationService
-> FakeCampaignGenerationService ou OpenRouterCampaignGenerationService
-> Campanha persistida no MySQL
-> Consulta por API
-> Exibição no frontend
```

## Endpoints de campanhas

```http
POST /api/campanhas/gerar
GET  /api/campanhas
GET  /api/campanhas/{id}
GET  /api/campanhas/{id}/revisao
PUT  /api/campanhas/{id}/revisao
POST /api/campanhas/{id}/regenerar
POST /api/campanhas/{id}/aprovar
GET  /api/campanhas/{id}/historico-revisoes
POST /api/campanhas/{id}/publicar
POST /api/campanhas/{id}/despublicar
GET  /api/campanhas/{id}/publicacao
GET  /api/campanhas/{id}/leads
GET  /api/publico/campanhas/{slug}
POST /api/publico/campanhas/{slug}/leads
GET  /api/leads
GET  /api/leads/{id}
GET  /api/configuracoes
GET  /api/configuracoes/{categoria}
PUT  /api/configuracoes/{categoria}
POST /api/configuracoes/{categoria}/testar
GET  /api/configuracoes/status
GET  /api/configuracoes/historico
```

Endpoint disponível somente em Development:

```http
POST /api/desenvolvimento/openrouter/testar
```

## Exemplo de requisição

```json
{
  "tipoPublico": "Familia",
  "cidade": "Rio de Janeiro",
  "estado": "RJ",
  "regiao": "Barra da Tijuca",
  "operadora": "Amil",
  "operadoraOutra": null,
  "orcamentoDiario": 20,
  "objetivo": "Captar famílias interessadas em cotação consultiva."
}
```

## Exemplo de resposta

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "nome": "Plano Familiar Amil - Barra da Tijuca",
  "tipoPublico": "Familia",
  "cidade": "Rio de Janeiro",
  "estado": "RJ",
  "regiao": "Barra da Tijuca",
  "operadora": "Amil",
  "orcamentoDiario": 20,
  "status": "Gerada",
  "tituloLandingPage": "Plano de Saúde Familiar em Barra da Tijuca",
  "subtituloLandingPage": "Compare opções para seu perfil com atendimento personalizado.",
  "textoBotao": "Solicitar cotação pelo WhatsApp",
  "mensagemWhatsApp": "Olá, gostaria de uma cotação de plano de saúde familiar em Barra da Tijuca.",
  "slug": "plano-familiar-amil-barra-da-tijuca",
  "beneficios": ["Atendimento consultivo", "Cotação conforme perfil"],
  "perguntasFrequentes": [
    {
      "pergunta": "O valor é fixo?",
      "resposta": "Não. Os preços variam por idade, região e tipo de contratação."
    }
  ],
  "palavrasChave": ["plano de saúde familiar"],
  "palavrasChaveNegativas": ["emprego", "boleto"],
  "titulosAnuncios": ["Plano de saúde"],
  "descricoesAnuncios": ["Receba atendimento para comparar opções conforme seu perfil."],
  "providerIa": "OpenRouter",
  "modeloIa": "openai/gpt-4o-mini",
  "duracaoGeracaoMs": 1200
}
```

## Revisão comercial

Depois da geração, o usuário pode revisar a campanha antes de uma futura publicação no Google Ads. Esta etapa não publica, não ativa e não integra OAuth Google.

Fluxo atual:

```text
Campanha gerada
-> revisão dos conteúdos
-> edição manual
-> regeneração parcial com OpenRouter
-> aprovação
-> status Revisada
```

Campos editáveis em `PUT /api/campanhas/{id}/revisao`:

```json
{
  "nome": "Plano familiar RJ",
  "tituloLandingPage": "Plano de saúde para famílias no RJ",
  "subtituloLandingPage": "Compare opções conforme seu perfil.",
  "textoBotao": "Falar no WhatsApp",
  "mensagemWhatsApp": "Olá, quero comparar opções de plano de saúde.",
  "beneficios": ["Atendimento consultivo", "Comparação por perfil", "Suporte na escolha"],
  "perguntasFrequentes": [
    { "pergunta": "O valor é fixo?", "resposta": "Não. Valores variam por idade, região e contratação." },
    { "pergunta": "A rede é garantida?", "resposta": "Não. Rede e cobertura dependem do plano." },
    { "pergunta": "Existe carência?", "resposta": "Carência depende das regras da operadora." }
  ],
  "palavrasChave": ["plano de saúde familiar", "cotação plano saúde", "plano saúde rj"],
  "palavrasChaveNegativas": ["emprego", "boleto", "login"],
  "titulosAnuncios": ["Plano Saúde RJ", "Cotação Familiar", "Fale no WhatsApp", "Compare Planos", "Atendimento RJ", "Plano por Perfil", "Consultoria Local", "Solicite Cotação"],
  "descricoesAnuncios": ["Compare opções conforme seu perfil.", "Atendimento consultivo para planos de saúde.", "Solicite contato pelo WhatsApp."]
}
```

Campos não editáveis pelo frontend: `id`, `dataCriacao`, `providerIa`, `modeloIa`, `duracaoGeracaoMs`, `status` e `slug`.

Regeneração parcial:

```http
POST /api/campanhas/00000000-0000-0000-0000-000000000000/regenerar
```

```json
{
  "secao": "TitulosAnuncios",
  "instrucaoAdicional": "Use uma abordagem mais voltada para famílias."
}
```

Seções aceitas: `Nome`, `LandingPage`, `MensagemWhatsApp`, `Beneficios`, `PerguntasFrequentes`, `PalavrasChave`, `PalavrasChaveNegativas`, `TitulosAnuncios`, `DescricoesAnuncios`.

Aprovação:

```http
POST /api/campanhas/00000000-0000-0000-0000-000000000000/aprovar
```

Histórico:

```http
GET /api/campanhas/00000000-0000-0000-0000-000000000000/historico-revisoes
```

Retorna data, seção, origem, resumo, provider e modelo. Conteúdo completo, prompts e chaves não são retornados.

## Landing pública e captura de leads

Somente campanhas com status `Revisada` podem ser publicadas. Ao publicar, a landing fica ativa em `/lp/{slug}`.

Exemplo de URL pública:

```text
/lp/plano-familiar-amil-barra-da-tijuca
```

Exemplo de publicação:

```http
POST /api/campanhas/00000000-0000-0000-0000-000000000000/publicar
```

```json
{
  "status": "Revisada",
  "publicada": true,
  "ativo": true,
  "slugPublico": "plano-familiar-amil-barra-da-tijuca",
  "urlPublica": "/lp/plano-familiar-amil-barra-da-tijuca"
}
```

Se uma campanha publicada for editada ou regenerada, ela é despublicada automaticamente, volta para `Gerada` e exige nova aprovação/publicação. Esta é a regra mais segura para o MVP.

Consulta pública:

```http
GET /api/publico/campanhas/plano-familiar-amil-barra-da-tijuca
```

Captura de lead:

```http
POST /api/publico/campanhas/plano-familiar-amil-barra-da-tijuca/leads
```

```json
{
  "nome": "Maria Silva",
  "telefone": "21999999999",
  "email": "maria@email.com",
  "cidade": "Rio de Janeiro",
  "estado": "RJ",
  "quantidadeVidas": 3,
  "tipoContratacao": "Familiar",
  "observacao": "Quero cobertura nacional.",
  "consentimento": true,
  "formOpenedAt": 1784910000000,
  "utmSource": "google",
  "utmMedium": "cpc",
  "utmCampaign": "familia-rj",
  "utmTerm": "plano familiar",
  "utmContent": "anuncio-1",
  "gclid": "valor-opcional",
  "fbclid": null
}
```

```json
{
  "leadId": "00000000-0000-0000-0000-000000000000",
  "mensagem": "Lead registrado com sucesso.",
  "whatsAppUrl": "https://wa.me/5511999999999?text=..."
}
```

O endpoint público não retorna provider, modelo, duração, erro técnico, histórico ou dados administrativos. Se o mesmo telefone enviar novamente na mesma campanha dentro da janela configurada, a API retorna sucesso controlado e não cria novo lead.

Proteção anti-spam do MVP:

- rate limit por IP;
- honeypot `website`;
- tempo mínimo entre abrir e enviar o formulário;
- User-Agent obrigatório no contexto da requisição.

## Configurações e integrações

O módulo de configurações permite administrar pela interface valores operacionais que antes dependiam apenas de `appsettings` e variáveis de ambiente.

Categorias suportadas:

```text
OpenRouter
CampaignGeneration
WhatsApp
LeadCapture
ExternalLeadApi
Application
Landing
GoogleAds
```

Prioridade da configuração efetiva:

```text
1. Banco
2. Variável de ambiente
3. AppSettings
4. Padrão seguro
```

Leitura:

```http
GET /api/configuracoes/OpenRouter
```

Resposta para segredo:

```json
{
  "chave": "ApiKey",
  "valor": null,
  "sensivel": true,
  "configurado": true,
  "origem": "Banco"
}
```

Atualização:

```http
PUT /api/configuracoes/OpenRouter
```

```json
{
  "apiKey": "nova-chave",
  "model": "openai/gpt-4o-mini",
  "baseUrl": "https://openrouter.ai/api/v1",
  "timeoutSeconds": 60,
  "maxRetries": 2,
  "temperature": 0.3
}
```

Se `apiKey` não for enviada, o segredo atual é mantido. Para remover:

```json
{
  "removerApiKey": true
}
```

Teste:

```http
POST /api/configuracoes/WhatsApp/testar
```

Retorna uma URL de exemplo e não envia mensagem.

Valores sensíveis nesta etapa:

- `OpenRouter.ApiKey`;
- `ExternalLeadApi.ApiKey`;
- `GoogleAds.ClientSecret`;
- `GoogleAds.DeveloperToken`;
- tokens OAuth do Google Ads armazenados em `GoogleAdsContas`;
- futuros tokens, senhas e client secrets.

Segredos são protegidos com ASP.NET Core Data Protection antes de persistir. Eles não são retornados pela API nem gravados em histórico.

## Google Ads

O módulo Google Ads prepara a infraestrutura de conexão, sem criar campanhas, anúncios ou publicações.

Endpoints:

```http
GET /api/googleads/status
GET /api/googleads/ambiente
GET /api/googleads/auth-url
POST /api/googleads/oauth/callback
GET /api/googleads/contas
POST /api/googleads/contas/{id}/selecionar
POST /api/googleads/testar
```

Configurações:

```json
{
  "GoogleAds": {
    "ClientId": "",
    "ClientSecret": "",
    "DeveloperToken": "",
    "LoginCustomerId": "",
    "RedirectUri": "http://localhost:5173/leadcaptura/configuracoes?googleAdsCallback=1",
    "AuthEndpoint": "https://accounts.google.com/o/oauth2/v2/auth",
    "TokenEndpoint": "https://oauth2.googleapis.com/token",
    "UserInfoEndpoint": "https://openidconnect.googleapis.com/v1/userinfo",
    "ApiBaseUrl": "https://googleads.googleapis.com/v22",
    "Scopes": "https://www.googleapis.com/auth/adwords openid email profile",
    "DefaultDailyBudget": 10.00,
    "DefaultCountryCode": "BR",
    "DefaultLanguageCode": "pt",
    "DefaultCurrencyCode": "BRL",
    "DefaultKeywordMatchType": "Phrase",
    "DefaultCampaignStatus": "PAUSED",
    "EnableBroadMatch": false,
    "DefaultCpcBid": "",
    "DefaultBiddingStrategy": "ManualCpc",
    "ApiTimeoutSeconds": 60,
    "EnableRealPublishing": false,
    "UseTestAccount": false,
    "TestCustomerId": ""
  }
}
```

Fluxo:

1. Configure `ClientId`, `ClientSecret`, `DeveloperToken` e `RedirectUri`.
2. Clique em `Conectar conta Google` na tela de Configurações.
3. O callback troca o `code` por tokens e lista contas acessíveis.
4. Selecione a conta padrão.
5. Use `POST /api/googleads/testar` para validar a conexão.

`AccessToken` e `RefreshToken` são protegidos por `ISecretProtector`. A API nunca retorna tokens.

## Pré-publicação Google Ads

O preview técnico transforma uma campanha revisada e com landing publicada em uma estrutura compatível com Google Ads, sem chamar endpoints de criação.

Endpoints:

```http
POST /api/googleads/preview/campanhas/{campanhaId}
GET /api/googleads/preview/{id}
GET /api/googleads/preview/campanhas/{campanhaId}
POST /api/googleads/preview/{id}/validar
PUT /api/googleads/preview/{id}
POST /api/googleads/preview/{id}/sugerir-ajustes
POST /api/googleads/preview/{id}/aplicar-sugestao
GET /api/googleads/preview/{id}/payload
DELETE /api/googleads/preview/{id}
```

Exemplo de payload técnico:

```json
{
  "campaign": {
    "name": "Plano de Saúde Empresarial RJ",
    "advertisingChannelType": "SEARCH",
    "status": "PAUSED",
    "includeDisplayNetwork": false
  },
  "budget": {
    "amount": 10.00,
    "amountMicros": 10000000,
    "deliveryMethod": "STANDARD",
    "shared": false
  },
  "adGroups": [
    {
      "name": "Plano Empresarial",
      "keywords": [
        { "text": "plano de saúde empresarial", "matchType": "PHRASE" }
      ],
      "responsiveSearchAd": {
        "headlines": [],
        "descriptions": [],
        "finalUrls": []
      }
    }
  ]
}
```

Validações bloqueantes:

- campanha inexistente ou não aprovada;
- landing não publicada ou URL pública inválida;
- conta Google Ads padrão ausente;
- configuração Google Ads inválida;
- orçamento menor ou igual a zero;
- moeda ausente;
- menos de 3 ou mais de 15 headlines;
- menos de 2 ou mais de 4 descriptions;
- headline acima de 30 caracteres;
- description acima de 90 caracteres;
- ausência de keywords.

Avisos:

- orçamento baixo;
- poucas keywords;
- ausência de negativas;
- headline repetida;
- descrição semelhante;
- URL fora do domínio público configurado;
- ausência de keyword Exact;
- CPC não configurado.

O preview usa hash SHA-256 dos campos relevantes da campanha para detectar conteúdo desatualizado. Ajustes por IA retornam apenas sugestões e não substituem texto automaticamente.

## Publicação controlada Google Ads

A publicação usa exclusivamente um `GoogleAdsPlanoPublicacao` válido como fonte. A campanha, o grupo e o anúncio são sempre criados como `PAUSED`; o sistema nunca ativa campanhas automaticamente.

SDK oficial referenciado:

```text
Google.Ads.GoogleAds 26.0.1
```

Endpoints:

```http
POST /api/googleads/publicacoes/preview/{previewId}/validar-remotamente
POST /api/googleads/publicacoes/preview/{previewId}/dry-run
POST /api/googleads/publicacoes/preview/{previewId}/preparar
POST /api/googleads/publicacoes/preview/{previewId}/publicar
POST /api/googleads/publicacoes/{id}/reconciliar
GET /api/googleads/publicacoes/{id}
GET /api/googleads/publicacoes/{id}/historico
GET /api/googleads/publicacoes/campanha/{campanhaId}
GET /api/googleads/publicacoes
```

Validação remota:

```http
POST /api/googleads/publicacoes/preview/{previewId}/validar-remotamente
```

Executa as mesmas operações planejadas com `validateOnly=true`, persiste `requestId`, erros traduzidos e não cria recursos.

Preparação:

```json
{
  "nome": "Plano de Saúde Empresarial RJ",
  "customerIdMascarado": "12****90",
  "orcamentoDiario": 10.0,
  "statusPlanejado": "PAUSED",
  "validacaoRemota": true,
  "confirmationToken": "uso-unico"
}
```

Publicação:

```json
{
  "confirmationToken": "uso-unico",
  "confirmarCriacaoPausada": true
}
```

Resultado simplificado:

```json
{
  "status": "Publicada",
  "requestIdPublicacao": "abc",
  "recursos": [
    {
      "tipoRecurso": "Campaign",
      "resourceName": "customers/1234567890/campaigns/222",
      "status": "PAUSED"
    }
  ]
}
```

Idempotência:

- índice único por `GoogleAdsPlanoPublicacaoId`, `PreviewVersao` e `PreviewHash`;
- publicação já `Publicada` retorna o registro existente;
- publicação `Publicando` retorna conflito;
- publicação parcial exige reconciliação.

Conta de teste:

- `GoogleAds.UseTestAccount=true`;
- `GoogleAds.TestCustomerId` obrigatório;
- publicação é bloqueada quando o customer selecionado difere do customer de teste.

Nenhum endpoint retorna access token, refresh token, client secret ou developer token.

## Configuração

Provider padrão:

```json
{
  "CampaignGeneration": {
    "Provider": "Fake",
    "FallbackToFake": false
  }
}
```

Valores suportados:

```text
Fake
OpenRouter
```

OpenRouter:

```json
{
  "OpenRouter": {
    "BaseUrl": "https://openrouter.ai/api/v1",
    "ApiKey": "",
    "Model": "",
    "TimeoutSeconds": 60,
    "MaxRetries": 2,
    "Temperature": 0.3
  }
}
```

WhatsApp e captura pública:

```json
{
  "WhatsApp": {
    "Numero": "",
    "MensagemPadrao": "Gostaria de receber uma cotacao."
  },
  "LeadCapture": {
    "ConsentVersion": "1.0",
    "MinimumFormSeconds": 2,
    "MaxLeadsPerIpPerHour": 10,
    "DuplicateWindowHours": 24
  }
}
```

Prefira variáveis de ambiente:

```text
CAMPAIGN_GENERATION_PROVIDER=OpenRouter
CAMPAIGN_GENERATION_FALLBACK_TO_FAKE=false
OPENROUTER_API_KEY=
OPENROUTER_MODEL=
WHATSAPP_NUMERO=
WHATSAPP_MENSAGEM_PADRAO=
GOOGLE_ADS_CLIENT_ID=
GOOGLE_ADS_CLIENT_SECRET=
GOOGLE_ADS_DEVELOPER_TOKEN=
GOOGLE_ADS_REDIRECT_URI=
GOOGLE_ADS_DEFAULT_DAILY_BUDGET=10.00
GOOGLE_ADS_DEFAULT_COUNTRY_CODE=BR
GOOGLE_ADS_DEFAULT_LANGUAGE_CODE=pt
GOOGLE_ADS_DEFAULT_CURRENCY_CODE=BRL
GOOGLE_ADS_DEFAULT_KEYWORD_MATCH_TYPE=Phrase
GOOGLE_ADS_DEFAULT_CAMPAIGN_STATUS=PAUSED
GOOGLE_ADS_ENABLE_BROAD_MATCH=false
GOOGLE_ADS_DEFAULT_CPC_BID=
GOOGLE_ADS_USE_TEST_ACCOUNT=false
GOOGLE_ADS_TEST_CUSTOMER_ID=
```

Nunca coloque chave real no repositório.

## Executar com Docker

Crie um `.env` local a partir de `.env.example` e ajuste as senhas/chaves reais. O `.env` não deve ser commitado.

```bash
copy .env.example .env
docker compose up --build
```

Serviços:

```text
API: http://localhost:5080
Swagger: http://localhost:5080/swagger
Web: http://localhost:5173
MySQL: localhost:3306
```

A API aplica migrations automaticamente no container com `Database__ApplyMigrationsOnStartup=true`.

## Executar localmente

Backend:

```bash
dotnet restore
dotnet ef database update --project src/LeadEngine.Infrastructure --startup-project src/LeadEngine.Api
dotnet run --project src/LeadEngine.Api
```

Frontend:

```bash
cd src/LeadEngine.Web
npm install
npm run dev
```

Testes:

```bash
dotnet test LeadEngine.sln
```

Build:

```bash
dotnet build LeadEngine.sln
cd src/LeadEngine.Web
npm run build
```

## Migrations

Migrations criadas:

```text
20260724151255_AddCampanhas
20260724173833_AddCampaignGenerationDetails
20260724194341_AddCampanhaRevisoes
20260724203700_AddLandingPublicaCapturaLeads
20260724210540_AddConfiguracoesSistema
20260724212632_AddGoogleAdsIntegration
20260724214217_AddGoogleAdsPreview
20260724215939_AddGoogleAdsControlledPublishing
20260724222003_AddGoogleAdsTypedPublishingAudit
```

Criar nova migration:

```bash
dotnet ef migrations add NomeDaMigration --project src/LeadEngine.Infrastructure --startup-project src/LeadEngine.Api --output-dir Persistence/Migrations
```

## Próximas etapas

- Testar OpenRouter em ambiente real com chave e modelo configurados.
- Evoluir a revisão visual da campanha.
- Separar entidades de grupo de anúncio, palavras-chave, anúncios e landing page quando houver necessidade real.
- Preparar exportação ou publicação futura para Google Ads.
