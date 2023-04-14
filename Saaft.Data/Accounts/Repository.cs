using Saaft.Data.Database;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Saaft.Data.Accounts
{
    public class Repository
    {
        public Repository(DataStore dataStore)
        {
            _dataStore = dataStore;

            _currentFile = dataStore
                .WhereNotNull()
                .DistinctUntilChanged()
                .ShareReplay(1);

            _currentVersions = _currentFile
                .Select(file => file.Database.AccountVersions)
                .DistinctUntilChanged()
                .Select(versions => versions
                    .Where(version => versions
                        .All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                    .OrderBy(version => version.Id)
                    .ToList())
                .DistinctUntilChanged(
                    keySelector:    versions => versions.Select(version => version.Id),
                    comparer:       SequenceEqualityComparer<long>.Default)
                .ShareReplay(1);

            var versions = _currentFile
                .Select(file => file.Database.AccountVersions)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _maxAccountId = versions
                .Select(versions => versions
                    .Select(version => version.AccountId)
                    .DefaultIfEmpty()
                    .Max())
                .DistinctUntilChanged()
                .ShareReplay(1);

            _maxVersionId = versions
                .Select(versions => versions
                    .Select(version => version.Id)
                    .DefaultIfEmpty()
                    .Max())
                .DistinctUntilChanged()
                .ShareReplay(1);
        }

        public IObservable<IReadOnlyList<VersionEntity>> CurrentVersions
            => _currentVersions;

        public IObservable<Unit> Create(IObservable<CreationModel> createRequested)
            => createRequested
                .WithLatestFrom(_currentFile,   (model, currentFile) => (model, currentFile))
                .WithLatestFrom(_maxAccountId,  (@params, maxAccountId) => (@params.model, @params.currentFile, maxAccountId))
                .WithLatestFrom(_maxVersionId,  (@params, maxVersionId) => (@params.model, @params.currentFile, @params.maxAccountId, maxVersionId))
                .Do(@params =>
                {
                    _dataStore.Value = @params.currentFile with
                    {
                        Database = @params.currentFile.Database with
                        {
                            AccountVersions = @params.currentFile.Database.AccountVersions
                                .Add(new()
                                { 
                                    AccountId           = @params.maxAccountId + 1,
                                    Description         = @params.model.Description,
                                    Id                  = @params.maxVersionId + 1,
                                    Name                = @params.model.Name!,
                                    ParentAccountId     = @params.model.ParentAccountId,
                                    Type                = @params.model.Type
                                })
                        }
                    };
                })
                .Select(_ => default(Unit));

        private readonly IObservable<FileEntity>                    _currentFile;
        private readonly IObservable<IReadOnlyList<VersionEntity>>  _currentVersions;
        private readonly DataStore                                  _dataStore;
        private readonly IObservable<long>                          _maxAccountId;
        private readonly IObservable<long>                          _maxVersionId;
    }
}
