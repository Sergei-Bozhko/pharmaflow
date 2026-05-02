namespace PharmaFlow.Domain.Common.Ids;

public interface IStronglyTypedId<TKey>
    where TKey : notnull
{
    TKey Value { get; }
}