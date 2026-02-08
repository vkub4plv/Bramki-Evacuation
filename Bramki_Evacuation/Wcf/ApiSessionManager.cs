using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bramki_Evacuation.Wcf;

public interface IApiSessionProvider
{
    ValueTask<Guid> GetTokenAsync(CancellationToken ct = default);
    ValueTask<Guid> RefreshTokenAsync(CancellationToken ct = default);
}

public sealed class ApiSessionManager : BackgroundService, IApiSessionProvider
{
    private readonly ApiClientFactory _clients;
    private readonly ApiOptions _opt;
    private readonly ILogger<ApiSessionManager> _log;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private Guid _token;

    public ApiSessionManager(ApiClientFactory clients, IOptions<ApiOptions> opt, ILogger<ApiSessionManager> log)
    {
        _clients = clients;
        _opt = opt.Value;
        _log = log;
    }

    public async ValueTask<Guid> GetTokenAsync(CancellationToken ct = default)
    {
        if (_token != Guid.Empty) return _token;
        await EnsureConnectedAsync(ct);
        return _token;
    }

    public async ValueTask<Guid> RefreshTokenAsync(CancellationToken ct = default)
    {
        await ReconnectAsync(ct);
        return _token;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await EnsureConnectedAsync(stoppingToken); break; }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _log.LogWarning(ex, "Initial connect failed; retrying in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        var keepAlive = TimeSpan.FromSeconds(Math.Clamp(_opt.KeepAliveSeconds, 5, 300));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(keepAlive, stoppingToken);

                if (_token == Guid.Empty)
                {
                    await EnsureConnectedAsync(stoppingToken);
                    continue;
                }

                using var sm = _clients.CreateSession();
                var op = await sm.GetOperatorBySessionAsync(_token);

                if (op is null || string.IsNullOrWhiteSpace(op.Login))
                    await ReconnectAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _log.LogWarning(ex, "Keepalive failed; reconnecting.");
                await ReconnectAsync(stoppingToken);
            }
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_token != Guid.Empty) return;

            using var sm = _clients.CreateSession();
            _token = await sm.ConnectAsync(_opt.ServiceAccount.Login, _opt.ServiceAccount.Password);
            _log.LogInformation("Connected. Token={Token}", _token);
        }
        finally { _lock.Release(); }
    }

    private async Task ReconnectAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await DisconnectSafeAsync();
            using var sm = _clients.CreateSession();
            _token = await sm.ConnectAsync(_opt.ServiceAccount.Login, _opt.ServiceAccount.Password);
            _log.LogInformation("Reconnected. Token={Token}", _token);
        }
        finally { _lock.Release(); }
    }

    private async Task DisconnectSafeAsync()
    {
        try
        {
            if (_token == Guid.Empty) return;
            using var sm = _clients.CreateSession();
            await sm.DisconnectAsync(_token);
        }
        catch { /* ignore */ }
        finally { _token = Guid.Empty; }
    }
}