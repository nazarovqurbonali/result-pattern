using Resulto;
using Resulto.AspNetCore;
using System.Collections.Concurrent;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

// A trivial in-memory store standing in for a real data source.
ConcurrentDictionary<int, User> users = new();
int nextId = 0;

// GET: 200 with the user, or 404 ProblemDetails.
app.MapGet("/users/{id:int}", (int id) =>
    GetUser(id).ToHttpResult());

// POST: 201 Created, or 422 with per-field validation errors.
app.MapPost("/users", (CreateUser cmd) =>
{
    Result<User> result = CreateUserHandler(cmd);
    return result.ToHttpCreated(uri: result.IsSuccess ? $"/users/{result.Value.Id}" : null);
});

// DELETE: 204 No Content, or 404 ProblemDetails.
app.MapDelete("/users/{id:int}", (int id) =>
    DeleteUser(id).ToHttpResult());

// Demonstrates a CUSTOM error type (429) mapped via a per-call mapper.
app.MapGet("/limited", () =>
    Result<string>.Failure(ResultError.Custom(RateLimited, "Slow down."))
        .ToHttpResult(onError: err => err.ErrorType == RateLimited
            ? Results.StatusCode(err.Code)
            : ResultHttp.ProblemDetails(err)));

app.Run();
return;

// ── Domain logic returning Result instead of throwing ─────────────────────────

Result<User> GetUser(int id) =>
    users.TryGetValue(id, out User? user)
        ? user
        : ResultError.NotFound($"User {id} was not found.");

Result<User> CreateUserHandler(CreateUser cmd)
{
    List<ValidationError> errors = [];
    if (string.IsNullOrWhiteSpace(cmd.Name))
        errors.Add(new("Name", ValidationCodes.Required, "Name is required."));
    if (string.IsNullOrWhiteSpace(cmd.Email) || !cmd.Email.Contains('@'))
        errors.Add(new("Email", ValidationCodes.InvalidEmail, "A valid email is required."));

    if (errors.Count > 0)
        return ResultError.Validation(errors);

    // Validated above, so the nullable fields are known to be present here.
    User created = new(Interlocked.Increment(ref nextId), cmd.Name!, cmd.Email!);
    users[created.Id] = created;
    return created;
}

BaseResult DeleteUser(int id) =>
    users.TryRemove(id, out _)
        ? BaseResult.Success()
        : ResultError.NotFound($"User {id} was not found.");

// A custom error category — note: declared by the app, not the library.
public partial class Program
{
    private static readonly ErrorType RateLimited = new("RateLimited", 429);
}

public record User(int Id, string Name, string Email);
public record CreateUser(string? Name, string? Email);
