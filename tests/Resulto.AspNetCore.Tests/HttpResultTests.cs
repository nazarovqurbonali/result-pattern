using IHttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Resulto.AspNetCore.Tests;

public class HttpResultTests
{
    private static int? StatusOf(IHttpResult result) => ((IStatusCodeHttpResult)result).StatusCode;

    [Fact]
    public void Success_maps_to_200_ok()
    {
        IHttpResult http = Result<int>.Success(7).ToHttpResult();

        Assert.Equal(StatusCodes.Status200OK, StatusOf(http));
    }

    [Fact]
    public void BaseResult_success_maps_to_204_no_content()
    {
        IHttpResult http = BaseResult.Success().ToHttpResult();

        Assert.Equal(StatusCodes.Status204NoContent, StatusOf(http));
    }

    [Fact]
    public void Created_maps_to_201()
    {
        IHttpResult http = Result<int>.Success(1).ToHttpCreated("/items/1");

        Assert.Equal(StatusCodes.Status201Created, StatusOf(http));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(409)]
    public void Error_maps_to_its_status_code(int code)
    {
        ResultError error = code switch
        {
            400 => ResultError.BadRequest(),
            404 => ResultError.NotFound(),
            _ => ResultError.Conflict(),
        };

        IHttpResult http = Result<int>.Failure(error).ToHttpResult();

        Assert.Equal(code, StatusOf(http));
    }

    [Fact]
    public void Validation_error_maps_to_422()
    {
        ResultError error = ResultError.Validation(
        [
            new("Email", ValidationCodes.Required, "Email is required."),
        ]);

        IHttpResult http = Result<int>.Failure(error).ToHttpResult();

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, StatusOf(http));
    }

    [Fact]
    public void Custom_error_type_maps_via_its_code()
    {
        ResultError error = ResultError.Custom(new ErrorType("RateLimited", 429));

        IHttpResult http = Result<int>.Failure(error).ToHttpResult();

        Assert.Equal(429, StatusOf(http));
    }

    [Fact]
    public void Per_call_mapper_overrides_default()
    {
        IHttpResult http = Result<int>.Failure(ResultError.NotFound())
            .ToHttpResult(_ => Results.StatusCode(418));

        Assert.Equal(418, StatusOf(http));
    }

    [Fact]
    public void Global_default_mapper_can_be_replaced()
    {
        ResultErrorMapper original = ResultHttp.DefaultErrorMapper;
        try
        {
            ResultHttp.DefaultErrorMapper = error => error.ErrorType == ErrorType.NotFound
                ? Results.StatusCode(499)
                : ResultHttp.ProblemDetails(error);

            Assert.Equal(499, StatusOf(Result<int>.Failure(ResultError.NotFound()).ToHttpResult()));
            Assert.Equal(409, StatusOf(Result<int>.Failure(ResultError.Conflict()).ToHttpResult()));
        }
        finally
        {
            ResultHttp.DefaultErrorMapper = original;
        }
    }

    [Fact]
    public void DefaultErrorMapper_rejects_null() => Assert.Throws<ArgumentNullException>(() => ResultHttp.DefaultErrorMapper = null!);
}
