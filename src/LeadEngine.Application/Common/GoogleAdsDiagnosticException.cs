using LeadEngine.Application.DTOs;

namespace LeadEngine.Application.Common;

public sealed class GoogleAdsDiagnosticException(GoogleAdsDiagnosticResponse diagnostic, Exception? innerException = null)
    : Exception(diagnostic.Mensagem, innerException)
{
    public GoogleAdsDiagnosticResponse Diagnostic { get; } = diagnostic;
}
