using System.Collections.Immutable;

using Saaft.Data.Accounts;

namespace Saaft.Data.Database
{
    public record Entity
    {
        public static readonly Entity Empty
            = new()
            {
                AccountVersions = ImmutableList<VersionEntity>.Empty,
            };

        public required ImmutableList<VersionEntity> AccountVersions { get; init; }
    }
}
