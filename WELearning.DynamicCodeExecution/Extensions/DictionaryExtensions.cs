namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static void AssignTo<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IDictionary<TKey, TValue> target)
        {
            foreach (var kvp in source)
                target[kvp.Key] = kvp.Value;
        }
    }
}
