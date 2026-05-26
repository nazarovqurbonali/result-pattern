namespace Resulto;

/// <summary>
/// Represents the outcome of an operation that produces a value of type <typeparamref name="T"/> on success.
/// Use <see cref="BaseResult"/> when the operation returns no value.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T> : IResult
{
    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    private readonly T? _value;
    private readonly ResultError? _error;

    /// <summary>
    /// The success value. Throws <see cref="InvalidOperationException"/> when <see cref="IsFailure"/> is true.
    /// Always check <see cref="IsSuccess"/> before accessing this property, or use <see cref="Match{TOut}"/>.
    /// </summary>
    public T Value
    {
        get
        {
            if (IsFailure) ThrowNoValueOnFailure();
            return _value!;
        }
    }

    /// <summary>
    /// The error details. Throws <see cref="InvalidOperationException"/> when <see cref="IsSuccess"/> is true.
    /// Always check <see cref="IsFailure"/> before accessing this property, or use <see cref="Match{TOut}"/>.
    /// </summary>
    public ResultError Error
    {
        get
        {
            if (IsSuccess) ThrowNoErrorOnSuccess();
            return _error!;
        }
    }

    private Result(bool isSuccess, T? value, ResultError? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>Creates a successful result wrapping the given value.</summary>
    /// <remarks>
    /// Uses <c>if (value is null)</c> rather than a generic null-check helper because
    /// <typeparamref name="T"/> is unconstrained — passing it as <c>object?</c> would box
    /// value types on every call. The pattern-match is optimized by the JIT to a no-op for value types.
    /// </remarks>
    public static Result<T> Success(T value)
    {
        if (value is null) ThrowArgumentNull(nameof(value));
        return new(true, value, null);
    }

    /// <summary>Creates a failed result from the given error.</summary>
    public static Result<T> Failure(ResultError error)
    {
        Guard.NotNull(error);
        return new(false, default, error);
    }

    /// <summary>Creates a failed result carrying structured per-field validation errors.</summary>
    public static Result<T> ValidationFailure(IReadOnlyList<ValidationError> errors)
    {
        Guard.NotNull(errors);
        return new(false, default, ResultError.Validation(errors));
    }

    // ──────────────────────────────────────────────
    //  Transforms
    // ──────────────────────────────────────────────

    /// <summary>
    /// Transforms the success value using <paramref name="mapper"/>.
    /// If the result is a failure, the error is forwarded without calling <paramref name="mapper"/>.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        Guard.NotNull(mapper);
        return IsSuccess ? Result<TOut>.Success(mapper(_value!)) : Result<TOut>.Failure(_error!);
    }

    /// <summary>Async version of <see cref="Map{TOut}"/>.</summary>
    /// <remarks>
    /// Non-async signature: on failure, returns <c>Task.FromResult</c> without allocating
    /// an async state machine. Only the success path enters <see cref="MapAsyncCore{TOut}"/>.
    /// </remarks>
    public Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper)
    {
        Guard.NotNull(mapper);
        return IsSuccess ? MapAsyncCore(mapper) : Task.FromResult(Result<TOut>.Failure(_error!));
    }

    /// <summary>
    /// Chains this result with another result-producing operation.
    /// If the result is a failure, the error is forwarded without calling <paramref name="binder"/>.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        Guard.NotNull(binder);
        return IsSuccess ? binder(_value!) : Result<TOut>.Failure(_error!);
    }

    /// <summary>Async version of <see cref="Bind{TOut}"/>.</summary>
    /// <remarks>
    /// Non-async signature: both paths return a <see cref="Task{TResult}"/> directly —
    /// the binder's task on success, or <c>Task.FromResult</c> on failure.
    /// No async state machine allocated.
    /// </remarks>
    public Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
    {
        Guard.NotNull(binder);
        return IsSuccess ? binder(_value!) : Task.FromResult(Result<TOut>.Failure(_error!));
    }

    // ──────────────────────────────────────────────
    //  Ensure — conditional failure based on the value
    // ──────────────────────────────────────────────

    /// <summary>
    /// If the result is successful and <paramref name="predicate"/> returns false for the value,
    /// converts this to a failure with the given <paramref name="error"/>.
    /// If the result is already a failure, the predicate is not evaluated and the error is forwarded.
    /// </summary>
    /// <remarks>
    /// Use for domain invariant checks in fluent chains:
    /// <code>
    /// Result&lt;Vacancy&gt; result = await GetVacancyAsync(id, ct)
    ///     .Ensure(v => v.IsActive, ResultError.Forbidden("Vacancy is closed"))
    ///     .Ensure(v => v.OwnerId == currentUserId, ResultError.Forbidden("Not the owner"));
    /// </code>
    /// </remarks>
    public Result<T> Ensure(Func<T, bool> predicate, ResultError error)
    {
        Guard.NotNull(predicate);
        Guard.NotNull(error);

        if (IsFailure) return this;
        return predicate(_value!) ? this : Failure(error);
    }

    /// <summary>Async version of <see cref="Ensure"/>.</summary>
    /// <remarks>
    /// Non-async signature: on failure, returns <c>Task.FromResult(this)</c> without
    /// allocating an async state machine. Only the success path enters the async core.
    /// </remarks>
    public Task<Result<T>> EnsureAsync(Func<T, Task<bool>> predicate, ResultError error)
    {
        Guard.NotNull(predicate);
        Guard.NotNull(error);

        if (IsFailure) return Task.FromResult(this);
        return EnsureAsyncCore(predicate, error);
    }

    private async Task<Result<T>> EnsureAsyncCore(Func<T, Task<bool>> predicate, ResultError error) => await predicate(_value!).ConfigureAwait(false) ? this : Failure(error);

    // ──────────────────────────────────────────────
    //  Side effects (Tap)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Executes <paramref name="action"/> on the value only when the result is successful.
    /// Returns the same result for fluent chaining.
    /// </summary>
    public Result<T> Tap(Action<T> action)
    {
        Guard.NotNull(action);
        if (IsSuccess) action(_value!);
        return this;
    }

    /// <summary>Async version of <see cref="Tap"/>.</summary>
    /// <remarks>
    /// Non-async signature: on failure, returns <c>Task.FromResult(this)</c> without
    /// allocating an async state machine (~100 bytes saved per no-op call).
    /// Only the success path enters <see cref="TapAsyncCore"/>.
    /// </remarks>
    public Task<Result<T>> TapAsync(Func<T, Task> action)
    {
        Guard.NotNull(action);
        if (IsFailure) return Task.FromResult(this);
        return TapAsyncCore(action);
    }

    private async Task<Result<T>> TapAsyncCore(Func<T, Task> action)
    {
        await action(_value!).ConfigureAwait(false);
        return this;
    }

    /// <summary>
    /// Executes <paramref name="action"/> on the error only when the result is a failure.
    /// Returns the same result for fluent chaining.
    /// </summary>
    public Result<T> TapError(Action<ResultError> action)
    {
        Guard.NotNull(action);
        if (IsFailure) action(_error!);
        return this;
    }

    /// <summary>Async version of <see cref="TapError"/>.</summary>
    /// <remarks>
    /// Non-async signature: on success, returns <c>Task.FromResult(this)</c> without
    /// allocating an async state machine. Only the failure path enters the async core.
    /// </remarks>
    public Task<Result<T>> TapErrorAsync(Func<ResultError, Task> action)
    {
        Guard.NotNull(action);
        if (IsSuccess) return Task.FromResult(this);
        return TapErrorAsyncCore(action);
    }

    private async Task<Result<T>> TapErrorAsyncCore(Func<ResultError, Task> action)
    {
        await action(_error!).ConfigureAwait(false);
        return this;
    }

    // ──────────────────────────────────────────────
    //  Pattern matching
    // ──────────────────────────────────────────────

    /// <summary>
    /// Pattern-matches on the result, calling <paramref name="onSuccess"/> or <paramref name="onFailure"/>.
    /// Guarantees exhaustive handling of both cases.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<ResultError, TOut> onFailure)
    {
        Guard.NotNull(onSuccess);
        Guard.NotNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    /// <summary>Async version of <see cref="Match{TOut}"/>.</summary>
    /// <remarks>
    /// Non-async signature: the callback already returns <see cref="Task{TOut}"/>,
    /// so we return it directly — no async state machine allocated.
    /// The original <c>async/await</c> version created a state machine (~100 bytes)
    /// on every call just to unwrap and re-wrap the same Task.
    /// </remarks>
    public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<ResultError, Task<TOut>> onFailure)
    {
        Guard.NotNull(onSuccess);
        Guard.NotNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    // ──────────────────────────────────────────────
    //  Conversions
    // ──────────────────────────────────────────────

    /// <summary>Drops the value and returns a <see cref="BaseResult"/> preserving success/failure state.</summary>
    public BaseResult ToBaseResult() => IsSuccess ? BaseResult.Success() : BaseResult.Failure(_error!);

    /// <summary>Allows implicit conversion from a value to a successful <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Allows implicit conversion from <see cref="ResultError"/> to a failed <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(ResultError error) => Failure(error);

    // ──────────────────────────────────────────────
    //  Async core methods
    // ──────────────────────────────────────────────

    private async Task<Result<TOut>> MapAsyncCore<TOut>(Func<T, Task<TOut>> mapper) =>
        Result<TOut>.Success(await mapper(_value!).ConfigureAwait(false));

    // ──────────────────────────────────────────────
    //  Throw helpers
    // ──────────────────────────────────────────────

    [StackTraceHidden]
    private static void ThrowNoValueOnFailure() =>
        throw new InvalidOperationException(Messages.Exception.ResultIsFailure());

    [StackTraceHidden]
    private static void ThrowNoErrorOnSuccess() =>
        throw new InvalidOperationException(Messages.Exception.CannotAccessErrorOnSuccess());

    /// <remarks>
    /// Custom throw helper used for the unconstrained <typeparamref name="T"/> path so the value
    /// is never passed as <c>object?</c> (which would box value types). The caller's
    /// <c>if (value is null)</c> test is optimized by the JIT to a no-op for value types.
    /// </remarks>
    [StackTraceHidden]
    private static void ThrowArgumentNull(string paramName) =>
        throw new ArgumentNullException(paramName);
}
