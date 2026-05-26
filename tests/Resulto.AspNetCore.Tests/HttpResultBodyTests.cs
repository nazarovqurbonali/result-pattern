using IHttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Resulto.AspNetCore.Tests;

/// <summary>
/// Executes the produced <see cref="IHttpResult"/> against a real <see cref="HttpContext"/>
/// and inspects the serialized response body — not just the status code.
/// </summary>
public class HttpResultBodyTests
{
    private static async Task<(int Status, JsonElement Body)> ExecuteAsync(IHttpResult result)
    {
        ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();
        DefaultHttpContext ctx = new() { RequestServices = services };
        using MemoryStream stream = new();
        ctx.Response.Body = stream;

        await result.ExecuteAsync(ctx);

        stream.Position = 0;
        JsonDocument doc = await JsonDocument.ParseAsync(stream);
        return (ctx.Response.StatusCode, doc.RootElement.Clone());
    }

    [Fact]
    public async Task Error_body_carries_message_and_status()
    {
        IHttpResult http = Result<int>.Failure(ResultError.NotFound("user 42 missing")).ToHttpResult();

        (int status, JsonElement body) = await ExecuteAsync(http);

        Assert.Equal(404, status);
        Assert.Equal("user 42 missing", body.GetProperty("detail").GetString());
        Assert.Equal(404, body.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Validation_body_carries_per_field_errors()
    {
        ResultError error = ResultError.Validation(
        [
            new("Email", ValidationCodes.Required, "Email is required."),
            new("Email", ValidationCodes.InvalidEmail, "Email is invalid."),
            new("Age", ValidationCodes.OutOfRange, "Age must be 18 or older."),
        ]);

        IHttpResult http = Result<int>.Failure(error).ToHttpResult();

        (int status, JsonElement body) = await ExecuteAsync(http);

        Assert.Equal(422, status);
        JsonElement errors = body.GetProperty("errors");

        JsonElement email = errors.GetProperty("Email");
        Assert.Equal(2, email.GetArrayLength());
        Assert.Equal("Email is required.", email[0].GetString());
        Assert.Equal("Email is invalid.", email[1].GetString());

        Assert.Equal("Age must be 18 or older.", errors.GetProperty("Age")[0].GetString());
    }

    [Fact]
    public async Task Success_body_carries_value()
    {
        IHttpResult http = Result<int>.Success(99).ToHttpResult();

        (int status, JsonElement body) = await ExecuteAsync(http);

        Assert.Equal(200, status);
        Assert.Equal(99, body.GetInt32());
    }
}
