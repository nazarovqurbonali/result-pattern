# Benchmarks

Why model *expected* failures with `Result` instead of throwing? Because exceptions are
expensive: throwing one captures a stack trace and allocates, while returning a `Result` is
just a normal method return.

The benchmark below ([`benchmarks/Resulto.Benchmarks`](benchmarks/Resulto.Benchmarks)) compares
the two for the exact same logic — an operation that fails and whose caller handles the failure.

## Results

`BenchmarkDotNet v0.14.0`, .NET 8, 13th Gen Intel Core i7-1355U.
`Depth` is how many stack frames the failure propagates through before it is handled.

| Method                      | Depth |        Mean |     Ratio | Allocated | Alloc Ratio |
|---------------------------- |------:|------------:|----------:|----------:|------------:|
| **Result (return failure)** |     1 |    `15.2 ns` |  **1.00** |    `80 B` |        1.00 |
| Exception (throw + catch)   |     1 | `5,779.3 ns` | **≈381×** |   `344 B` |        4.30 |
| Result (success)            |     1 |     `5.4 ns` |      0.36 |    `32 B` |        0.40 |
|                             |       |             |           |           |             |
| **Result (return failure)** |    16 |    `20.4 ns` |  **1.00** |    `80 B` |        1.00 |
| Exception (throw + catch)   |    16 | `5,793.5 ns` | **≈284×** |   `344 B` |        4.30 |
| Result (success)            |    16 |    `12.0 ns` |      0.59 |    `32 B` |        0.40 |

> Numbers from a `ShortRun` job (3 warmups, 3 iterations) and are indicative — run the full job
> for publication-grade figures.

## Takeaways

- **Returning a failure is ~300–380× faster than throwing one** and allocates ~4× less.
- The `Result` cost is roughly **flat** regardless of stack depth; exception cost is dominated by
  the throw itself (stack capture), so it stays high no matter where it is caught.
- The success path is cheapest of all (~5 ns, one small allocation).

**Rule of thumb:** use exceptions for the *exceptional and unexpected* (bugs, corrupted state).
Use `Result` for *ordinary, expected* outcomes ("not found", "already exists", "invalid input")
that callers are meant to handle — they're both cheaper and explicit in the type signature.

## Running it yourself

```bash
dotnet run -c Release --project benchmarks/Resulto.Benchmarks -- --filter '*'
# faster, indicative run:
dotnet run -c Release --project benchmarks/Resulto.Benchmarks -- --filter '*' --job short
```
