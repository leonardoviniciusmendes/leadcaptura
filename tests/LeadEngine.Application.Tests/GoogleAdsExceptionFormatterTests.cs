using LeadEngine.Infrastructure.GoogleAds;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeadEngine.Application.Tests;

public sealed class GoogleAdsExceptionFormatterTests
{
    [Fact]
    public void RestError_RetornaDetalhesDoGoogleAdsFailure()
    {
        var formatter = new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance);
        const string body = """
        {
          "error": {
            "code": 400,
            "message": "Request contains an invalid argument.",
            "status": "INVALID_ARGUMENT",
            "details": [
              {
                "@type": "type.googleapis.com/google.ads.googleads.v22.errors.GoogleAdsFailure",
                "errors": [
                  {
                    "errorCode": { "urlFieldError": "INVALID_TRACKING_URL_TEMPLATE" },
                    "message": "The URL is invalid.",
                    "trigger": { "stringValue": "http://localhost:5173/leadcaptura/lp/x" },
                    "location": {
                      "fieldPathElements": [
                        { "fieldName": "mutate_operations", "index": 0 },
                        { "fieldName": "campaign_operation" },
                        { "fieldName": "create" },
                        { "fieldName": "final_url_suffix" }
                      ]
                    }
                  }
                ],
                "requestId": "abc123"
              }
            ]
          }
        }
        """;

        var result = formatter.FromRestError(body, null, "400", "Google Ads rejeitou a operacao.");

        Assert.False(result.Sucesso);
        Assert.Equal("urlFieldError.INVALID_TRACKING_URL_TEMPLATE", result.Codigo);
        Assert.Equal("abc123", result.RequestId);
        Assert.Single(result.Erros);
        Assert.Equal("The URL is invalid.", result.Erros[0].Mensagem);
        Assert.Equal("http://localhost:5173/leadcaptura/lp/x", result.Erros[0].Trigger);
        Assert.Contains("mutate_operations[0]", result.Erros[0].FieldPathElements!);
        Assert.Equal("400", result.Erros[0].StatusCode);
    }

    [Fact]
    public void AggregateException_NaoEscondeInnerExceptions()
    {
        var formatter = new GoogleAdsExceptionFormatter(NullLogger<GoogleAdsExceptionFormatter>.Instance);
        var exception = new AggregateException(
            new InvalidOperationException("developer token invalido"),
            new TimeoutException("timeout ao consultar Google Ads"));

        var result = formatter.FromException(exception, "req-1");

        Assert.False(result.Sucesso);
        Assert.Equal("req-1", result.RequestId);
        Assert.Equal(2, result.Erros.Count);
        Assert.Contains(result.Erros, x => x.Mensagem.Contains("developer token", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Erros, x => x.Mensagem.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }
}
