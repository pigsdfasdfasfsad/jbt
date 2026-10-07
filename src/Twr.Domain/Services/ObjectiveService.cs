using Twr.Domain.Contracts.Events; using Twr.Domain.Core; using Twr.Domain.Model;
namespace Twr.Domain.Services;
public sealed class ObjectiveService(EventStream events) { public bool Complete(MatchState m,string id,DateTimeOffset now){if(!m.CompletedObjectives.Add(id))return false;events.Publish(new ObjectiveCompletedEvent(id,now));return true;} }
