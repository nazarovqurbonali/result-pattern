namespace Resulto.Tests;

public class BaseResultTests
{
    [Fact]
    public void Success_returns_cached_singleton()
    {
        Assert.Same(BaseResult.Success(), BaseResult.Success());
        Assert.True(BaseResult.Success().IsSuccess);
    }

    [Fact]
    public void Failure_carries_error()
    {
        BaseResult result = BaseResult.Failure(ResultError.Forbidden());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.ErrorType);
    }

    [Fact]
    public void Accessing_error_on_success_throws()
    {
        Assert.Throws<InvalidOperationException>(() => BaseResult.Success().Error);
    }

    [Fact]
    public void Implicit_conversion_from_error_creates_failure()
    {
        BaseResult result = ResultError.Unauthorized();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.ErrorType);
    }

    [Fact]
    public void Match_selects_branch()
    {
        Assert.Equal("ok", BaseResult.Success().Match(() => "ok", _ => "err"));
        Assert.Equal("err", BaseResult.Failure(ResultError.NotFound()).Match(() => "ok", _ => "err"));
    }

    [Fact]
    public void Ensure_converts_to_failure_when_predicate_false()
    {
        BaseResult result = BaseResult.Success().Ensure(() => false, ResultError.Conflict());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void ToResult_with_value_wraps_success()
    {
        Result<int> result = BaseResult.Success().ToResult(99);

        Assert.True(result.IsSuccess);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public void ToResult_forwards_failure_error()
    {
        Result<int> result = BaseResult.Failure(ResultError.NotFound()).ToResult<int>();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
    }

    [Fact]
    public void ToResult_on_success_without_value_throws()
    {
        Assert.Throws<InvalidOperationException>(() => BaseResult.Success().ToResult<int>());
    }
}
