using Twr.Domain.Model; namespace Twr.Domain.Persistence; public interface IProfileStore { Profile Load(); void Save(Profile profile); }
