namespace ContentHub.BuildingBlocks.Application.Models;

/// <summary>Başarı/başarısızlığı istisnasız taşıyan sonuç tipi.</summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Başarılı sonuç hata taşıyamaz.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Başarısız sonuç bir hata taşımalıdır.");
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

/// <summary>Değer taşıyan sonuç.</summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız sonucun değeri okunamaz.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
