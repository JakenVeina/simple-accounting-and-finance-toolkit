namespace Saaft.Desktop.Accounts
{
    public record NameExistsError
    {
        public required string Name { get; init; }
    }
}
