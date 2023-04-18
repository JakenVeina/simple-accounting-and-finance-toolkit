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
        public Repository(
            Data.Auditing.Repository    auditingRepository,
            DataStore                   dataStore,
            SystemClock                 systemClock)
        {
            _auditingRepository = auditingRepository;
            _dataStore          = dataStore;
            _systemClock        = systemClock;

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
                    comparer:       SequenceEqualityComparer<ulong>.Default)
                .ShareReplay(1);

            var versions = _currentFile
                .Select(file => file.Database.AccountVersions)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _nextAccountId = versions
                .Select(versions => versions
                    .Select(version => version.AccountId)
                    .DefaultIfEmpty()
                    .Max())
                .Select(maxAccountId => maxAccountId + 1)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _nextVersionId = versions
                .Select(versions => versions
                    .Select(version => version.Id)
                    .DefaultIfEmpty()
                    .Max())
                .Select(maxVersionId => maxVersionId + 1)
                .DistinctUntilChanged()
                .ShareReplay(1);
        }

        public IObservable<IReadOnlyList<VersionEntity>> CurrentVersions
            => _currentVersions;

        public IObservable<Unit> Create(IObservable<CreationModel> createRequested)
            => createRequested
                .WithLatestFrom(
                    Observable.CombineLatest(
                        _currentFile,
                        _nextAccountId,
                        _nextVersionId,
                        _auditingRepository.NextActionId,
                        (currentFile, nextAccountId, nextVersionId, nextActionId) => (currentFile, nextAccountId, nextVersionId, nextActionId)),
                    (model, state) => (model, state))
                .Do(@params => _dataStore.Value = @params.state.currentFile with
                {
                    Database = @params.state.currentFile.Database with
                    {
                        AccountVersions = @params.state.currentFile.Database.AccountVersions
                            .Add(new()
                            { 
                                Id                  = @params.state.nextVersionId,
                                AccountId           = @params.state.nextAccountId,
                                CreationId          = @params.state.nextActionId,
                                Description         = @params.model.Description,
                                Name                = @params.model.Name,
                                ParentAccountId     = @params.model.ParentAccountId,
                                Type                = @params.model.Type
                            }),
                        AuditingActions = @params.state.currentFile.Database.AuditingActions
                            .Add(new()
                            {
                                Id          = @params.state.nextActionId,
                                Performed   = _systemClock.Now,
                                TypeId      = Auditing.ActionTypes.AccountCreated.Id
                            })
                    }
                })
                .Select(_ => default(Unit));

        public IObservable<Unit> Mutate(IObservable<MutationModel> mutateRequested)
            => mutateRequested
                .WithLatestFrom(
                    Observable.CombineLatest(
                        _currentFile,
                        _currentVersions,
                        _nextVersionId,
                        _auditingRepository.NextActionId,
                        (currentFile, currentVersions, nextVersionId, nextActionId) => (currentFile, currentVersions, nextVersionId, nextActionId)),
                    (model, state) => (model, state))
                .Do(@params =>
                {
                    var currentVersion = @params.state.currentVersions
                        .First(version => version.AccountId == @params.model.AccountId);
                    
                    _dataStore.Value = @params.state.currentFile with
                    {
                        Database = @params.state.currentFile.Database with
                        {
                            AccountVersions = @params.state.currentFile.Database.AccountVersions
                                .Replace(currentVersion, currentVersion with
                                {
                                    Id                  = @params.state.nextVersionId,
                                    CreationId          = @params.state.nextActionId,
                                    Description         = @params.model.Description,
                                    Name                = @params.model.Name,
                                    ParentAccountId     = @params.model.ParentAccountId,
                                    Type                = @params.model.Type
                                }),
                            AuditingActions = @params.state.currentFile.Database.AuditingActions
                                .Add(new()
                                {
                                    Id          = @params.state.nextActionId,
                                    Performed   = _systemClock.Now,
                                    TypeId      = Auditing.ActionTypes.AccountMutated.Id
                                })
                        }
                    };
                })
                .Select(_ => default(Unit));

        private readonly Data.Auditing.Repository                   _auditingRepository;
        private readonly IObservable<FileEntity>                    _currentFile;
        private readonly IObservable<IReadOnlyList<VersionEntity>>  _currentVersions;
        private readonly DataStore                                  _dataStore;
        private readonly IObservable<ulong>                         _nextAccountId;
        private readonly IObservable<ulong>                         _nextVersionId;
        private readonly SystemClock                                _systemClock;
    }
}
