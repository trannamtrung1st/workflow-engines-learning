using WELearning.ConsoleApp.Testing.Framework.Abstracts;
using WELearning.ConsoleApp.Testing.ValueObjects;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class EntryBinding : ReadWriteBinding, IEntryBinding
{
    private readonly DataStore _dataStore;
    private new readonly EntryValueObject _valueObject;

    public EntryBinding(string name, EntryValueObject valueObject, DataStore dataStore) : base(name, valueObject)
    {
        _valueObject = valueObject;
        _dataStore = dataStore;
    }

    public string EntryKey => _valueObject.EntryKey;

    public string Prepend(string otherEntryName)
    {
        var otherEntry = _dataStore.GetEntry(otherEntryName);
        return $"{otherEntry.Value}{_valueObject.Value}";
    }

    public string Append(string otherEntryName)
    {
        var otherEntry = _dataStore.GetEntry(otherEntryName);
        return $"{_valueObject.Value}{otherEntry.Value}";
    }
}

public class ReadEntryBinding : IReadBinding, IEntryBinding
{
    private readonly EntryBinding _entryBinding;
    public ReadEntryBinding(string name, EntryValueObject valueObject, DataStore dataStore)
    {
        _entryBinding = new EntryBinding(name, valueObject, dataStore);
    }

    public object Value => _entryBinding.Value;
    public bool IsNumeric => _entryBinding.IsNumeric;
    public string Name => _entryBinding.Name;
    public bool ValueSet => _entryBinding.ValueSet;
    public string EntryKey => _entryBinding.EntryKey;

    public double AsDouble() => _entryBinding.AsDouble();
    public int AsInt() => _entryBinding.AsInt();
    public string Prepend(string otherEntry) => _entryBinding.Prepend(otherEntry);
    public string Append(string otherEntry) => _entryBinding.Append(otherEntry);
}