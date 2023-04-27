namespace System
{
    public static class ValueTypeExtensions
    {
        public static T? ToNullable<T>(this T value)
                where T : struct
            => value;
    }
}
