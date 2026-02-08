namespace Bramki_Evacuation.Wcf;

public sealed class ApiOptions
{
    public string BaseUrl { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 20;
    public int KeepAliveSeconds { get; set; } = 30;

    public ServiceAccountOptions ServiceAccount { get; set; } = new();
}

public sealed class ServiceAccountOptions
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}