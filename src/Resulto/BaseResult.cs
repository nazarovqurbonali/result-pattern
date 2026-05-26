namespace Resulto;

/// <summary>
/// Represents the outcome of an operation that returns no value.
/// Use <see cref="Result{T}"/> when the operation produces a value on success.
/// </summary>
public sealed class BaseResult : IResult
{
    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    private readonly ResultError? _error;

    /// <summary>
    /// The error details. Throws <see cref="InvalidOperationException"/> when <see cref="IsSuccess"/> is true.
    /// Always check <see cref="IsFailure"/> before accessing this property.
    /// </summary>
    public ResultError Error
    {
        get
        {
            if (IsSuccess) ThrowNoErrorOnSuccess();
            return _error!;
        }
    }

    private BaseResult(bool isSuccess, ResultError? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }


    /// <summary>
    /// Cached singleton — BaseResult is immutable and carries no value,
    /// so every Success() call returns the same instance. Zero GC pressure
    /// on void command hot paths (DeleteVacancy, MarkAsRead, etc.).
    /// </summary>
    private static readonly BaseResult SuccessInstance = new(true, null);

    /// <summary>Creates a successful result with no value.</summary>
    public static BaseResult Success() => SuccessInstance;

    /// <summary>Creates a failed result from the given error.</summary>
    public static BaseResult Failure(ResultError error)
    {
        Guard.NotNull(error);
        return new(false, error);
    }


    /// <summary>
    /// Pattern-matches on the result, calling <paramref name="onSuccess"/> or <paramref name="onFailure"/>.
    /// Guarantees exhaustive handling of both cases.
    /// </summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<ResultError, TOut> onFailure)
    {
        Guard.NotNull(onSuccess);
        Guard.NotNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(_error!);
    }


    /// <summary>
    /// Executes <paramref name="action"/> only when the result is successful.
    /// Returns the same result for fluent chaining.
    /// </summary>
    public BaseResult Tap(Action action)
    {
        Guard.NotNull(action);
        if (IsSuccess) action();
        return this;
    }

    /// <summary>
    /// Executes <paramref name="action"/> only when the result is a failure.
    /// Returns the same result for fluent chaining.
    /// </summary>
    public BaseResult TapError(Action<ResultError> action)
    {
        Guard.NotNull(action);
        if (IsFailure) action(_error!);
        return this;
    }


    /// <summary>
    /// If the result is successful and <paramref name="predicate"/> returns false,
    /// converts this to a failure with the given <paramref name="error"/>.
    /// If the result is already a failure, the predicate is not evaluated.
    /// </summary>
    public BaseResult Ensure(Func<bool> predicate, ResultError error)
    {
        Guard.NotNull(predicate);
        Guard.NotNull(error);

        if (IsFailure) return this;
        return predicate() ? this : Failure(error);
    }

    /// <summary>Async version of <see cref="Ensure"/>.</summary>
    /// <remarks>
    /// Non-async signature: on failure, returns cached <see cref="Task{BaseResult}"/>
    /// without allocating an async state machine. Only the success path (which actually
    /// awaits the predicate) enters the async core method.
    /// </remarks>
    public Task<BaseResult> EnsureAsync(Func<Task<bool>> predicate, ResultError error)
    {
        Guard.NotNull(predicate);
        Guard.NotNull(error);

        if (IsFailure) return Task.FromResult(this);
        return EnsureAsyncCore(predicate, error);
    }

    private async Task<BaseResult> EnsureAsyncCore(Func<Task<bool>> predicate, ResultError error)
        => await predicate().ConfigureAwait(false) ? this : Failure(error);


    /// <summary>
    /// Converts a failed <see cref="BaseResult"/> to a typed <see cref="Result{T}"/> carrying the same error.
    /// Throws when called on a successful result — use <see cref="ToResult{T}(T)"/> instead.
    /// </summary>
    public Result<T> ToResult<T>()
    {
        if (IsSuccess)
            throw new InvalidOperationException(
                Messages.Exception.CannotConvertSuccessBaseResultToTypedResult(typeof(T).Name));

        return Result<T>.Failure(_error!);
    }

    /// <summary>
    /// Converts this result to a typed <see cref="Result{T}"/>.
    /// On success, wraps <paramref name="value"/>. On failure, carries the same error.
    /// </summary>
    /// <remarks>
    /// Uses <c>if (value is null)</c> rather than a generic null-check helper because
    /// <typeparamref name="T"/> is unconstrained — passing it as <c>object?</c> would box
    /// value types on every call. The pattern-match is optimized by the JIT to a no-op for value types.
    /// </remarks>
    public Result<T> ToResult<T>(T value)
    {
        if (value is null) ThrowArgumentNull(nameof(value));
        return IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(_error!);
    }

    /// <summary>Allows implicit conversion from <see cref="ResultError"/> to a failed <see cref="BaseResult"/>.</summary>
    public static implicit operator BaseResult(ResultError error) => Failure(error);


    [StackTraceHidden]
    private static void ThrowNoErrorOnSuccess() =>
        throw new InvalidOperationException(Messages.Exception.CannotAccessErrorOnSuccess());

    [StackTraceHidden]
    private static void ThrowArgumentNull(string paramName) =>
        throw new ArgumentNullException(paramName);
}