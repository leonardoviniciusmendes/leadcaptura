using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Interfaces;

public interface ICreativeAssetAnalysisProvider
{
    Task<CreativeAssetAnalysisProviderResult> AnalyzeAsync(CreativeAssetAnalysisProviderRequest request, CancellationToken cancellationToken);
}
