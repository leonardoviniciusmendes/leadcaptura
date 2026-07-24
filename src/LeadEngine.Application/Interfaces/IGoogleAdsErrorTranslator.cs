using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface IGoogleAdsErrorTranslator
{
    GoogleAdsPublicationErrorDto Translate(Exception exception, string? requestId = null);
    GoogleAdsPublicationErrorDto Translate(string code, string message, string? operation, int? index, string? field, string? rejectedValue, string? requestId);
}
