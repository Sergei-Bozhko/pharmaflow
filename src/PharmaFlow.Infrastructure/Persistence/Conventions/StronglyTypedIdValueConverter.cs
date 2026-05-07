using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using PharmaFlow.Domain.Common.Ids;

namespace PharmaFlow.Infrastructure.Persistence.Conventions;

public sealed class StronglyTypedIdValueConverter<TId, TKey> : ValueConverter<TId, TKey>
    where TId : struct, IStronglyTypedId<TKey>
    where TKey : notnull
{
    public StronglyTypedIdValueConverter()
        : base(
            id => id.Value,
            value => (TId)Activator.CreateInstance(typeof(TId), value)!)
    {
    }
}