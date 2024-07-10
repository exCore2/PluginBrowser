public static class Extensions
{
    public static string Format(this DateTime date)
    {
        return date.ToString("dd MMMM yyyy HH:mm");
    }

    public static string WrapDiscordCodeBlock(this string s)
    {
        return $"```\n{s}\n```";
    }

    public static IEnumerable<(T1?, T2?)> OuterJoinUnique<T1, T2, TK>(this IEnumerable<T1> source, IEnumerable<T2> other, Func<T1, TK> keySelector1, Func<T2, TK> keySelector2)
        where TK : notnull
    {
        var l1 = source.ToDictionary(keySelector1, x => x);
        var l2 = other.ToDictionary(keySelector2, x => x);
        return l1.Keys.Union(l2.Keys).Select(x =>
            (l1.GetValueOrDefault(x),
             l2.GetValueOrDefault(x)));
    }

    public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> source) where T : struct
    {
        return source.Select(x => (T?)x);
    }
}
