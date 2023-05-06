namespace Saaft.Data
{
    public record StateEntity<TEvent>
    {
        public required TEvent LatestEvent { get; init; }
    }
}
