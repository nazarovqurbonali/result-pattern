namespace Resulto;

/// <summary>
/// Classifies a domain error into a category that carries its own HTTP status code.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a closed <c>enum</c>, this type is <b>open for extension</b>: any consumer can
/// declare its own error categories without forking the library. Define them as
/// <c>static readonly</c> fields and use them anywhere a built-in <see cref="ErrorType"/> is accepted.
/// </para>
/// <para>
/// <b>Equality</b> is structural (by <see cref="Name"/> and <see cref="HttpStatusCode"/>),
/// so two categories with the same name and code are equal regardless of where they were declared.
/// </para>
/// <example>
/// Adding a custom category for HTTP 429:
/// <code>
/// public static class MyErrors
/// {
///     public static readonly ErrorType RateLimited = new("RateLimited", 429);
/// }
///
/// return ResultError.Custom(MyErrors.RateLimited, "Slow down.");
/// </code>
/// </example>
/// </remarks>
public readonly record struct ErrorType
{
    /// <summary>A short, machine-readable name for this category (e.g. "NotFound", "RateLimited").</summary>
    public string Name { get; }

    /// <summary>The HTTP status code this category maps to (e.g. 404, 422, 500).</summary>
    public int HttpStatusCode { get; }

    /// <summary>Creates an error category with the given name and HTTP status code.</summary>
    /// <param name="name">A short, machine-readable name. Must not be null or whitespace.</param>
    /// <param name="httpStatusCode">The HTTP status code this category maps to.</param>
    public ErrorType(string name, int httpStatusCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Error type name must not be null or whitespace.", nameof(name));

        Name = name;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>400 — the request is malformed or contains invalid data.</summary>
    public static readonly ErrorType BadRequest = new("BadRequest", 400);

    /// <summary>401 — the caller is not authenticated.</summary>
    public static readonly ErrorType Unauthorized = new("Unauthorized", 401);

    /// <summary>403 — the caller is authenticated but lacks permission.</summary>
    public static readonly ErrorType Forbidden = new("Forbidden", 403);

    /// <summary>404 — the requested resource does not exist.</summary>
    public static readonly ErrorType NotFound = new("NotFound", 404);

    /// <summary>409 — the request conflicts with the current state (duplicate, version mismatch, etc.).</summary>
    public static readonly ErrorType Conflict = new("Conflict", 409);

    /// <summary>415 — the request body uses an unsupported content type.</summary>
    public static readonly ErrorType UnsupportedMediaType = new("UnsupportedMediaType", 415);

    /// <summary>422 — one or more fields failed validation rules.</summary>
    public static readonly ErrorType Validation = new("Validation", 422);

    /// <summary>500 — an unexpected internal error occurred.</summary>
    public static readonly ErrorType InternalServerError = new("InternalServerError", 500);

    /// <summary>Returns <see cref="Name"/> for diagnostics and logging.</summary>
    public override string ToString() => Name;
}
