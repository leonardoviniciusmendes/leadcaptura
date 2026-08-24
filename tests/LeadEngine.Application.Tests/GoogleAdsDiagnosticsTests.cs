using System.Net;
using System.Text;
using LeadEngine.Application.Common;
using LeadEngine.Application.DTOs;
using LeadEngine.Application.Interfaces;
using LeadEngine.Application.Services;
using LeadEngine.Domain.Entities;
using LeadEngine.Domain.Enums;
using LeadEngine.Infrastructure.GoogleAds;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsDiagnosticsTests
{
    [Fact]
    public async Task MutateTransport_EnviaLoginCustomerIdSomenteQuandoConfigurado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "mutateOperationResponses": [] }"""));
        var transport = new GoogleAdsRestMutateTransport(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(loginCustomerId: "123-456-7890"));

        await transport.SendAsync("1112223333", "access-token", "developer-token", Plan(), false, CancellationToken.None);

        Assert.Equal("1234567890", handler.LastHeaders["login-customer-id"]);
        Assert.Equal("Bearer access-token", handler.LastHeaders["Authorization"]);
        Assert.Equal("developer-token", handler.LastHeaders["developer-token"]);

        handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "mutateOperationResponses": [] }"""));
        transport = new GoogleAdsRestMutateTransport(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(loginCustomerId: ""));

        await transport.SendAsync("1112223333", "access-token", "developer-token", Plan(), false, CancellationToken.None);

        Assert.False(handler.LastHeaders.ContainsKey("login-customer-id"));
    }

    [Fact]
    public async Task GaqlClient_EnviaLoginCustomerIdSomenteQuandoConfigurado()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "results": [] }"""));
        var client = new GoogleAdsGaqlClient(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(loginCustomerId: "123-456-7890"), new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));

        var first = await client.SearchAsync("1112223333", "access-token", "developer-token", "SELECT campaign.id FROM campaign LIMIT 1", CancellationToken.None);
        first.Body.Dispose();

        Assert.Equal("1234567890", handler.LastHeaders["login-customer-id"]);
        Assert.Equal("Bearer access-token", handler.LastHeaders["Authorization"]);
        Assert.Equal("developer-token", handler.LastHeaders["developer-token"]);

        handler = new StubHttpMessageHandler(_ => JsonResponse("""{ "results": [] }"""));
        client = new GoogleAdsGaqlClient(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(loginCustomerId: ""), new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));

        var second = await client.SearchAsync("1112223333", "access-token", "developer-token", "SELECT campaign.id FROM campaign LIMIT 1", CancellationToken.None);
        second.Body.Dispose();

        Assert.False(handler.LastHeaders.ContainsKey("login-customer-id"));
    }

    [Fact]
    public async Task DiagnosticsQueryClient_GetAdGroups_ConsultaAdGroupsComCampaignResource()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "results": [
            {
              "adGroup": {
                "resourceName": "customers/1112223333/adGroups/10",
                "id": "10",
                "name": "Grupo diagnostico",
                "status": "PAUSED"
              },
              "campaign": {
                "resourceName": "customers/1112223333/campaigns/24172834235"
              }
            }
          ]
        }
        """));
        var gaql = new GoogleAdsGaqlClient(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(), new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));
        var client = new GoogleAdsDiagnosticsQueryClient(gaql);

        var result = await client.GetAdGroupsAsync("1112223333", "access-token", "developer-token", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("customers/1112223333/adGroups/10", item.ResourceName);
        Assert.Equal("10", item.Id);
        Assert.Equal("Grupo diagnostico", item.Name);
        Assert.Equal("PAUSED", item.Status);
        Assert.Equal("customers/1112223333/campaigns/24172834235", item.CampaignResourceName);
        Assert.Contains("FROM ad_group", handler.LastRequestBody);
        Assert.Contains("campaign.resource_name", handler.LastRequestBody);
    }

    [Fact]
    public async Task DiagnosticsQueryClient_GetKeywords_ConsultaKeywordsComAdGroupResource()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "results": [
            {
              "adGroupCriterion": {
                "resourceName": "customers/1112223333/adGroupCriteria/199207417403~3001",
                "criterionId": "3001",
                "status": "PAUSED",
                "keyword": {
                  "text": "plano saude",
                  "matchType": "PHRASE"
                }
              },
              "adGroup": {
                "resourceName": "customers/1112223333/adGroups/199207417403"
              }
            }
          ]
        }
        """));
        var gaql = new GoogleAdsGaqlClient(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(), new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));
        var client = new GoogleAdsDiagnosticsQueryClient(gaql);

        var result = await client.GetKeywordsAsync("1112223333", "access-token", "developer-token", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("customers/1112223333/adGroupCriteria/199207417403~3001", item.ResourceName);
        Assert.Equal("3001", item.Id);
        Assert.Equal("plano saude", item.Text);
        Assert.Equal("PHRASE", item.MatchType);
        Assert.Equal("PAUSED", item.Status);
        Assert.Equal("customers/1112223333/adGroups/199207417403", item.AdGroupResourceName);
        Assert.Contains("FROM ad_group_criterion", handler.LastRequestBody);
        Assert.Contains("ad_group_criterion.keyword.text", handler.LastRequestBody);
        Assert.Contains("ad_group.resource_name", handler.LastRequestBody);
    }

    [Fact]
    public async Task DiagnosticsQueryClient_GetResponsiveSearchAds_ConsultaRsasComAdGroupResource()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "results": [
            {
              "adGroupAd": {
                "resourceName": "customers/1112223333/adGroupAds/199207417403~4001",
                "status": "PAUSED",
                "ad": {
                  "id": "4001"
                }
              },
              "adGroup": {
                "resourceName": "customers/1112223333/adGroups/199207417403"
              }
            }
          ]
        }
        """));
        var gaql = new GoogleAdsGaqlClient(new StubHttpClientFactory(new HttpClient(handler)), new Resolver(), new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));
        var client = new GoogleAdsDiagnosticsQueryClient(gaql);

        var result = await client.GetResponsiveSearchAdsAsync("1112223333", "access-token", "developer-token", CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("customers/1112223333/adGroupAds/199207417403~4001", item.ResourceName);
        Assert.Equal("4001", item.Id);
        Assert.Equal("PAUSED", item.Status);
        Assert.Equal("customers/1112223333/adGroups/199207417403", item.AdGroupResourceName);
        Assert.Contains("FROM ad_group_ad", handler.LastRequestBody);
        Assert.Contains("RESPONSIVE_SEARCH_AD", handler.LastRequestBody);
        Assert.Contains("ad_group.resource_name", handler.LastRequestBody);
    }

    [Fact]
    public async Task DiagnosticsService_CreateCampaign_GeraSomenteBudgetECampaignComCampaignPaused()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(
            new Contas(),
            new Token(),
            new Query(),
            mutation,
            new Resolver());

        await service.CreateCampaignAsync(new CreateGoogleAdsDiagnosticCampaignRequest("LeadEngine - Diagnostico", 10_000_000), CancellationToken.None);

        var plan = mutation.LastPlan!;
        Assert.Equal(["Budget", "Campaign"], plan.Operations.Select(x => x.TipoRecurso).ToArray());
        Assert.DoesNotContain(plan.Operations, x => x.TipoRecurso is "AdGroup" or "Keyword" or "ResponsiveSearchAd");

        var typed = new GoogleAdsTypedOperationFactory().Create(plan);
        Assert.NotNull(typed[0].CampaignBudgetOperation);
        Assert.NotNull(typed[1].CampaignOperation);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.CampaignStatusEnum.Types.CampaignStatus.Paused, typed[1].CampaignOperation.Create.Status);
        Assert.Equal(
            Google.Ads.GoogleAds.V22.Enums.EuPoliticalAdvertisingStatusEnum.Types.EuPoliticalAdvertisingStatus.DoesNotContainEuPoliticalAdvertising,
            typed[1].CampaignOperation.Create.ContainsEuPoliticalAdvertising);
        Assert.DoesNotContain("ENABLED", new GoogleAdsTypedOperationFactory().ToGoogleAdsJson(plan), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiagnosticsService_CreateCampaign_RequestInvalidoFalhaSemChamarMutation()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCampaignAsync(new CreateGoogleAdsDiagnosticCampaignRequest(" ", 10_000_000), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCampaignAsync(new CreateGoogleAdsDiagnosticCampaignRequest("Campanha", 0), CancellationToken.None));
        Assert.Equal(0, mutation.Calls);
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdGroup_GeraSomenteAdGroupComStatusPaused()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(
            new Contas(),
            new Token(),
            new Query(),
            mutation,
            new Resolver());

        await service.CreateAdGroupAsync(new CreateGoogleAdsDiagnosticAdGroupRequest(
            "customers/1112223333/campaigns/24172834235",
            "LeadEngine - Diagnostico Ad Group"), CancellationToken.None);

        var plan = mutation.LastPlan!;
        var operation = Assert.Single(plan.Operations);
        Assert.Equal("AdGroup", operation.TipoRecurso);
        Assert.Equal("AdGroupOperation", operation.Operation);
        Assert.DoesNotContain(plan.Operations, x => x.TipoRecurso is "Keyword" or "ResponsiveSearchAd" or "Campaign" or "Budget");
        Assert.Contains("\"campaignResource\":\"customers/1112223333/campaigns/24172834235\"", operation.PayloadJson);
        Assert.Contains("\"name\":\"LeadEngine - Diagnostico Ad Group\"", operation.PayloadJson);
        Assert.DoesNotContain("tracking", operation.PayloadJson, StringComparison.OrdinalIgnoreCase);

        var typed = new GoogleAdsTypedOperationFactory().Create(plan);
        Assert.NotNull(typed[0].AdGroupOperation);
        Assert.Equal("customers/1112223333/campaigns/24172834235", typed[0].AdGroupOperation.Create.Campaign);
        Assert.Equal("LeadEngine - Diagnostico Ad Group", typed[0].AdGroupOperation.Create.Name);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupStatusEnum.Types.AdGroupStatus.Paused, typed[0].AdGroupOperation.Create.Status);

        var json = new GoogleAdsTypedOperationFactory().ToGoogleAdsJson(plan);
        Assert.DoesNotContain("ENABLED", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adGroupCriterionOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adGroupAdOperation", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdGroup_RequestInvalidoFalhaSemChamarMutation()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdGroupAsync(new CreateGoogleAdsDiagnosticAdGroupRequest(" ", "Ad Group"), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAdGroupAsync(new CreateGoogleAdsDiagnosticAdGroupRequest("customers/1112223333/campaigns/1", " "), CancellationToken.None));
        Assert.Equal(0, mutation.Calls);
    }

    [Fact]
    public async Task DiagnosticsService_CreateAdGroup_ErroNaoExpoeCredenciaisDoContexto()
    {
        var mutation = new Mutation(success: false);
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => service.CreateAdGroupAsync(new CreateGoogleAdsDiagnosticAdGroupRequest(
            "customers/1112223333/campaigns/24172834235",
            "LeadEngine - Diagnostico Ad Group"), CancellationToken.None));
        var serialized = System.Text.Json.JsonSerializer.Serialize(ex.Diagnostic);

        Assert.DoesNotContain("access-token", serialized);
        Assert.DoesNotContain("developer-token", serialized);
    }

    [Fact]
    public async Task DiagnosticsService_CreateKeywords_GeraSomenteKeywordsComStatusPaused()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(
            new Contas(),
            new Token(),
            new Query(),
            mutation,
            new Resolver());

        await service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest(
            "customers/1112223333/adGroups/199207417403",
            [
                new("plano saude", "PHRASE"),
                new("cotacao plano saude", "EXACT")
            ]), CancellationToken.None);

        var plan = mutation.LastPlan!;
        Assert.Equal(["Keyword", "Keyword"], plan.Operations.Select(x => x.TipoRecurso).ToArray());
        Assert.DoesNotContain(plan.Operations, x => x.TipoRecurso is "ResponsiveSearchAd" or "AdGroup" or "Campaign" or "Budget");
        Assert.All(plan.Operations, operation =>
        {
            Assert.Equal("AdGroupCriterionOperation", operation.Operation);
            Assert.Contains("\"adGroupResource\":\"customers/1112223333/adGroups/199207417403\"", operation.PayloadJson);
        });
        Assert.Contains("\"text\":\"plano saude\"", plan.Operations[0].PayloadJson);
        Assert.Contains("\"matchType\":\"PHRASE\"", plan.Operations[0].PayloadJson);
        Assert.Contains("\"text\":\"cotacao plano saude\"", plan.Operations[1].PayloadJson);
        Assert.Contains("\"matchType\":\"EXACT\"", plan.Operations[1].PayloadJson);

        var typed = new GoogleAdsTypedOperationFactory().Create(plan);
        Assert.All(typed, operation =>
        {
            Assert.NotNull(operation.AdGroupCriterionOperation);
            Assert.Equal("customers/1112223333/adGroups/199207417403", operation.AdGroupCriterionOperation.Create.AdGroup);
            Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupCriterionStatusEnum.Types.AdGroupCriterionStatus.Paused, operation.AdGroupCriterionOperation.Create.Status);
        });
        Assert.Equal("plano saude", typed[0].AdGroupCriterionOperation.Create.Keyword.Text);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.KeywordMatchTypeEnum.Types.KeywordMatchType.Phrase, typed[0].AdGroupCriterionOperation.Create.Keyword.MatchType);
        Assert.Equal("cotacao plano saude", typed[1].AdGroupCriterionOperation.Create.Keyword.Text);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.KeywordMatchTypeEnum.Types.KeywordMatchType.Exact, typed[1].AdGroupCriterionOperation.Create.Keyword.MatchType);

        var json = new GoogleAdsTypedOperationFactory().ToGoogleAdsJson(plan);
        Assert.DoesNotContain("ENABLED", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adGroupAdOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adGroupOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("campaignOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("campaignBudgetOperation", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiagnosticsService_CreateKeywords_RejeitaBroadSemChamarMutation()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest(
            "customers/1112223333/adGroups/199207417403",
            [new("plano saude", "BROAD")]), CancellationToken.None));

        Assert.Contains("Broad", ex.Message);
        Assert.Equal(0, mutation.Calls);
    }

    [Fact]
    public async Task DiagnosticsService_CreateKeywords_RequestInvalidoFalhaSemChamarMutation()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest(" ", [new("plano saude", "PHRASE")]), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest("customers/1112223333/adGroups/199207417403", []), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest("customers/1112223333/adGroups/199207417403", [new(" ", "PHRASE")]), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest("customers/1112223333/adGroups/199207417403", [new("plano saude", "INVALID")]), CancellationToken.None));
        Assert.Equal(0, mutation.Calls);
    }

    [Fact]
    public async Task DiagnosticsService_CreateKeywords_ErroNaoExpoeCredenciaisDoContexto()
    {
        var mutation = new Mutation(success: false);
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => service.CreateKeywordsAsync(new CreateGoogleAdsDiagnosticKeywordsRequest(
            "customers/1112223333/adGroups/199207417403",
            [new("plano saude", "PHRASE")]), CancellationToken.None));
        var serialized = System.Text.Json.JsonSerializer.Serialize(ex.Diagnostic);

        Assert.DoesNotContain("access-token", serialized);
        Assert.DoesNotContain("developer-token", serialized);
    }

    [Fact]
    public async Task DiagnosticsService_CreateResponsiveSearchAd_GeraSomenteRsaComStatusPaused()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(
            new Contas(),
            new Token(),
            new Query(),
            mutation,
            new Resolver());

        await service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest(
            "customers/1112223333/adGroups/199207417403",
            "https://leadengine.test/lp/google",
            ["Plano Saude RJ", "Cotacao Rapida", "Contrate Hoje"],
            ["Compare planos de saude no Rio.", "Fale com um especialista agora."]), CancellationToken.None);

        var plan = mutation.LastPlan!;
        var operation = Assert.Single(plan.Operations);
        Assert.Equal("ResponsiveSearchAd", operation.TipoRecurso);
        Assert.Equal("AdGroupAdOperation", operation.Operation);
        Assert.DoesNotContain(plan.Operations, x => x.TipoRecurso is "Campaign" or "Budget" or "AdGroup" or "Keyword");
        Assert.Contains("\"adGroupResource\":\"customers/1112223333/adGroups/199207417403\"", operation.PayloadJson);
        Assert.Contains("\"finalUrls\":[\"https://leadengine.test/lp/google\"]", operation.PayloadJson);
        Assert.Contains("\"headlines\":[\"Plano Saude RJ\",\"Cotacao Rapida\",\"Contrate Hoje\"]", operation.PayloadJson);
        Assert.Contains("\"descriptions\":[\"Compare planos de saude no Rio.\",\"Fale com um especialista agora.\"]", operation.PayloadJson);
        Assert.DoesNotContain("tracking", operation.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", operation.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sitelink", operation.PayloadJson, StringComparison.OrdinalIgnoreCase);

        var typed = new GoogleAdsTypedOperationFactory().Create(plan);
        Assert.NotNull(typed[0].AdGroupAdOperation);
        Assert.Equal("customers/1112223333/adGroups/199207417403", typed[0].AdGroupAdOperation.Create.AdGroup);
        Assert.Equal(Google.Ads.GoogleAds.V22.Enums.AdGroupAdStatusEnum.Types.AdGroupAdStatus.Paused, typed[0].AdGroupAdOperation.Create.Status);
        Assert.Equal(["https://leadengine.test/lp/google"], typed[0].AdGroupAdOperation.Create.Ad.FinalUrls.ToArray());
        Assert.Equal(["Plano Saude RJ", "Cotacao Rapida", "Contrate Hoje"], typed[0].AdGroupAdOperation.Create.Ad.ResponsiveSearchAd.Headlines.Select(x => x.Text).ToArray());
        Assert.Equal(["Compare planos de saude no Rio.", "Fale com um especialista agora."], typed[0].AdGroupAdOperation.Create.Ad.ResponsiveSearchAd.Descriptions.Select(x => x.Text).ToArray());

        var json = new GoogleAdsTypedOperationFactory().ToGoogleAdsJson(plan);
        Assert.DoesNotContain("ENABLED", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("campaignOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("campaignBudgetOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adGroupOperation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adGroupCriterionOperation", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiagnosticsService_CreateResponsiveSearchAd_RequestInvalidoFalhaSemChamarMutation()
    {
        var mutation = new Mutation();
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());
        var validHeadlines = new[] { "Plano Saude RJ", "Cotacao Rapida", "Contrate Hoje" };
        var validDescriptions = new[] { "Compare planos de saude no Rio.", "Fale com um especialista agora." };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest(" ", "https://leadengine.test/lp", validHeadlines, validDescriptions), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "http://leadengine.test/lp", validHeadlines, validDescriptions), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", [], validDescriptions), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", ["Um", "Dois"], validDescriptions), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", Enumerable.Range(1, 16).Select(x => $"Titulo {x}").ToArray(), validDescriptions), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", ["Titulo maior que trinta caracteres"], validDescriptions), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", validHeadlines, []), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", validHeadlines, ["Uma descricao apenas"]), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", validHeadlines, Enumerable.Range(1, 5).Select(x => $"Descricao {x}").ToArray()), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest("customers/1112223333/adGroups/199207417403", "https://leadengine.test/lp", validHeadlines, ["Descricao com mais de noventa caracteres para comprovar que o validador local rejeita textos longos antes da Meta", "Outra descricao valida"]), CancellationToken.None));
        Assert.Equal(0, mutation.Calls);
    }

    [Fact]
    public async Task DiagnosticsService_CreateResponsiveSearchAd_ErroNaoExpoeCredenciaisDoContexto()
    {
        var mutation = new Mutation(success: false);
        var service = new GoogleAdsDiagnosticsService(new Contas(), new Token(), new Query(), mutation, new Resolver());

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => service.CreateResponsiveSearchAdAsync(new CreateGoogleAdsDiagnosticResponsiveSearchAdRequest(
            "customers/1112223333/adGroups/199207417403",
            "https://leadengine.test/lp/google",
            ["Plano Saude RJ", "Cotacao Rapida", "Contrate Hoje"],
            ["Compare planos de saude no Rio.", "Fale com um especialista agora."]), CancellationToken.None));
        var serialized = System.Text.Json.JsonSerializer.Serialize(ex.Diagnostic);

        Assert.DoesNotContain("access-token", serialized);
        Assert.DoesNotContain("developer-token", serialized);
    }

    [Fact]
    public void GoogleAdsExceptionFormatter_SanitizaCredenciaisNoDiagnosticoELog()
    {
        var logger = new CaptureLogger<GoogleAdsExceptionFormatter>();
        var formatter = new GoogleAdsExceptionFormatter(logger);
        const string body = """
        {
          "error": {
            "message": "Invalid request access_token=access-secret developer-token=developer-secret",
            "status": "INVALID_ARGUMENT",
            "details": [
              {
                "errors": [
                  {
                    "message": "Bearer access-secret client_secret=client-secret",
                    "trigger": { "stringValue": "refresh_token=refresh-secret" }
                  }
                ],
                "requestId": "req-1"
              }
            ]
          }
        }
        """;

        var result = formatter.FromRestError(body, null, "400", "developer_token=developer-secret");
        var serialized = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.DoesNotContain("access-secret", serialized);
        Assert.DoesNotContain("developer-secret", serialized);
        Assert.DoesNotContain("client-secret", serialized);
        Assert.DoesNotContain("refresh-secret", serialized);
        Assert.DoesNotContain("access-secret", logger.LastMessage);
        Assert.DoesNotContain("developer-secret", logger.LastMessage);
        Assert.DoesNotContain("client-secret", logger.LastMessage);
        Assert.DoesNotContain("refresh-secret", logger.LastMessage);
    }

    [Fact]
    public async Task OAuthClient_ListAccessibleAccounts_DevolveDiagnosticoParaDeveloperTokenNotApproved()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "code": 403,
            "message": "The developer token is only approved for use with test accounts.",
            "status": "PERMISSION_DENIED",
            "details": [
              {
                "@type": "type.googleapis.com/google.ads.googleads.v22.errors.GoogleAdsFailure",
                "errors": [
                  {
                    "errorCode": { "authorizationError": "DEVELOPER_TOKEN_NOT_APPROVED" },
                    "message": "The developer token is only approved for use with test accounts."
                  }
                ],
                "requestId": "req-dev-token"
              }
            ]
          }
        }
        """, HttpStatusCode.Forbidden));
        var client = new GoogleAdsOAuthClient(
            new StubHttpClientFactory(new HttpClient(handler)),
            new Resolver(),
            new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance));

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => client.ListAccessibleAccountsAsync("access-token", CancellationToken.None));

        Assert.Equal("authorizationError.DEVELOPER_TOKEN_NOT_APPROVED", ex.Diagnostic.Codigo);
        Assert.Equal("req-dev-token", ex.Diagnostic.RequestId);
        Assert.Equal("403", ex.Diagnostic.StatusCode);
        Assert.Contains("test accounts", ex.Diagnostic.Mensagem);
    }

    [Fact]
    public async Task OAuthClient_ListAccessibleAccounts_SanitizaCredenciaisDoErro()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""
        {
          "error": {
            "code": 403,
            "message": "Bearer access-secret developer-token=developer-secret client_secret=client-secret refresh_token=refresh-secret",
            "status": "PERMISSION_DENIED",
            "details": [
              {
                "errors": [
                  {
                    "errorCode": { "authorizationError": "DEVELOPER_TOKEN_NOT_APPROVED" },
                    "message": "Authorization failed access_token=access-secret"
                  }
                ],
                "requestId": "req-sanitized"
              }
            ]
          }
        }
        """, HttpStatusCode.Forbidden));
        var logger = new CaptureLogger<GoogleAdsExceptionFormatter>();
        var client = new GoogleAdsOAuthClient(
            new StubHttpClientFactory(new HttpClient(handler)),
            new Resolver(),
            new GoogleAdsExceptionFormatter(logger));

        var ex = await Assert.ThrowsAsync<GoogleAdsDiagnosticException>(() => client.ListAccessibleAccountsAsync("access-secret", CancellationToken.None));
        var serialized = System.Text.Json.JsonSerializer.Serialize(ex.Diagnostic);

        Assert.DoesNotContain("access-secret", serialized);
        Assert.DoesNotContain("developer-secret", serialized);
        Assert.DoesNotContain("client-secret", serialized);
        Assert.DoesNotContain("refresh-secret", serialized);
        Assert.DoesNotContain("access-secret", logger.LastMessage);
        Assert.DoesNotContain("developer-secret", logger.LastMessage);
        Assert.DoesNotContain("client-secret", logger.LastMessage);
        Assert.DoesNotContain("refresh-secret", logger.LastMessage);
    }

    private static GoogleAdsOperationPlan Plan()
    {
        return new GoogleAdsOperationPlan("HASH", 1, "1112223333", string.Empty, string.Empty,
        [
            new("Budget", "Budget", "CampaignBudgetOperation", "{\"resourceName\":\"customers/1112223333/campaignBudgets/-1\",\"name\":\"Budget\",\"amountMicros\":10000000}", "customers/1112223333/campaignBudgets/-1"),
            new("Campaign", "Campaign", "CampaignOperation", "{\"resourceName\":\"customers/1112223333/campaigns/-2\",\"name\":\"Campaign\",\"budgetResource\":\"customers/1112223333/campaignBudgets/-1\"}", "customers/1112223333/campaigns/-2")
        ], []);
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public Dictionary<string, string> LastHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastHeaders.Clear();
            LastRequestBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult() ?? string.Empty;
            foreach (var header in request.Headers)
            {
                LastHeaders[header.Key] = string.Join(",", header.Value);
            }
            return Task.FromResult(responder(request));
        }
    }

    private sealed class Resolver(string loginCustomerId = "") : IConfigurationResolver
    {
        public Task<ResolvedConfigurationValue> ResolveAsync(CategoriaConfiguracao categoria, string chave, CancellationToken cancellationToken)
        {
            var value = chave switch
            {
                "ApiBaseUrl" => "https://googleads.googleapis.com/v22",
                "ApiTimeoutSeconds" => "60",
                "LoginCustomerId" => loginCustomerId,
                "DeveloperToken" => "developer-token",
                _ => string.Empty
            };
            return Task.FromResult(new ResolvedConfigurationValue(value, !string.IsNullOrWhiteSpace(value), OrigemConfiguracao.Banco, chave.Contains("Token", StringComparison.OrdinalIgnoreCase)));
        }

        public Task InvalidateAsync(CategoriaConfiguracao categoria, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Contas : IGoogleAdsContaRepository
    {
        private readonly GoogleAdsConta conta = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = "1112223333",
            Nome = "Conta teste",
            Ativa = true,
            Padrao = true,
            AccessTokenProtegido = "protected",
            RefreshTokenProtegido = "protected",
            AccessTokenExpiraEm = DateTime.UtcNow.AddHours(1)
        };

        public Task<GoogleAdsConta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<GoogleAdsConta?>(conta);
        public Task<GoogleAdsConta?> ObterPorCustomerIdAsync(string customerId, CancellationToken cancellationToken) => Task.FromResult<GoogleAdsConta?>(conta);
        public Task<GoogleAdsConta?> ObterPadraoAsync(CancellationToken cancellationToken) => Task.FromResult<GoogleAdsConta?>(conta);
        public Task<IReadOnlyList<GoogleAdsConta>> ListarAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<GoogleAdsConta>>([conta]);
        public Task AdicionarAsync(GoogleAdsConta conta, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SalvarAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Token : IGoogleAdsTokenService
    {
        public Task<string> ObterAccessTokenValidoAsync(GoogleAdsConta conta, CancellationToken cancellationToken) => Task.FromResult("access-token");
    }

    private sealed class Query : IGoogleAdsDiagnosticsQueryClient
    {
        public Task<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>> GetCampaignsAsync(string customerId, string accessToken, string developerToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GoogleAdsDiagnosticCampaignDto>>([]);
        }

        public Task<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>> GetAdGroupsAsync(string customerId, string accessToken, string developerToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GoogleAdsDiagnosticAdGroupDto>>([]);
        }

        public Task<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>> GetKeywordsAsync(string customerId, string accessToken, string developerToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GoogleAdsDiagnosticKeywordDto>>([]);
        }

        public Task<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>> GetResponsiveSearchAdsAsync(string customerId, string accessToken, string developerToken, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<GoogleAdsDiagnosticResponsiveSearchAdDto>>([]);
        }
    }

    private sealed class Mutation(bool success = true) : IGoogleAdsMutationClient
    {
        public int Calls { get; private set; }
        public GoogleAdsOperationPlan? LastPlan { get; private set; }

        public Task<GoogleAdsMutationResult> MutateAsync(string customerId, string accessToken, string developerToken, GoogleAdsOperationPlan plan, bool validateOnly, CancellationToken cancellationToken)
        {
            Calls++;
            LastPlan = plan;
            if (!success)
            {
                return Task.FromResult(new GoogleAdsMutationResult(false, "req-1", [], [
                    new("fieldError.REQUIRED", "Campo obrigatorio ausente.", "AdGroupOperation", 0, "ad_group.name", null, "req-1", false, null)
                ], false));
            }

            var resources = plan.Operations.Select((operation, index) =>
                new GoogleAdsPublishedResourceDto(
                    operation.TipoRecurso,
                    operation.TipoRecurso == "AdGroup" ? $"customers/{customerId}/adGroups/{index + 1}" : $"customers/{customerId}/{operation.TipoRecurso}/{index + 1}",
                    (index + 1).ToString(),
                    operation.Nome,
                    "PAUSED")).ToArray();

            return Task.FromResult(new GoogleAdsMutationResult(true, "req-1", resources, [], false));
        }

        public Task<IReadOnlyList<GoogleAdsPublishedResourceDto>> CheckResourcesAsync(string customerId, string accessToken, string developerToken, IReadOnlyList<GoogleAdsPublishedResourceDto> resources, CancellationToken cancellationToken)
        {
            return Task.FromResult(resources);
        }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public string LastMessage { get; private set; } = string.Empty;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastMessage = formatter(state, exception);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
