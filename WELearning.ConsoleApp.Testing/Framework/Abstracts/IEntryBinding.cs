namespace WELearning.ConsoleApp.Testing.Framework.Abstracts;

public interface IEntryBinding
{
    string Prepend(string otherEntry);
    string Append(string otherEntry);
    string EntryKey { get; }
}
