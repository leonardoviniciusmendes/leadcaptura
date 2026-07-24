namespace LeadEngine.Application.Common;

public sealed class CampaignGenerationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
