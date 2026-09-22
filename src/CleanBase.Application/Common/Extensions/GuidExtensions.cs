namespace CleanBase.Application.Common.Extensions;

public static class GuidExtensions
{
    /// <summary>
    /// Returns null when the value is null or <see cref="Guid.Empty"/>, otherwise the value itself.
    /// Model binders coerce an omitted/blank <c>Guid?</c> field to <see cref="Guid.Empty"/> rather than
    /// null, so use this to normalize "no id supplied" back to null.
    /// </summary>
    public static Guid? NullIfEmpty(this Guid? value) =>
        value is { } id && id != Guid.Empty ? id : null;

    /// <inheritdoc cref="NullIfEmpty(System.Nullable{System.Guid})"/>
    public static Guid? NullIfEmpty(this Guid value) =>
        value != Guid.Empty ? value : null;
}
