# Arquitetura do LeadEngine

## Objetivo

O LeadEngine é um assistente para gerar campanhas de Google Ads para planos de saúde. O usuário não precisa conhecer Google Ads; ele informa um briefing simples e o sistema gera o conteúdo inicial da campanha.

## Camadas

```text
LeadEngine.Domain
  Entidades e enums sem dependências externas.

LeadEngine.Application
  Casos de uso, DTOs, validações, prompt builder, parser e abstrações.

LeadEngine.Infrastructure
  Entity Framework Core, MySQL, repositories, OpenRouter e migrations.

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
-> ICampaignGenerationService
-> ConfiguredCampaignGenerationService
-> FakeCampaignGenerationService ou OpenRouterCampaignGenerationService
-> CampaignGenerationResponseParser
-> CampanhaRepository
-> MySQL
-> CampanhasView
```

Fluxo de revisão comercial:

```text
CampanhaRevisaoView
-> GET /api/campanhas/{id}/revisao
-> PUT /api/campanhas/{id}/revisao
-> POST /api/campanhas/{id}/regenerar
-> POST /api/campanhas/{id}/aprovar
-> CampaignReviewService
-> ICampaignSectionGenerationService quando ha regeneracao parcial
-> CampanhaRevisao para auditoria
-> MySQL
```

Fluxo de landing pública:

```text
Campanha Revisada
-> POST /api/campanhas/{id}/publicar
-> CampaignPublicationService
-> /lp/{slug}
-> GET /api/publico/campanhas/{slug}
-> POST /api/publico/campanhas/{slug}/leads
-> LeadService
-> LeadRepository
-> WhatsAppUrlBuilder
-> redirecionamento controlado para WhatsApp
```

Fluxo de configurações:

```text
ConfiguracoesView
-> /api/configuracoes
-> ConfiguracaoService
-> ConfigurationResolver
-> Banco -> Variavel de ambiente -> AppSettings -> Padrao
-> DataProtectionSecretProtector para segredos
-> cache curto por categoria/chave
```

## Domínio atual

Entidade principal:

```text
Campanha
CampanhaRevisao
Lead
ConfiguracaoSistema
ConfiguracaoSistemaHistorico
```

Campos mantidos dentro de `Campanha` nesta etapa:

- briefing;
- conteúdo gerado;
- benefícios;
- FAQ;
- palavras-chave;
- palavras negativas;
- títulos e descrições de anúncios;
- slug;
- status;
- provider/modelo da IA;
- erro de geração;
- duração e data da geração.
- histórico de revisão em tabela separada.
- publicação da landing na própria `Campanha`;
- vínculo opcional de `Lead` com `Campanha`;
- UTMs e status de envio externo preparados para integração futura.
- configurações operacionais em `ConfiguracaoSistema`;
- histórico de alteração sem armazenamento de segredos.

Entidades como `GrupoAnuncio`, `PalavraChave`, `Anuncio` e `LandingPage` ainda não foram criadas porque o fluxo atual não precisa delas como agregados separados.

## Decisões

- CQRS foi aplicado de forma leve por métodos de caso de uso, sem MediatR.
- Repository Pattern foi mantido com `ICampanhaRepository`.
- A geração usa `ICampaignGenerationService`.
- `FakeCampaignGenerationService` continua disponível para desenvolvimento e testes.
- `OpenRouterCampaignGenerationService` usa HTTP simples com `IHttpClientFactory`, sem SDK externo.
- `CampaignGenerationResponseParser` valida e normaliza a resposta da IA antes da persistência final.
- O slug retornado pela IA nunca é confiado diretamente; ele é recriado com `CampanhaText.Slugify`.
- Fallback para Fake só ocorre quando `CampaignGeneration:FallbackToFake` está explicitamente `true`.
- Google Ads possui apenas infraestrutura de conexão OAuth, seleção de conta e teste de conectividade; publicação de campanhas continua fora do escopo.
- Pré-publicação Google Ads gera apenas preview técnico persistido; não cria budget, campaign, ad group, keyword ou ad no Google Ads.
- O módulo antigo de leads permanece no código, mas não é o fluxo principal do produto.
- A revisão manual não aceita status, provider, modelo, duração, id ou data de criação vindos do frontend.
- `CampanhaSecao` é enum e o backend valida se a seção recebida é suportada antes de chamar IA.
- Regeneração parcial chama OpenRouter apenas para a seção solicitada e aplica o resultado somente após validação da campanha completa.
- Ao editar uma campanha já aprovada, o status volta para `Gerada`; não foi criado um status `EmRevisao` para manter a modelagem simples.
- O histórico armazena conteúdo anterior e novo em JSON, mas a API pública de histórico retorna apenas resumo, origem, seção, provider e modelo.
- Ao editar ou regenerar uma campanha publicada, ela é despublicada automaticamente e volta para `Gerada`.
- A landing pública usa DTO específico e não expõe provider, modelo, duração, erro técnico, histórico ou campos administrativos.
- A tabela `Leads` já existia; a migration da landing adiciona vínculo com campanha, campos de rastreamento, tipo de contratação e controles de envio externo.
- A captura pública não envia para API externa nesta etapa; `StatusEnvioExterno` fica preparado como `Pendente`.
- Duplicidade é verificada por consulta usando campanha, telefone normalizado e janela configurável, sem índice único para não bloquear casos legítimos.
- O WhatsApp é apenas URL gerada; não há envio automático.
- OpenRouter, geração, WhatsApp, LeadCapture, URL pública e landing passam por um resolver único de configuração efetiva.
- Segredos usam `ISecretProtector`; a implementação atual é `DataProtectionSecretProtector`.
- A prioridade efetiva é Banco > variáveis de ambiente > appsettings > padrão seguro.
- O cache de configuração evita leitura do banco em toda resolução e é invalidado ao atualizar uma categoria.
- Configurações sensíveis não são devolvidas ao frontend e não entram no histórico.

## Persistência

EF Core com MySQL.

Tabela principal:

```text
Campanhas
CampanhasRevisoes
Leads
ConfiguracoesSistema
ConfiguracoesSistemaHistorico
GoogleAdsContas
GoogleAdsPlanosPublicacao
```

Regras relevantes:

- `OrcamentoDiario` com precisão `decimal(10,2)`;
- índice único para `Slug`;
- índice por `DataCriacao`;
- índice por `Status`;
- listas geradas armazenadas em colunas JSON dentro de `Campanha`.
- `CampanhasRevisoes` referencia `Campanhas` com cascade delete;
- `ConteudoAnterior` e `ConteudoNovo` ficam em colunas JSON;
- prompts completos, API keys e respostas completas de provider não são expostos por endpoint de histórico.
- `Campanhas` possui `Publicada`, `Ativo`, `DataPublicacao`, `DataDespublicacao` e `UrlPublica`;
- `Leads` possui índice em `CampanhaId` e índice composto em `CampanhaId`, `WhatsAppNormalizado`, `CriadoEm` para duplicidade por janela;
- IP puro não é armazenado na captura pública; o contexto expõe hash.
- `ConfiguracoesSistema.Chave` é única;
- `ConfiguracoesSistemaHistorico` mantém valores anteriores/novos apenas para configurações não sensíveis.
- `GoogleAdsContas.CustomerId` é único;
- tokens OAuth do Google Ads ficam protegidos em `AccessTokenProtegido` e `RefreshTokenProtegido`.
- `GoogleAdsPlanosPublicacao.CampanhaId` é único para evitar previews duplicados por campanha;
- `GoogleAdsPlanosPublicacao` mantém controle normalizado e payload técnico detalhado em JSON.

## Configurações

Categorias fixas:

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

Serviços que usam configuração efetiva:

- `OpenRouterCampaignGenerationService`;
- `OpenRouterCampaignSectionGenerationService`;
- `ConfiguredCampaignGenerationService`;
- `WhatsAppUrlBuilder`;
- `LeadService`;
- `CampaignPublicationService`.

Valores sensíveis:

- `OpenRouter.ApiKey`;
- `ExternalLeadApi.ApiKey`;
- `GoogleAds.ClientSecret`;
- `GoogleAds.DeveloperToken`;
- tokens OAuth persistidos em `GoogleAdsContas`;
- futuros tokens, senhas e client secrets.

Limitação atual: o rate limiter global do ASP.NET Core é configurado na inicialização; os demais valores operacionais resolvidos pelos serviços passam a valer sem reiniciar após invalidação de cache.

## Google Ads

O módulo `GoogleAds` é uma preparação de infraestrutura. Ele não cria campanhas, grupos de anúncios, palavras-chave, anúncios ou publicações.

Componentes:

- `GoogleAdsController` expõe status, auth URL, callback OAuth, contas, seleção da conta padrão e teste;
- `GoogleAdsConnectionService` coordena OAuth, persistência de contas e teste;
- `GoogleAdsTokenService` renova access token automaticamente usando refresh token protegido;
- `IGoogleAdsOAuthClient` isola chamadas HTTP reais para Google OAuth e Google Ads API;
- `GoogleAdsContaRepository` persiste contas conectadas.

Fluxo OAuth:

1. A UI chama `GET /api/googleads/auth-url`.
2. O usuário autoriza no Google.
3. O callback envia `code` para `POST /api/googleads/oauth/callback`.
4. O backend troca o code por tokens, busca o e-mail do usuário e lista contas acessíveis.
5. Contas são gravadas em `GoogleAdsContas`; tokens são protegidos com `ISecretProtector`.
6. A conta padrão pode ser selecionada por `POST /api/googleads/contas/{id}/selecionar`.

Status possíveis na UI:

- `Nao conectado`;
- `Conectado`;
- `Token expirado`.

## Pré-publicação Google Ads

O módulo de preview técnico fica entre a campanha revisada/publicada e uma futura publicação real.

Fluxo:

1. `GoogleAdsPreviewService` carrega a campanha, a conta Google Ads padrão e configurações efetivas.
2. `GoogleAdsValidationService` valida pendências de entrada.
3. `GoogleAdsCampaignMappingService` monta campanha SEARCH, orçamento STANDARD, um ad group, keywords, negativas e Responsive Search Ad.
4. O preview é validado e persistido em `GoogleAdsPlanosPublicacao`.
5. O payload fica disponível por `GET /api/googleads/preview/{id}/payload`.

Decisões:

- apenas um ad group é gerado por campanha neste MVP, mas o payload já usa lista;
- campanhas planejadas saem sempre como `PAUSED`;
- Display Network fica sempre desativada;
- orçamento é armazenado em decimal e convertido para micros no payload;
- palavras principais usam `PHRASE`; palavras de intenção como cotação/preço/contratar usam `EXACT`;
- `BROAD` não é gerado automaticamente, salvo configuração explícita;
- paths são derivados do slug com `Slugify` e limite de 15 caracteres;
- textos acima do limite não são truncados silenciosamente;
- ajuste por IA retorna original/sugestão e exige aplicação manual.

Status do plano:

- `Rascunho`;
- `Valido`;
- `Invalido`;
- `Desatualizado`;
- `Publicado`;
- `Erro`.

Nesta etapa o sistema nunca define `Publicado`.

Detecção de desatualização:

- o serviço calcula SHA-256 sobre headlines, descriptions, keywords, negativas, slug, URL pública, orçamento, público, região, cidade/UF e benefícios;
- se o hash atual divergir do salvo, a resposta marca o preview como `Desatualizado`.

O payload técnico não inclui access token, refresh token, client secret, developer token, API key ou qualquer segredo.

## Captura pública

Proteções do MVP:

- rate limit por IP no endpoint público de lead;
- honeypot `website`;
- tempo mínimo configurável entre renderizar o formulário e enviar;
- User-Agent exigido quando disponível no contexto;
- mensagens de erro controladas pelo middleware.

Configurações:

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

## OpenRouter

O provider `OpenRouter` chama:

```http
POST /api/v1/chat/completions
```

Com:

- `Authorization: Bearer {ApiKey}`;
- `Content-Type: application/json`;
- `HTTP-Referer`;
- `X-Title`;
- `response_format: { "type": "json_object" }`.

Resiliência implementada:

- timeout configurável;
- retry para 408, 429 e HTTP 5xx;
- sem retry para erros 4xx não transitórios;
- logs sem expor chave;
- erro controlado pela API.
- falhas na regeneração parcial não apagam nem substituem o conteúdo atual.
