using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using Saaft.Data.Auditing;

namespace Saaft.Data.Accounts
{
    public class Repository
    {
        public Repository(
            Data.Auditing.Repository    auditingRepository,
            Database.Repository         databaseRepository,
            DataStateStore              dataState,
            SystemClock                 systemClock)
        {
            _auditingRepository = auditingRepository;
            _dataState          = dataState;
            _systemClock        = systemClock;

            var allVersions = databaseRepository.LoadedDatabase
                .Select(database => database.AccountVersions)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _currentVersions = allVersions
                .Select(versions => versions
                    .Where(version => versions
                        .All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                    .OrderBy(version => version.Id)
                    .ToList())
                .DistinctUntilChanged(
                    keySelector:    versions => versions.Select(version => version.Id),
                    comparer:       SequenceEqualityComparer<ulong>.Default)
                .ShareReplay(1);

            _nextAccountId = allVersions
                .Select(versions => versions
                    .Select(version => version.AccountId)
                    .DefaultIfEmpty()
                    .Max())
                .Select(maxAccountId => maxAccountId + 1)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _nextVersionId = allVersions
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

        public IObservable<AccountCreatedEvent> Create(IObservable<CreationModel> createRequested)
            => createRequested
                .WithLatestFrom(
                    Observable.CombineLatest(
                        _nextAccountId,
                        _nextVersionId,
                        _auditingRepository.NextActionId,
                        (nextAccountId, nextVersionId, nextActionId) => (nextAccountId, nextVersionId, nextActionId)),
                    (model, state) => new AccountCreatedEvent()
                    {
                        Action  = new AuditedActionEntity()
                        {
                            Id          = state.nextActionId,
                            Performed   = _systemClock.Now,
                            TypeId      = Auditing.ActionTypes.AccountCreated.Id
                        },
                        Version = new VersionEntity()
                        { 
                            Id                  = state.nextVersionId,
                            AccountId           = state.nextAccountId,
                            CreationId          = state.nextActionId,
                            Description         = model.Description,
                            Name                = model.Name,
                            ParentAccountId     = model.ParentAccountId,
                            Type                = model.Type
                        }
                    })
                .Do(@event => _dataState.Value = _dataState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = _dataState.Value.LoadedFile with
                    {
                        Database    = _dataState.Value.LoadedFile.Database with
                        {
                            AccountVersions = _dataState.Value.LoadedFile.Database.AccountVersions
                                .Add(@event.Version),
                            AuditingActions = _dataState.Value.LoadedFile.Database.AuditingActions
                                .Add(@event.Action)
                        },
                        HasChanges  = true
                    }
                });

        public IObservable<AccountMutatedEvent> Mutate(IObservable<MutationModel> mutateRequested)
            => mutateRequested
                .WithLatestFrom(
                    Observable.CombineLatest(
                        _currentVersions,
                        _nextVersionId,
                        _auditingRepository.NextActionId,
                        (currentVersions, nextVersionId, nextActionId) => (currentVersions, nextVersionId, nextActionId)),
                    (model, state) =>
                    {
                        var currentVersion = state.currentVersions
                            .First(version => version.AccountId == model.AccountId);

                        return new AccountMutatedEvent()
                        {
                            Action      = new AuditedActionEntity()
                            {
                                Id          = state.nextActionId,
                                Performed   = _systemClock.Now,
                                TypeId      = Auditing.ActionTypes.AccountMutated.Id
                            },
                            NewVersion  = currentVersion with
                            {
                                Id                  = state.nextVersionId,
                                CreationId          = state.nextActionId,
                                Description         = model.Description,
                                Name                = model.Name,
                                ParentAccountId     = model.ParentAccountId,
                                Type                = model.Type
                            },
                            OldVersion  = currentVersion
                        };
                    })
                .Do(@event => _dataState.Value = _dataState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = _dataState.Value.LoadedFile with
                    {
                        Database    = _dataState.Value.LoadedFile.Database with
                        {
                            AccountVersions = _dataState.Value.LoadedFile.Database.AccountVersions
                                .Replace(@event.OldVersion, @event.NewVersion),
                            AuditingActions = _dataState.Value.LoadedFile.Database.AuditingActions
                                .Add(@event.Action)
                        },
                        HasChanges  = true
                    }
                });

        private readonly Data.Auditing.Repository                   _auditingRepository;
        private readonly IObservable<IReadOnlyList<VersionEntity>>  _currentVersions;
        private readonly DataStateStore                             _dataState;
        private readonly IObservable<ulong>                         _nextAccountId;
        private readonly IObservable<ulong>                         _nextVersionId;
        private readonly SystemClock                                _systemClock;
    }
}
