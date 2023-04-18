using System;
using System.Linq;
using System.Reactive.Linq;

namespace Saaft.Data.Auditing
{
    public class Repository
    {
        public Repository(DataStore dataStore)
            => _nextActionId = dataStore
                .WhereNotNull()
                .Select(file => file.Database.AuditingActions)
                .DistinctUntilChanged()
                .Select(actions => actions
                    .Select(action => action.Id)
                    .DefaultIfEmpty()
                    .Max())
                .Select(maxActionId => maxActionId + 1)
                .ShareReplay(1);

        public IObservable<ulong> NextActionId
            => _nextActionId;

        private readonly IObservable<ulong> _nextActionId;
    }
}
