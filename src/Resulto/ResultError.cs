namespace Resulto;

/// <summary>
/// Represents a domain error with a category, HTTP code, message, and optional per-field validation details.
/// </summary>
/// <remarks>
/// <para>
/// <b>Equality:</b> based on <see cref="ErrorType"/> only.
/// Two errors of the same type but with different messages are considered equal.
/// This is intentional — equality answers "is it the same kind of error?", not "is it the same instance?".
/// </para>
/// <para>
/// <b>Validation errors:</b> when <see cref="ErrorType"/> equals <see cref="Resulto.ErrorType.Validation"/>,
/// <see cref="ValidationErrors"/> contains per-field details that API clients can bind to form fields.
/// </para>
/// <para>
/// <b>Custom categories:</b> use <see cref="Custom(Resulto.ErrorType,string)"/> with your own
/// <see cref="ErrorType"/> to model errors beyond the built-in set (e.g. HTTP 429).
/// </para>
/// </remarks>
public sealed record ResultError
{
    /// <summary>HTTP status code, taken from <see cref="ErrorType"/> (e.g. 404, 422, 500).</summary>
    public int Code => ErrorType.HttpStatusCode;

    /// <summary>Human-readable, localized error message suitable for displaying to end users.</summary>
    public string Message { get; }

    /// <summary>The category of this error, used for programmatic branching and HTTP status code mapping.</summary>
    public ErrorType ErrorType { get; }

    /// <summary>
    /// Per-field validation errors. Non-empty only when <see cref="ErrorType"/> equals
    /// <see cref="Resulto.ErrorType.Validation"/>. API clients use this collection to display
    /// errors next to individual form fields. Returns an empty list for non-validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    private ResultError(ErrorType errorType, string? message, IReadOnlyList<ValidationError>? validationErrors = null)
    {
        ErrorType = errorType;
        Message = message ?? errorType.ToDefaultMessage();
        ValidationErrors = validationErrors ?? [];
    }

    /// <summary>
    /// Determines equality based on <see cref="ErrorType"/> only.
    /// Two errors with the same type but different messages are considered equal
    /// because equality answers "is it the same kind of error?", not "is it the same instance?".
    /// </summary>
    public bool Equals(ResultError? other) => other is not null && ErrorType == other.ErrorType;

    /// <summary>Returns a hash code consistent with <see cref="Equals(ResultError?)"/> — based on <see cref="ErrorType"/> only.</summary>
    public override int GetHashCode() => ErrorType.GetHashCode();

    /// <summary>
    /// Creates an error of an arbitrary <paramref name="errorType"/>.
    /// Use this with a custom <see cref="ErrorType"/> to model categories beyond the built-in set.
    /// </summary>
    /// <param name="errorType">The error category (built-in or custom).</param>
    /// <param name="message">Optional custom message. Falls back to the type's localized default if null.</param>
    public static ResultError Custom(ErrorType errorType, string? message = null) => new(errorType, message);

    /// <summary>Creates a 400 Bad Request error.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError BadRequest(string? message = null) => new(ErrorType.BadRequest, message);

    /// <summary>Creates a 404 Not Found error.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError NotFound(string? message = null) => new(ErrorType.NotFound, message);

    /// <summary>Creates a 409 Conflict error. Use for duplicates, version mismatches, and state conflicts.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError Conflict(string? message = null) => new(ErrorType.Conflict, message);

    /// <summary>Creates a 403 Forbidden error.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError Forbidden(string? message = null) => new(ErrorType.Forbidden, message);

    /// <summary>Creates a 401 Unauthorized error.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError Unauthorized(string? message = null) => new(ErrorType.Unauthorized, message);

    /// <summary>Creates a 415 Unsupported Media Type error.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError UnsupportedMediaType(string? message = null) => new(ErrorType.UnsupportedMediaType, message);

    /// <summary>Creates a 500 Internal Server Error.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError InternalServerError(string? message = null) => new(ErrorType.InternalServerError, message);

    /// <summary>Creates a 422 Validation error with a summary message but no per-field details.</summary>
    /// <param name="message">Optional custom message. Falls back to the localized default if null.</param>
    public static ResultError Validation(string? message = null) => new(ErrorType.Validation, message);

    /// <summary>
    /// Creates a 422 Validation error with per-field details.
    /// This is the preferred overload — it lets API clients bind errors to individual form fields.
    /// </summary>
    /// <param name="errors">The list of per-field validation errors. Must not be null.</param>
    public static ResultError Validation(IReadOnlyList<ValidationError> errors)
    {
        Guard.NotNull(errors);
        return new(ErrorType.Validation, ErrorType.Validation.ToDefaultMessage(), errors);
    }

    /// <summary>
    /// Creates a 422 Validation error with both a custom summary message and per-field details.
    /// Use when you need a human-readable summary alongside structured field errors.
    /// </summary>
    /// <param name="message">A summary message describing the overall validation failure.</param>
    /// <param name="errors">The list of per-field validation errors. Must not be null.</param>
    public static ResultError Validation(string message, IReadOnlyList<ValidationError> errors)
    {
        Guard.NotNull(errors);
        return new(ErrorType.Validation, message, errors);
    }
}
