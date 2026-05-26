using IHttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Resulto.AspNetCore;

/// <summary>
/// Extension methods that convert <see cref="Result{T}"/> and <see cref="BaseResult"/>
/// into ASP.NET Core HTTP responses.
/// </summary>
/// <remarks>
/// On success, they return <c>Ok</c> / <c>Created</c> / <c>NoContent</c>. On failure, they delegate to a
/// <see cref="ResultErrorMapper"/>: the one passed to the call, or <see cref="ResultHttp.DefaultErrorMapper"/>
/// when none is given. See <see cref="ResultHttp"/> for how to customize error rendering.
/// </remarks>
public static class ResultExtensions
{
    /// <summary>Converts a <see cref="Result{T}"/> to 200 OK with the value, or an error response.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="onError">Optional error mapper for this call; defaults to <see cref="ResultHttp.DefaultErrorMapper"/>.</param>
    public static IHttpResult ToHttpResult<T>(this Result<T> result, ResultErrorMapper? onError = null)
        => result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error, onError);

    /// <summary>
    /// Converts a <see cref="BaseResult"/> to 204 No Content, or an error response.
    /// Use for operations that return no data on success (DELETE, UPDATE, void commands).
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="onError">Optional error mapper for this call; defaults to <see cref="ResultHttp.DefaultErrorMapper"/>.</param>
    public static IHttpResult ToHttpResult(this BaseResult result, ResultErrorMapper? onError = null)
        => result.IsSuccess ? Results.NoContent() : MapError(result.Error, onError);

    /// <summary>
    /// Converts a <see cref="Result{T}"/> to 201 Created with the value and an optional location URI,
    /// or an error response. Use for POST endpoints that create new resources.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="uri">Optional location URI for the created resource (Location header).</param>
    /// <param name="onError">Optional error mapper for this call; defaults to <see cref="ResultHttp.DefaultErrorMapper"/>.</param>
    public static IHttpResult ToHttpCreated<T>(this Result<T> result, string? uri = null,
        ResultErrorMapper? onError = null)
        => result.IsSuccess ? Results.Created(uri, result.Value) : MapError(result.Error, onError);

    private static IHttpResult MapError(ResultError error, ResultErrorMapper? onError)
        => (onError ?? ResultHttp.DefaultErrorMapper)(error);
}
