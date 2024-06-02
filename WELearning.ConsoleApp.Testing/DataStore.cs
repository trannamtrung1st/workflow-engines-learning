using WELearning.ConsoleApp.Testing.Entities;

public class DataStore
{
    private readonly Dictionary<string, EntryEntity> _entryMap;

    public DataStore()
    {
        _entryMap = new();
        _entryMap["Temperature"] = new EntryEntity("Temperature", Random.Shared.NextDouble() * 50);
        _entryMap["Humidity"] = new EntryEntity("Humidity", Random.Shared.NextDouble() * 50);
        _entryMap["Report"] = new EntryEntity("Report", null);
        _entryMap["FinalReport"] = new EntryEntity("FinalReport", null);
    }

    public EntryEntity GetEntry(string key) => _entryMap[key].Clone() as EntryEntity;

    public void UpdateEntry(string key, object value) => _entryMap[key].Value = value;
}