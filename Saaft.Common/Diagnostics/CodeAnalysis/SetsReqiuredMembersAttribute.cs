namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(
        validOn:        AttributeTargets.Constructor,
        AllowMultiple   = false,
        Inherited       = false)]
    public sealed class SetsRequiredMembersAttribute
        : Attribute
    { }
}
