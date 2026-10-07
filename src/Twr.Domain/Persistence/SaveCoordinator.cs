using Twr.Domain.Contracts.Events; using Twr.Domain.Core; using Twr.Domain.Model;
namespace Twr.Domain.Persistence;
public sealed class SaveCoordinator(IProfileStore store,EventStream events) { public void Save(Profile p,string reason,DateTimeOffset now){store.Save(p);events.Publish(new ProfileSavedEvent(reason,now));} }
