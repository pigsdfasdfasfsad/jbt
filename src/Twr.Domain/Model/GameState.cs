namespace Twr.Domain.Model;
public sealed class GameState { public MatchState Match {get;}=new(); public PlayerState Player {get;}=new(); public string SelectedMap {get;set;}="Ranch"; }
