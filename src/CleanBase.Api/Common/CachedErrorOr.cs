namespace CleanBase.Api.Common;

sealed record CachedErrorOr<T>
{
    public T? Value { get; init; }
    public List<CachedError>? Errors { get; init; }
    public bool IsError => Errors is { Count: > 0 };

    public static CachedErrorOr<T> FromErrorOr(ErrorOr<T> errorOr)
    {
        if (errorOr.IsError)
        {
            return new CachedErrorOr<T>
            {
                Errors = errorOr.Errors.Select(e => new CachedError
                {
                    Code = e.Code,
                    Description = e.Description,
                    Type = (int)e.Type
                }).ToList()
            };
        }

        return new CachedErrorOr<T> { Value = errorOr.Value };
    }

    public ErrorOr<T> ToErrorOr()
    {
        if (IsError)
        {
            var errors = Errors!.Select(e => Error.Custom(
                e.Type,
                e.Code,
                e.Description
            )).ToList();
            return errors;
        }

        return Value!;
    }
}

sealed record CachedError
{
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public int Type { get; init; }
}