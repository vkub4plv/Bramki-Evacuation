using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Bramki_Evacuation.Dashboard;
using Bramki_Evacuation.Wcf;

namespace Bramki_Evacuation.Services;

public sealed class EvacPoller : BackgroundService
{
    private readonly IntegrationFacade _api;
    private readonly EvacStore _store;
    private readonly DashboardOptions _opt;
    private readonly ILogger<EvacPoller> _log;

    public EvacPoller(
        IntegrationFacade api,
        EvacStore store,
        IOptions<DashboardOptions> opt,
        ILogger<EvacPoller> log)
    {
        _api = api;
        _store = store;
        _opt = opt.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refresh = TimeSpan.FromMilliseconds(Math.Max(250, _opt.RefreshMs));
        var timer = new PeriodicTimer(refresh);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var onsite = await _api.GetZonePeopleAsync(_opt.OnsiteZoneId, stoppingToken);
                var evac = await _api.GetZonePeopleAsync(_opt.MusterZoneId, stoppingToken);

                var evacIds = evac.Select(p => p.ID).ToHashSet();
                var still = onsite.Where(p => !evacIds.Contains(p.ID)).ToArray();

                static PersonRow Map(Integration.PersonData p)
                {
                    static string Clean(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

                    var name = Clean(p.Name);
                    var fn = Clean(p.FirstName);
                    var ln = Clean(p.LastName);

                    // Normalize for comparison (ignore spaces and case)
                    static string Normalize(string s) =>
                        new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray())
                            .ToLowerInvariant();

                    var fullName = $"{fn} {ln}".Trim();

                    string display;

                    if (!string.IsNullOrEmpty(name) &&
                        Normalize(name) == Normalize(fullName))
                    {
                        display = fullName.Length > 0 ? fullName : name;
                    }
                    else
                    {
                        display = string.Join(" ", new[] { name, fn, ln }.Where(x => x.Length > 0));
                    }

                    if (string.IsNullOrWhiteSpace(display))
                        display = $"PersonID={p.ID}";

                    return new PersonRow(p.ID, display);
                }

                _store.Set(new EvacSnapshot(
                    LastSuccessUtc: DateTimeOffset.UtcNow,
                    IsStale: false,
                    Error: null,
                    Evacuated: evac.Select(Map).OrderBy(x => x.DisplayName).ToArray(),
                    StillOnsite: still.Select(Map).OrderBy(x => x.DisplayName).ToArray()
                ));
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _log.LogWarning(ex, "Poll failed; keeping last known data.");
                var prev = _store.Snapshot;
                _store.Set(prev with { IsStale = true, Error = ex.Message });
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}