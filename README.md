# LeadEngine

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
- Google Ads real;
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
    "RedirectUri": "http://localhost:5173/configuracoes?googleAdsCallback=1",
    "AuthEndpoint": "https://accounts.google.com/o/oauth2/v2/auth",
    "TokenEndpoint": "https://oauth2.googleapis.com/token",
    "UserInfoEndpoint": "https://openidconnect.googleapis.com/v1/userinfo",
    "ApiBaseUrl": "https://googleads.googleapis.com/v19",
    "Scopes": "https://www.googleapis.com/auth/adwords openid email profile"
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
