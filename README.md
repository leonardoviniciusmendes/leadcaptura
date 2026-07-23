# Lead Engine

Aplicação simples para capturar leads originados de campanhas do Google Ads para venda de planos de saúde.

Fluxo atual:

```text
Google Ads -> Landing page -> Formulário -> WhatsApp do corretor
```

Neste primeiro momento, o formulário abre o WhatsApp do corretor com os dados preenchidos. A API e a estrutura de banco permanecem no projeto para a fase futura, quando os leads também serão persistidos.

## Arquitetura

```text
src/
  LeadEngine.Api             Controllers, Swagger, Serilog, middlewares
  LeadEngine.Application     DTOs, interfaces, validacoes e casos de uso
  LeadEngine.Domain          Entidades e enums
  LeadEngine.Infrastructure  EF Core, MySQL, repositorios e API externa
  LeadEngine.Web             Vue 3, landing pages e formularios
```

## Configuração

Principais variaveis:

```text
ConnectionStrings__DefaultConnection
IntegracaoLeads__BaseUrl
IntegracaoLeads__Endpoint
IntegracaoLeads__ApiKey
Cors__AllowedOrigins__0
VITE_API_BASE_URL
VITE_CORRETOR_WHATSAPP
```

`VITE_CORRETOR_WHATSAPP` deve ser informado com DDI e DDD, somente números. Exemplo: `5521999999999`.

Exemplo da integração externa:

```json
{
  "IntegracaoLeads": {
    "BaseUrl": "https://api-destino.com",
    "Endpoint": "/api/leads",
    "ApiKey": ""
  }
}
```

## Executar com Docker

Crie um `.env` local a partir de `.env.example` e ajuste as senhas/chaves reais. O `.env` não deve ser commitado.

```bash
copy .env.example .env
docker compose up --build
```

A API aplica as migrations automaticamente no container por causa de `Database__ApplyMigrationsOnStartup=true`.

Serviços:

```text
API: http://localhost:5080
Swagger: http://localhost:5080/swagger
Web: http://localhost:5173
MySQL: localhost:3306
```

Aplicar migrations no banco:

```bash
dotnet ef database update --project src/LeadEngine.Infrastructure --startup-project src/LeadEngine.Api
```

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

## Criar migrations

```bash
dotnet ef migrations add NomeDaMigration --project src/LeadEngine.Infrastructure --startup-project src/LeadEngine.Api --output-dir Persistence/Migrations
```

## Endpoints

```http
POST /api/leads
GET  /api/leads
GET  /api/leads/{id}
POST /api/leads/{id}/reenviar
GET  /health
```

Filtros de `GET /api/leads`:

```text
dataInicial, dataFinal, tipo, status, campanha, landingPage, whatsApp, pagina, tamanhoPagina
```

## Landing pages

```text
/
/plano-de-saude
/plano-familiar
/plano-individual
/plano-empresarial
/plano-mei
/amil
/bradesco-saude
/sulamerica-saude
/unimed
```

As paginas ficam configuradas em `src/LeadEngine.Web/src/landingPages.ts`.

## Exemplo de requisição

```json
{
  "tipo": "Familia",
  "nome": "Joao da Silva",
  "whatsApp": "21999999999",
  "cep": "22795941",
  "quantidadeVidas": 3,
  "idades": [42, 45, 12],
  "hospitalDesejado": "Barra D'Or",
  "operadoraDesejada": "Amil",
  "possuiPlanoAtual": true,
  "planoAtual": "Unimed",
  "consentimentoContato": true,
  "origem": {
    "gclid": "exemplo",
    "utmSource": "google",
    "utmMedium": "cpc",
    "utmCampaign": "plano_familiar_rj",
    "utmTerm": "plano de saude familiar",
    "landingPage": "/plano-familiar",
    "referrer": "https://www.google.com"
  }
}
```

## Segurança e privacidade

- O IP original não é armazenado; apenas hash.
- O frontend não envia dados pessoais para `dataLayer`.
- O backend aplica limite de request, rate limiting, honeypot, validacao e headers de seguranca.
- Logs não registram corpo completo da requisição.
- WhatsApp, e-mail, CNPJ e CEP são mascarados nas respostas administrativas.

## Pendencias para producao

- Configurar URL e chave reais da API externa.
- Configurar HTTPS e dominios permitidos no CORS.
- Rodar migrations no ambiente definitivo.
- Definir tag real do Google Ads/GTM na hospedagem.
- Integrar reCAPTCHA ou Cloudflare Turnstile se houver volume de spam.
