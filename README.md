# LeadEngine

Assistente para gerar campanhas de Google Ads para planos de saúde sem exigir conhecimento técnico de mídia paga.

Nesta etapa, o usuário informa apenas:

- público;
- cidade;
- estado;
- bairro ou região, opcional;
- operadora;
- orçamento diário;
- objetivo ou observação, opcional.

O sistema gera uma campanha simulada, salva no MySQL e exibe o resultado no painel. Ainda não há integração real com OpenRouter nem Google Ads.

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
- OpenRouter real;
- Google Ads real;
- landing pública;
- leads;
- métricas.

Os módulos antigos de captura de leads ainda existem no código, mas não são o fluxo principal do produto nesta etapa.

## Arquitetura

```text
src/
  LeadEngine.Api             Controllers, Swagger, middlewares, CORS e health check
  LeadEngine.Application     DTOs, validações, interfaces e casos de uso
  LeadEngine.Domain          Entidades e enums de domínio
  LeadEngine.Infrastructure  EF Core, MySQL, repositories, migrations e integrações
  LeadEngine.Web             Vue 3, TypeScript e telas do painel
tests/
  LeadEngine.Application.Tests
```

Mais detalhes em `docs/architecture.md`.

## Fluxo implementado

```text
Briefing simples
-> FakeCampaignGenerationService
-> Campanha persistida no MySQL
-> Consulta por API
-> Exibição no frontend
```

## Endpoints de campanhas

```http
POST /api/campanhas/gerar
GET  /api/campanhas
GET  /api/campanhas/{id}
PUT  /api/campanhas/{id}/revisao
```

Exemplo de briefing:

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

## Configuração

Principais variáveis:

```text
ConnectionStrings__DefaultConnection
Cors__AllowedOrigins__0
VITE_API_BASE_URL
```

O OpenRouter será configurado em etapa futura pelo `appsettings`.

## Executar com Docker

Crie um `.env` local a partir de `.env.example` e ajuste as senhas reais. O `.env` não deve ser commitado.

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

Migration criada nesta etapa:

```text
20260724151255_AddCampanhas
```

Criar nova migration:

```bash
dotnet ef migrations add NomeDaMigration --project src/LeadEngine.Infrastructure --startup-project src/LeadEngine.Api --output-dir Persistence/Migrations
```

## Próximas etapas

- Integrar `ICampaignGenerationService` com OpenRouter.
- Separar entidades de grupo de anúncio, palavras-chave, anúncios e landing page quando houver necessidade real.
- Adicionar revisão visual mais completa da campanha.
- Preparar exportação ou publicação futura para Google Ads.
