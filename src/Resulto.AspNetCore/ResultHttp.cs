using IHttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Resulto.AspNetCore;

/// <summary>
/// Converts a <see cref="ResultError"/> into an ASP.NET Core HTTP response.
/// Implement your own to fully control how failures are rendered.
/// </summary>
/// <param name="error">The error to render.</param>
/// <returns>The HTTP response representing the error.</returns>
public delegate IHttpResult ResultErrorMapper(ResultError error);

/// <summary>
/// Extension point for how <see cref="Result{T}"/> and <see cref="BaseResult"/> failures
/// are mapped to HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// Out of the box, errors are rendered as RFC 7807 ProblemDetails via <see cref="ProblemDetails"/>.
/// You can customize this in three ways, from least to most invasive:
/// </para>
/// <list type="number">
/// <item><b>Per call</b> — pass a <see cref="ResultErrorMapper"/> to
/// <c>ToHttpResult(onError)</c> for a single endpoint.</item>
/// <item><b>Globally</b> — assign <see cref="DefaultErrorMapper"/> once at startup to change
/// the default for every endpoint.</item>
/// <item><b>Compose</b> — call <see cref="ProblemDetails"/> from your own mapper to handle a few
/// custom error types and fall back to the built-in behavior for the rest.</item>
/// </list>
/// <example>
/// <code>
/// // Startup: add a Retry-After header for a custom 429 error, keep defaults otherwise.
/// ResultHttp.DefaultErrorMapper = error => error.ErrorType == AppErrors.RateLimited
///     ? Results.StatusCode(429)
///     : ResultHttp.ProblemDetails(error);
/// </code>
/// </example>
/// </remarks>
public static class ResultHttp
{
    /// <summary>
    /// The mapper used by <c>ToHttpResult</c> / <c>ToHttpCreated</c> when no per-call mapper is given.
    /// Defaults to <see cref="ProblemDetails"/>. Assign once at application startup.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when set to <see langword="null"/>.</exception>
    public static ResultErrorMapper DefaultErrorMapper
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = ProblemDetails;

    /// <summary>
    /// The built-in mapper: renders the error as RFC 7807 ProblemDetails, taking the status code
    /// directly from <see cref="ResultError.Code"/> and emitting per-field details for validation errors.
    /// Public so custom mappers can compose with it.
    /// </summary>
    public static IHttpResult ProblemDetails(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.ErrorType == ErrorType.Validation
            ? ValidationProblem(error)
            : Results.Problem(detail: error.Message, statusCode: error.Code);
    }

    private static IHttpResult ValidationProblem(ResultError error)
    {
        if (error.ValidationErrors.Count == 0)
            return Results.Problem(detail: error.Message, statusCode: StatusCodes.Status422UnprocessableEntity);

        Dictionary<string, List<string>> grouped = new(error.ValidationErrors.Count);
        foreach (ValidationError validationError in error.ValidationErrors)
        {
            if (!grouped.TryGetValue(validationError.Field, out List<string>? list))
            {
                list = new List<string>(1);
                grouped[validationError.Field] = list;
            }

            list.Add(validationError.Message);
        }

        Dictionary<string, string[]> errors = new(grouped.Count);
        foreach (KeyValuePair<string, List<string>> pair in grouped)
            errors[pair.Key] = pair.Value.ToArray();

        return Results.ValidationProblem(
            errors,
            detail: error.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }
}
