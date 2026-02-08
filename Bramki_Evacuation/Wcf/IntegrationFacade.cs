using Microsoft.Extensions.Logging;

namespace Bramki_Evacuation.Wcf;

public sealed class IntegrationFacade
{
    private readonly ApiClientFactory _clients;
    private readonly IApiSessionProvider _session;
    private readonly ILogger<IntegrationFacade> _log;

    public IntegrationFacade(ApiClientFactory clients, IApiSessionProvider session, ILogger<IntegrationFacade> log)
    {
        _clients = clients;
        _session = session;
        _log = log;
    }

    public async Task<IReadOnlyList<Integration.PersonData>> GetZonePeopleAsync(int zoneId, CancellationToken ct = default)
    {
        var token = await _session.GetTokenAsync(ct);

        try
        {
            using var integ = _clients.CreateIntegration();
            var arr = await integ.GetAttendanceZoneOccupanciesAsync(zoneId, token);
            return arr ?? Array.Empty<Integration.PersonData>();
        }
        catch (Exception ex) when (LooksLikeSessionExpired(ex))
        {
            _log.LogWarning(ex, "Session invalid; refreshing token and retrying zone read.");
            var token2 = await _session.RefreshTokenAsync(ct);

            using var integ = _clients.CreateIntegration();
            var arr = await integ.GetAttendanceZoneOccupanciesAsync(zoneId, token2);
            return arr ?? Array.Empty<Integration.PersonData>();
        }
    }

    private static bool LooksLikeSessionExpired(Exception ex)
    {
        var s = ex.ToString();
        return s.Contains("session", StringComparison.OrdinalIgnoreCase) &&
               (s.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("token", StringComparison.OrdinalIgnoreCase));
    }
}