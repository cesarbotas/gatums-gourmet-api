namespace GatumsGourmet.Domain.Common.V1;

public sealed class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static Result<T> Ok(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Fail(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> ValidationFail(IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors };
}
