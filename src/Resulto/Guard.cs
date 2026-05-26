namespace Resulto;

/// <summary>
/// Internal argument guards. Used instead of <c>ArgumentNullException.ThrowIfNull</c> so the same
/// source compiles on netstandard2.0 (where that helper does not exist).
/// </summary>
internal static class Guard
{
    [StackTraceHidden]
    public static void NotNull([NotNull] object? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null) throw new ArgumentNullException(paramName);
    }
}