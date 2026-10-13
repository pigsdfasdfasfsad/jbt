namespace Twr.Domain.Services;
public sealed class ReceiptLedger
{
    private readonly HashSet<string> _ids=[];
    public bool TryIssue(string id)=>_ids.Add(id);
    public bool Contains(string id)=>_ids.Contains(id);
    public IReadOnlyCollection<string> Snapshot()=>_ids.ToArray();
    // These are session-scoped receipts, not stored achievement receipts.
    // Start of a NEW run may earn Wave 1 again; duplicates within a run
    // remain blocked by the ledger.
    public void ClearForNewMatch()=>_ids.Clear();
}
