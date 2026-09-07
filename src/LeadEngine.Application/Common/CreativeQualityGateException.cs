using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Common;

public sealed class CreativeQualityGateException(CreativeQualityGateResponse gate)
    : InvalidOperationException("Creative quality gate blocked approval.")
{
    public CreativeQualityGateResponse Gate { get; } = gate;
}
