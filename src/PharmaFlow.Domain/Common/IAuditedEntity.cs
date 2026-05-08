namespace PharmaFlow.Domain.Common;

/// <summary>
/// Non-generic marker on <see cref="Entity{TId}"/> so the audit interceptor can
/// filter entries without knowing the typed-ID parameter.                                                                                 
/// </summary>                                                                                                                             
public interface IAuditedEntity
{
}