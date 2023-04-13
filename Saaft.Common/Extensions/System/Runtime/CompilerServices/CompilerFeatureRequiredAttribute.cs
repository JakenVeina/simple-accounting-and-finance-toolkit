namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        validOn:        AttributeTargets.All,
        AllowMultiple   = true,
        Inherited       = false)]
    public sealed class CompilerFeatureRequiredAttribute
        : Attribute
    {
        public const string RefStructs
            = nameof(RefStructs);

        public const string RequiredMembers
            = nameof(RequiredMembers);

        public CompilerFeatureRequiredAttribute(string featureName)
            => FeatureName = featureName;

        public string FeatureName { get; }

        public bool IsOptional { get; set; }
    }
}
