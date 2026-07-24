# Arquitetura do LeadEngine

## Objetivo

O LeadEngine é um assistente para gerar campanhas de Google Ads para planos de saúde. O usuário não precisa conhecer Google Ads; ele informa um briefing simples e o sistema gera o conteúdo inicial da campanha.

## Camadas

```text
LeadEngine.Domain
  Entidades e enums sem dependências externas.

LeadEngine.Application
  Casos de uso, DTOs, validações, interfaces e abstrações.

LeadEngine.Infrastructure
  Entity Framework Core, MySQL, repositories e migrations.

LeadEngine.Api
  Controllers HTTP, middlewares, CORS, Swagger e composição de dependências.

LeadEngine.Web
  Vue 3 e TypeScript para as telas de campanha.
```

## Fluxo vertical atual

```text
NovaCampanhaView
-> POST /api/campanhas/gerar
-> CampanhaService.GerarCampanhaAsync
-> FakeCampaignGenerationService
-> CampanhaRepository
-> MySQL
-> CampanhasView
```

## Domínio atual

Entidade principal:

```text
Campanha
```

Campos mantidos dentro de `Campanha` nesta etapa:

- briefing;
- conteúdo gerado;
- slug;
- status;
- datas de criação e atualização.

Entidades como `GrupoAnuncio`, `PalavraChave`, `Anuncio` e `LandingPage` ainda não foram criadas porque o fluxo atual não precisa delas.

## Decisões

- CQRS foi aplicado de forma leve por métodos de caso de uso, sem MediatR.
- Repository Pattern foi mantido com `ICampanhaRepository`.
- A geração usa `ICampaignGenerationService`.
- A implementação atual é `FakeCampaignGenerationService`, determinística e sem IA.
- OpenRouter e Google Ads não foram integrados nesta etapa.
- O módulo antigo de leads permanece no código, mas não é o fluxo principal do produto.

## Persistência

EF Core com MySQL.

Tabela criada:

```text
Campanhas
```

Regras relevantes:

- `OrcamentoDiario` com precisão `decimal(10,2)`;
- índice único para `Slug`;
- índice por `DataCriacao`;
- índice por `Status`.
