namespace Resulto.Resources;

/// <summary>
/// Centralized, type-safe access to the library's localized strings.
/// Resource keys are resolved from method names via <see cref="ResourceManagerExtensions.Get"/>.
/// Supported cultures: English (default), Russian (ru), Tajik (tg).
/// </summary>
internal static class Messages
{
    private static readonly ResourceManager Resources = new(typeof(Messages).FullName!, typeof(Messages).Assembly);

    /// <summary>Exception messages thrown by the library.</summary>
    public static class Exception
    {
        /// <summary>Thrown when the caller accesses <c>Error</c> on a successful result.</summary>
        public static string CannotAccessErrorOnSuccess() => Resources.Get().Format();

        /// <summary>Thrown when the caller accesses <c>Value</c> on a failed result.</summary>
        public static string ResultIsFailure() => Resources.Get().Format();

        /// <summary>Thrown when converting a successful <c>BaseResult</c> to a typed result without a value.</summary>
        public static string CannotConvertSuccessBaseResultToTypedResult(string responseType) =>
            Resources.Get().Format(responseType);
    }

    /// <summary>Default messages for error types.</summary>
    public static class ResultError
    {
        /// <summary>
        /// Resolves the localized default message for an error type by its name.
        /// Falls back to the name itself when no resource entry exists (e.g. custom error types).
        /// </summary>
        public static string ForType(string name) =>
            Resources.GetString(name, CultureInfo.CurrentCulture) ?? name;
    }
}
