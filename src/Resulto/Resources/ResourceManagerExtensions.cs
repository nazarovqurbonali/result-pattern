namespace Resulto.Resources;

/// <summary>
/// Lightweight extensions for <see cref="ResourceManager"/> that replicate the
/// DotNext <c>Get().Format()</c> pattern using <see cref="CallerMemberNameAttribute"/>
/// to auto-resolve resource keys from the calling method name.
/// </summary>
internal static class ResourceManagerExtensions
{
    /// <summary>
    /// Retrieves a localized string from the resource manager using the caller's method name as the key.
    /// Returns the key itself if the resource is not found (fail-safe, never throws).
    /// </summary>
    /// <param name="manager">The resource manager to query.</param>
    /// <param name="key">
    /// Autopopulated by the compiler from the calling method's name.
    /// For example, calling <c>Resources.Get()</c> inside <c>ResultIsFailure()</c>
    /// will look up the key <c>"ResultIsFailure"</c>.
    /// </param>
    /// <returns>The localized string for the current culture, or the raw key if not found.</returns>
    public static string Get(this ResourceManager manager, [CallerMemberName] string key = "")
        => manager.GetString(key, CultureInfo.CurrentCulture) ?? key;

    /// <summary>
    /// Formats a template string with the given arguments using the current culture.
    /// If no arguments are provided, returns the template as-is (no allocation).
    /// </summary>
    /// <param name="template">The format string (e.g. "Circuit '{0}' failure #{1}.").</param>
    /// <param name="args">The values to substitute into the placeholders.</param>
    /// <returns>The formatted string.</returns>
    public static string Format(this string template, params object?[]? args)
        => args is null || args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, args);
}