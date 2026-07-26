namespace Osmalyzer;

public static class LinqExtensions
{
    public static IEnumerable<T> Except<T>(this IList<T> source, T other)
    {
        foreach (T item in source)
            if (!EqualityComparer<T>.Default.Equals(item, other))
                yield return item;
    }
}