namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        validOn:        AttributeTargets.Class |
            AttributeTargets.Struct |
            AttributeTargets.Field |
            AttributeTargets.Property,
        AllowMultiple   = false,
        Inherited       = false)]
    public sealed class RequiredMemberAttribute
        : Attribute
    { }
}
