namespace Bramki_Evacuation.Services;

public sealed class DashboardOptions
{
    public int OnsiteZoneId { get; set; }
    public int MusterZoneId { get; set; }
    public int RefreshMs { get; set; } = 1000;
}