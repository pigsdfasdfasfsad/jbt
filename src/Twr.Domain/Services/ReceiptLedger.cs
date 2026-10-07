namespace Twr.Domain.Services;
public sealed class ReceiptLedger { private readonly HashSet<string> _ids=[]; public bool TryIssue(string id)=>_ids.Add(id); public bool Contains(string id)=>_ids.Contains(id); public IReadOnlyCollection<string> Snapshot()=>_ids.ToArray(); }
