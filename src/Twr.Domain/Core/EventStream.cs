using Twr.Domain.Contracts;
namespace Twr.Domain.Core;
public sealed class EventStream { private readonly List<IGameEvent> _events=[]; public void Publish(IGameEvent e)=>_events.Add(e); public IReadOnlyList<IGameEvent> Drain(){var x=_events.ToArray();_events.Clear();return x;} }
