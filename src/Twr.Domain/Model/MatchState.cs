namespace Twr.Domain.Model;
public sealed class MatchState { public string MapName {get;set;}=""; public int Wave {get;set;}=0; public MatchPhase Phase {get;set;}=MatchPhase.Lobby; public bool CompletionAwarded {get;set;}=false; public bool SaveCommitted {get;set;}=false; public HashSet<string> CompletedObjectives {get;}=[]; }
