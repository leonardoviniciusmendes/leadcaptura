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

Prefira variáveis de ambiente:

```text
CAMPAIGN_GENERATION_PROVIDER=OpenRouter
CAMPAIGN_GENERATION_FALLBACK_TO_FAKE=false
OPENROUTER_API_KEY=
OPENROUTER_MODEL=
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
