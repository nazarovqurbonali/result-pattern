namespace Resulto.Benchmarks;

/// <summary>
/// Compares modeling an *expected* failure with the Result pattern versus throwing/catching
/// an exception. Includes a deep-stack variant, where exception cost grows with stack depth
/// while the Result cost stays flat.
/// </summary>
[MemoryDiagnoser]
public class ResultVsExceptionBenchmarks
{
    // BenchmarkDotNet sets this via reflection, so it must be a public, settable member —
    // not a constructor parameter (the class needs a public parameterless constructor).
    [Params(1, 16)]
    public int Depth { get; set; }

    // ── Failure path ─────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Result (return failure)")]
    public bool Result_Failure() => DivideResult(10, 0, Depth).IsFailure;

    [Benchmark(Description = "Exception (throw + catch)")]
    public bool Exception_ThrowCatch()
    {
        try
        {
            DivideThrow(10, 0, Depth);
            return false;
        }
        catch (DivideByZeroException)
        {
            return true;
        }
    }

    // ── Success path (reference: both are cheap) ────────────────

    [Benchmark(Description = "Result (success)")]
    public int Result_Success() => DivideResult(10, 2, Depth).Value;

    // ── Helpers ──────────────────────────────────────────────────

    private static Result<int> DivideResult(int a, int b, int depth)
    {
        if (depth > 0) return DivideResult(a, b, depth - 1);
        return b == 0 ? ResultError.BadRequest("Division by zero.") : a / b;
    }

    private static int DivideThrow(int a, int b, int depth)
    {
        if (depth > 0) return DivideThrow(a, b, depth - 1);
        return b == 0 ? throw new DivideByZeroException() : a / b;
    }
}
