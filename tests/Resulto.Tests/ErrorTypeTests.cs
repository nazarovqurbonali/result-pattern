namespace Resulto.Tests;

public class ErrorTypeTests
{
    [Fact]
    public void Builtin_types_carry_expected_http_codes()
    {
        Assert.Equal(400, ErrorType.BadRequest.HttpStatusCode);
        Assert.Equal(401, ErrorType.Unauthorized.HttpStatusCode);
        Assert.Equal(403, ErrorType.Forbidden.HttpStatusCode);
        Assert.Equal(404, ErrorType.NotFound.HttpStatusCode);
        Assert.Equal(409, ErrorType.Conflict.HttpStatusCode);
        Assert.Equal(415, ErrorType.UnsupportedMediaType.HttpStatusCode);
        Assert.Equal(422, ErrorType.Validation.HttpStatusCode);
        Assert.Equal(500, ErrorType.InternalServerError.HttpStatusCode);
    }

    [Fact]
    public void Custom_type_is_usable_and_carries_its_code()
    {
        ErrorType rateLimited = new("RateLimited", 429);
        ResultError error = ResultError.Custom(rateLimited, "Slow down.");

        Assert.Equal(429, error.Code);
        Assert.Equal(rateLimited, error.ErrorType);
        Assert.Equal("Slow down.", error.Message);
    }

    [Fact]
    public void Equality_is_structural()
    {
        Assert.Equal(new ErrorType("RateLimited", 429), new ErrorType("RateLimited", 429));
        Assert.NotEqual(new ErrorType("RateLimited", 429), new ErrorType("RateLimited", 430));
    }

    [Fact]
    public void Constructor_rejects_blank_name()
    {
        Assert.Throws<ArgumentException>(() => new ErrorType(" ", 400));
        Assert.Throws<ArgumentException>(() => new ErrorType(null!, 400));
    }

    [Fact]
    public void ToDefaultMessage_resolves_builtin_in_english()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");
            Assert.Equal("The requested resource was not found.", ErrorType.NotFound.ToDefaultMessage());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ToDefaultMessage_resolves_russian()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru");
            Assert.Equal("Запрашиваемый ресурс не найден.", ErrorType.NotFound.ToDefaultMessage());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ToDefaultMessage_falls_back_to_name_for_custom_type() => Assert.Equal("TeapotError", new ErrorType("TeapotError", 418).ToDefaultMessage());
}
