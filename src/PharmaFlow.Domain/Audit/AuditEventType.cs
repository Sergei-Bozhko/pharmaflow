namespace PharmaFlow.Domain.Audit;

public enum AuditEventType
{
    Create = 0,
    Update = 1,
    SoftDelete = 2,
    Read = 3,
    Login = 4,
    LoginFailed = 5,
    RoleChange = 6,
    SignatureApplied = 7,
    DocumentEffective = 8,
    ConsentCaptured = 9,
    StatusTransition = 10,
    KeyRotation = 11,
}