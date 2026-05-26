namespace Resulto.Tests;

public class ResultTests
{
    [Fact]
    public void Success_exposes_value_and_is_successful()
    {
        Result<int> result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_exposes_error_and_is_failed()
    {
        Result<int> result = Result<int>.Failure(ResultError.NotFound());

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
    }

    [Fact]
    public void Success_with_null_reference_throws() => Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));

    [Fact]
    public void Accessing_value_on_failure_throws()
    {
        Result<int> result = ResultError.NotFound();

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Accessing_error_on_success_throws()
    {
        Result<int> result = 5;

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Implicit_conversion_from_value_creates_success()
    {
        Result<int> result = 7;

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_error_creates_failure()
    {
        Result<int> result = ResultError.Conflict();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void Map_transforms_success_value()
    {
        Result<int> result = Result<int>.Success(10).Map(x => x * 2);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void Map_forwards_error_without_invoking_mapper()
    {
        bool called = false;
        Result<int> result = Result<int>.Failure(ResultError.NotFound()).Map(x => { called = true; return x; });

        Assert.True(result.IsFailure);
        Assert.False(called);
    }

    [Fact]
    public void Bind_chains_success()
    {
        Result<int> result = Result<int>.Success(4).Bind(x => Result<string>.Success($"v{x}"))
            .Bind(s => Result<int>.Success(s.Length));

        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        Result<int> result = Result<int>.Failure(ResultError.BadRequest()).Bind(x => Result<int>.Success(x + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.BadRequest, result.Error.ErrorType);
    }

    [Fact]
    public void Ensure_fails_when_predicate_false()
    {
        Result<int> result = Result<int>.Success(3).Ensure(x => x > 5, ResultError.BadRequest("too small"));

        Assert.True(result.IsFailure);
        Assert.Equal("too small", result.Error.Message);
    }

    [Fact]
    public void Ensure_keeps_success_when_predicate_true()
    {
        Result<int> result = Result<int>.Success(10).Ensure(x => x > 5, ResultError.BadRequest());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Ensure_does_not_evaluate_predicate_on_failure()
    {
        bool called = false;
        Result<int> result = Result<int>.Failure(ResultError.NotFound())
            .Ensure(_ => { called = true; return true; }, ResultError.BadRequest());

        Assert.True(result.IsFailure);
        Assert.False(called);
    }

    [Fact]
    public void Tap_runs_only_on_success()
    {
        int seen = 0;
        Result<int>.Success(9).Tap(x => seen = x);
        Assert.Equal(9, seen);

        seen = 0;
        Result<int>.Failure(ResultError.NotFound()).Tap(x => seen = x);
        Assert.Equal(0, seen);
    }

    [Fact]
    public void TapError_runs_only_on_failure()
    {
        ResultError? seen = null;
        Result<int>.Failure(ResultError.Conflict()).TapError(e => seen = e);
        Assert.NotNull(seen);

        seen = null;
        Result<int>.Success(1).TapError(e => seen = e);
        Assert.Null(seen);
    }

    [Fact]
    public void Match_selects_branch()
    {
        string ok = Result<int>.Success(2).Match(x => $"ok:{x}", e => $"err:{e.Code}");
        string err = Result<int>.Failure(ResultError.NotFound()).Match(x => $"ok:{x}", e => $"err:{e.Code}");

        Assert.Equal("ok:2", ok);
        Assert.Equal("err:404", err);
    }

    [Fact]
    public void ToBaseResult_preserves_state()
    {
        Assert.True(Result<int>.Success(1).ToBaseResult().IsSuccess);
        Assert.True(Result<int>.Failure(ResultError.NotFound()).ToBaseResult().IsFailure);
    }

    [Fact]
    public void Null_delegate_throws()
    {
        Result<int> result = Result<int>.Success(1);

        Assert.Throws<ArgumentNullException>(() => result.Map<int>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Bind<int>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Tap(null!));
    }
}
