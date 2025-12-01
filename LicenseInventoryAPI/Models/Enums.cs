namespace LicenseInventoryAPI.Models
{
    public enum LicenseType
    {
        PerUser,
        PerDevice,
        Concurrent,
        Subscription
    }

    public enum ComplianceSeverity
    {
        Low,
        Medium,
        High
    }

    public enum ComplianceEventType
    {
        Expiry,
        OverUse,
        Unused
    }

    public enum AllocationMethod
    {
        Fixed,
        UsageBased
    }

    public enum UserRole
    {
        Admin,
        Finance,
        Auditor,
        Viewer
    }
}