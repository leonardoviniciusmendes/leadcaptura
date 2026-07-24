namespace LeadEngine.Application.Services;

public sealed class LeadCaptureOptions
{
    public string ConsentVersion { get; set; } = "1.0";
    public int MinimumFormSeconds { get; set; } = 2;
    public int MaxLeadsPerIpPerHour { get; set; } = 10;
    public int DuplicateWindowHours { get; set; } = 24;
}
