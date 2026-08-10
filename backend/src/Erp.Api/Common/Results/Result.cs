using System.Diagnostics.CodeAnalysis;

namespace Erp.Api.Common.Results;

/// <summary>
/// The outcome of an operation that can fail for an expected, modelled reason.
/// <para>
/// Expected failures (duplicate part number, closed financial year, insufficient
/// stock) are values, not exceptions. Exceptions remain for genuinely exceptional
/// conditions. The legacy codebase had 911 catch blocks against 117 throws because
/// it used exceptions for ordinary business outcomes and then swallowed them.
/// </para>
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <inheritdoc cref="Result"/>
/// <typeparam name="TValue">The value produced on success.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    /// <summary>
    /// The success value. Throws when the result is a failure, so a caller that
    /// forgets to check <see cref="Result.IsSuccess"/> fails loudly rather than
    /// silently propagating a default.
    /// </summary>
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
