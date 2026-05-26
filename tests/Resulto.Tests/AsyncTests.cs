namespace Resulto.Tests;

public class AsyncTests
{
    [Fact]
    public async Task MapAsync_transforms_success()
    {
        Result<int> result = await Result<int>.Success(5).MapAsync(x => Task.FromResult(x + 1));

        Assert.Equal(6, result.Value);
    }

    [Fact]
    public async Task MapAsync_forwards_failure()
    {
        Result<int> result = await Result<int>.Failure(ResultError.NotFound()).MapAsync(x => Task.FromResult(x + 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task BindAsync_chains_success()
    {
        Result<string> result = await Result<int>.Success(3)
            .BindAsync(x => Task.FromResult(Result<string>.Success($"v{x}")));

        Assert.Equal("v3", result.Value);
    }

    [Fact]
    public async Task EnsureAsync_fails_when_predicate_false()
    {
        Result<int> result = await Result<int>.Success(1)
            .EnsureAsync(_ => Task.FromResult(false), ResultError.BadRequest());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task TapAsync_runs_only_on_success()
    {
        int seen = 0;
        await Result<int>.Success(8).TapAsync(x => { seen = x; return Task.CompletedTask; });
        Assert.Equal(8, seen);

        seen = 0;
        await Result<int>.Failure(ResultError.NotFound()).TapAsync(x => { seen = x; return Task.CompletedTask; });
        Assert.Equal(0, seen);
    }

    [Fact]
    public async Task MatchAsync_selects_branch()
    {
        string ok = await Result<int>.Success(2)
            .MatchAsync(x => Task.FromResult($"ok:{x}"), _ => Task.FromResult("err"));

        Assert.Equal("ok:2", ok);
    }

    [Fact]
    public async Task BaseResult_EnsureAsync_keeps_success()
    {
        BaseResult result = await BaseResult.Success().EnsureAsync(() => Task.FromResult(true), ResultError.Conflict());

        Assert.True(result.IsSuccess);
    }
}
