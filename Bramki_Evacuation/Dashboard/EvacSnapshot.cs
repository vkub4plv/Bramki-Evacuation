namespace Bramki_Evacuation.Dashboard;

public sealed record PersonRow(int PersonId, string DisplayName);

public sealed record EvacSnapshot(
    DateTimeOffset LastSuccessUtc,
    bool IsStale,
    string? Error,
    PersonRow[] Evacuated,
    PersonRow[] StillOnsite
)
{
    public int EvacuatedCount => Evacuated.Length;
    public int StillOnsiteCount => StillOnsite.Length;
}