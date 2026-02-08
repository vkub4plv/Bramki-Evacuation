namespace Bramki_Evacuation.Dashboard;

public sealed class EvacStore
{
    private readonly object _gate = new();
    private EvacSnapshot _snapshot = new(
        LastSuccessUtc: DateTimeOffset.MinValue,
        IsStale: true,
        Error: "Not started",
        Evacuated: Array.Empty<PersonRow>(),
        StillOnsite: Array.Empty<PersonRow>());

    public event Action? Changed;

    public EvacSnapshot Snapshot { get { lock (_gate) return _snapshot; } }

    public void Set(EvacSnapshot snap)
    {
        lock (_gate) _snapshot = snap;
        Changed?.Invoke();
    }
}