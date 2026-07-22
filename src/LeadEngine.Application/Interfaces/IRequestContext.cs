namespace LeadEngine.Application.Interfaces;

public interface IRequestContext
{
    string? IpHash { get; }
    string? UserAgent { get; }
}
