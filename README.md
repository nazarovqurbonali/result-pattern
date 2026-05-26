# Resulto

A lightweight, dependency-free **Result pattern** for .NET. Model success and failure
*explicitly* instead of throwing exceptions for expected error cases — then chain, transform,
and map your results with a small, predictable API.

[![CI](https://github.com/nazarovqurbonali/result-pattern/actions/workflows/ci.yml/badge.svg)](https://github.com/nazarovqurbonali/result-pattern/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Resulto.svg)](https://www.nuget.org/packages/Resulto)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

```csharp
Result<User> result = await GetUserAsync(id)
    .Ensure(u => u.IsActive, ResultError.Forbidden("Account is disabled"))
    .Map(u => u.ToDto());

return result.Match(
    onSuccess: dto => Ok(dto),
    onFailure: err => Problem(err.Message, statusCode: err.Code));
```

## Why

Exceptions are for the *exceptional* — not for "user not found" or "email already taken".
Those are ordinary outcomes your callers should handle. `Resulto` makes the failure path part
of the type signature, so the compiler reminds you it exists.

- **Zero dependencies** in the core package — works anywhere (`netstandard2.0` + `net8.0`).
- **Extensible error categories** — add your own error types (e.g. HTTP 429) without forking.
- **Structured validation errors** — per-field details that bind cleanly to API clients.
- **Optional ASP.NET Core integration** — map results to RFC 7807 ProblemDetails in one call.
- **Localized default messages** — English, Russian, and Tajik ship in the box.

## Packages

| Package | Target | Purpose |
| --- | --- | --- |
| [`Resulto`](https://www.nuget.org/packages/Resulto) | `netstandard2.0`, `net8.0` | Core `Result`/`BaseResult` pattern. No dependencies. |
| [`Resulto.AspNetCore`](https://www.nuget.org/packages/Resulto.AspNetCore) | `net8.0` | Converts results to HTTP responses (ProblemDetails). |

```bash
dotnet add package Resulto
dotnet add package Resulto.AspNetCore   # only if you build web APIs
```

## Core concepts

### `Result<T>` — an operation that returns a value

```csharp
Result<int> Parse(string s) =>
    int.TryParse(s, out int n)
        ? n                                  // implicit success
        : ResultError.BadRequest("Not a number");   // implicit failure

Result<int> r = Parse("42");
if (r.IsSuccess) Console.WriteLine(r.Value);   // 42
```

`Value` throws if the result is a failure, and `Error` throws if it is a success — so you always
go through `IsSuccess`/`IsFailure` or `Match`.

### `BaseResult` — an operation that returns nothing

```csharp
BaseResult Delete(Guid id)
{
    if (!Exists(id)) return ResultError.NotFound();
    Remove(id);
    return BaseResult.Success();   // cached singleton, zero allocation
}
```

### Chaining

| Method | Runs when | Does |
| --- | --- | --- |
| `Map` | success | transforms the value |
| `Bind` | success | chains another result-producing call |
| `Ensure` | success | fails if a predicate is not met |
| `Tap` | success | side effect, returns the same result |
| `TapError` | failure | side effect on the error |
| `Match` | both | collapses to a single value, exhaustively |

Every method has an `async` sibling (`MapAsync`, `BindAsync`, `EnsureAsync`, …).

```csharp
Result<OrderDto> result = await GetOrderAsync(id)
    .EnsureAsync(o => o.OwnerId == userId, ResultError.Forbidden())
    .Bind(o => o.Confirm())          // returns Result<Order>
    .Map(o => o.ToDto());
```

## Errors

Built-in factories cover the common HTTP-shaped cases:

```csharp
ResultError.BadRequest();            // 400
ResultError.Unauthorized();          // 401
ResultError.Forbidden();             // 403
ResultError.NotFound();              // 404
ResultError.Conflict();              // 409
ResultError.UnsupportedMediaType();  // 415
ResultError.Validation();            // 422
ResultError.InternalServerError();   // 500
```

Each takes an optional message; when omitted, a **localized default** is used.

> **Equality note:** two `ResultError`s are equal when they share the same `ErrorType`,
> regardless of message. Equality answers *"is this the same kind of error?"* — handy for
> `result.Error == ResultError.NotFound()` style checks. Keep it in mind when comparing errors.

### Adding your own error types (extensibility)

`ErrorType` is **open for extension** — it is a `readonly record struct`, not a closed enum.
Declare your own categories and use them anywhere a built-in type is accepted:

```csharp
public static class AppErrors
{
    public static readonly ErrorType RateLimited = new("RateLimited", 429);
    public static readonly ErrorType PaymentRequired = new("PaymentRequired", 402);
}

return ResultError.Custom(AppErrors.RateLimited, "Too many requests, slow down.");
```

The HTTP status code travels inside the `ErrorType`, so the ASP.NET mapping (below) handles your
custom categories automatically — no extra wiring.

### Structured validation errors

```csharp
List<ValidationError> errors =
[
    new("Email", ValidationCodes.Required, "Email is required."),
    new("Age",   ValidationCodes.OutOfRange, "Age must be 18 or older."),
];

return ResultError.Validation(errors);   // 422 with per-field details
```

`ValidationCodes` provides machine-readable constants (`required`, `invalid_format`,
`out_of_range`, …) so clients can branch on a stable code rather than a message.

## ASP.NET Core integration

Add `Resulto.AspNetCore` and turn any result into an HTTP response:

```csharp
using Resulto.AspNetCore;

app.MapGet("/users/{id}", async (Guid id) =>
    (await userService.GetAsync(id)).ToHttpResult());        // 200 or ProblemDetails

app.MapPost("/users", async (CreateUser cmd) =>
{
    Result<User> result = await userService.CreateAsync(cmd);
    return result.ToHttpCreated(uri: result.IsSuccess ? $"/users/{result.Value.Id}" : null);  // 201 or ProblemDetails
});

app.MapDelete("/users/{id}", async (Guid id) =>
    (await userService.DeleteAsync(id)).ToHttpResult());     // 204 or ProblemDetails
```

By default, failures become RFC 7807 `ProblemDetails`; validation failures become
`ValidationProblem` with per-field errors. The status code is taken straight from the error,
so custom error types map correctly with no extra code.

### Customizing how errors are rendered

The error → HTTP mapping is an extension point — you are never locked into the built-in behavior.
Customize it at three levels (see `ResultHttp`):

**Per call** — override for a single endpoint:

```csharp
return result.ToHttpResult(onError: err => Results.StatusCode(err.Code));
```

**Globally** — set the default once at startup, composing with the built-in mapper for the rest:

```csharp
// Program.cs
ResultHttp.DefaultErrorMapper = error => error.ErrorType == AppErrors.RateLimited
    ? Results.Json(new { error = "rate_limited" }, statusCode: 429)
    : ResultHttp.ProblemDetails(error);   // built-in behavior for everything else
```

**Fully** — write your own `ResultErrorMapper` from scratch:

```csharp
ResultErrorMapper myMapper = error => Results.Json(
    new { code = error.ErrorType.Name, message = error.Message },
    statusCode: error.Code);

ResultHttp.DefaultErrorMapper = myMapper;     // or pass per call: result.ToHttpResult(myMapper)
```

## Localization

Default error messages are resolved from `CultureInfo.CurrentCulture`. English (neutral),
Russian (`ru`), and Tajik (`tg`) ship in the package. Set a custom message on any factory to
override the default.

## Performance

Returning a failure with `Result` is **~300–380× faster** than throwing and catching an exception
for the same logic, and allocates ~4× less — because throwing captures a stack trace while a
`Result` is just a method return.

| Method (Depth=1)            |        Mean |     Ratio | Allocated |
|---------------------------- |------------:|----------:|----------:|
| **Result (return failure)** |    `15.2 ns` |  **1.00** |    `80 B` |
| Exception (throw + catch)   | `5,779.3 ns` | **≈381×** |   `344 B` |

See [BENCHMARKS.md](BENCHMARKS.md) for the full numbers and how to run them.

## Samples

A runnable minimal API showing `ToHttpResult`, validation errors, and a custom error type lives
in [`samples/Resulto.Sample.Api`](samples/Resulto.Sample.Api):

```bash
dotnet run --project samples/Resulto.Sample.Api
```

## Building from source

```bash
dotnet build
dotnet test
dotnet pack -c Release   # produces .nupkg + .snupkg
```

## Contributing

Issues and pull requests are welcome. Please make sure `dotnet build` (warnings are errors)
and `dotnet test` pass, and keep the core `Resulto` package dependency-free.

## License

[MIT](LICENSE) © Qurbonali Nazarov
