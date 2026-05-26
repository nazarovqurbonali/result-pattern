namespace Resulto;

/// <summary>
/// Common contract shared by <see cref="BaseResult"/> and <see cref="Result{T}"/>.
/// Enables generic handling of any result (e.g. logging middleware, response mapping).
/// </summary>
public interface IResult
{
    /// <summary>True when the operation succeeded.</summary>
   public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
  public  bool IsFailure { get; }

    /// <summary>The error details. Throws <see cref="InvalidOperationException"/> when <see cref="IsSuccess"/> is true.</summary>
  public  ResultError Error { get; }
}
