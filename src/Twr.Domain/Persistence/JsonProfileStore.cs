using System.Text.Json; using Twr.Domain.Model;
namespace Twr.Domain.Persistence;
public sealed class JsonProfileStore(string path) : IProfileStore {
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
    public Profile Load(){try{return File.Exists(path)?JsonSerializer.Deserialize<Profile>(File.ReadAllText(path),Options)??new Profile():new Profile();}catch{return new Profile();}}
    public void Save(Profile profile){var full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);var tmp=full+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(profile,Options));File.Move(tmp,full,true);}
}
