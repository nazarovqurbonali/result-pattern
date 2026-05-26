namespace Resulto;

/// <summary>
/// Extension helpers for <see cref="ErrorType"/> that resolve a localized default message.
/// </summary>
public static class ErrorTypeExtensions
{
    /// <summary>
    /// Per-culture cache of default messages.
    /// Key: (error type name, culture name). Value: localized message.
    /// First request for a (name, culture) pair populates the cache; subsequent reads are lock-free.
    /// </summary>
    private static readonly ConcurrentDictionary<(string Name, string Culture), string> MessageCache = new();

    /// <summary>
    /// Returns the localized default message for this error type using the current thread's
    /// culture (<see cref="CultureInfo.CurrentCulture"/>). Results are cached per (name, culture) pair.
    /// </summary>
    /// <remarks>
    /// Built-in error types resolve to a localized string (English, Russian, Tajik shipped).
    /// Custom error types fall back to their <see cref="ErrorType.Name"/> when no resource entry exists.
    /// </remarks>
    public static string ToDefaultMessage(this ErrorType errorType)
    {
        string cultureName = CultureInfo.CurrentCulture.Name;
        return MessageCache.GetOrAdd(
            (errorType.Name, cultureName),
            static key => Messages.ResultError.ForType(key.Name));
    }
}
