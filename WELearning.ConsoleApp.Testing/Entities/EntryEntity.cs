namespace WELearning.ConsoleApp.Testing.Entities;

public class EntryEntity
{
    public EntryEntity(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; }
    public object Value { get; set; }

    public override string ToString() => $"{Key}: {Value}";
}