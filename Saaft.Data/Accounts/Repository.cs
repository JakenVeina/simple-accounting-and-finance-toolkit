﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Collections;
using System.Reactive.Linq;

using Saaft.Data.Auditing;
using Saaft.Data.Database;

namespace Saaft.Data.Accounts
{
    public class Repository
    {
        public Repository(
            Data.Auditing.Repository    auditingRepository,
            Database.Repository         databaseRepository,
            FileStateStore              fileState,
            SystemClock                 systemClock)
        {
            _auditingRepository = auditingRepository;
            _fileState          = fileState;
            _systemClock        = systemClock;

            var allVersions = databaseRepository.LoadedDatabase
                .Select(database => database.AccountVersions)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _currentVersions = allVersions
                .Select(versions => versions
                    .Where(version => versions
                        .All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                    .OrderBy(static version => version.Id)
                    .ToList())
                .DistinctUntilChanged(
                    keySelector:    static versions => versions.Select(version => version.Id),
                    comparer:       SequenceEqualityComparer<ulong>.Default)
                .ShareReplay(1);

            _nextAccountId = fileState.Events
                .StartWith(null as FileStateEvent)
                .WithLatestFrom(
                    fileState.Select(static fileState => fileState.LoadedFile.Database.AccountVersions),
                    static (@event, versions) => (@event, versions))
                .Scan(1UL, static (nextAccountId, @params) => @params.@event switch
                {
                    null or FileLoadedEvent or NewFileLoadedEvent or FileClosedEvent
                        => @params.versions
                            .Select(version => version.AccountId)
                            .DefaultIfEmpty()
                            .Max() + 1,
                    AccountCreatedEvent creationEvent
                        => creationEvent.Version.AccountId + 1,
                    _   => nextAccountId
                })
                .DistinctUntilChanged()
                .ShareReplay(1);

            _nextVersionId = fileState.Events
                .StartWith(null as FileStateEvent)
                .WithLatestFrom(
                    fileState.Select(static fileState => fileState.LoadedFile.Database.AccountVersions),
                    static (@event, versions) => (@event, versions))
                .Scan(1UL, static (nextVersionId, @params) => @params.@event switch
                {
                    null or FileLoadedEvent or NewFileLoadedEvent or FileClosedEvent
                        => @params.versions
                            .Select(version => version.Id)
                            .DefaultIfEmpty()
                            .Max() + 1,
                    AccountCreatedEvent creationEvent
                        => creationEvent.Version.Id + 1,
                    AccountMutatedEvent creationEvent
                        => creationEvent.NewVersion.Id + 1,
                    _   => nextVersionId
                })
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
                        static (nextAccountId, nextVersionId, nextActionId) => (nextAccountId, nextVersionId, nextActionId)),
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
                .Do(@event => _fileState.Value = _fileState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = _fileState.Value.LoadedFile with
                    {
                        Database    = _fileState.Value.LoadedFile.Database with
                        {
                            AccountVersions = _fileState.Value.LoadedFile.Database.AccountVersions
                                .Add(@event.Version),
                            AuditingActions = _fileState.Value.LoadedFile.Database.AuditingActions
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
                        static (currentVersions, nextVersionId, nextActionId) => (currentVersions, nextVersionId, nextActionId)),
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
                .Do(@event => _fileState.Value = _fileState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = _fileState.Value.LoadedFile with
                    {
                        Database    = _fileState.Value.LoadedFile.Database with
                        {
                            AccountVersions = _fileState.Value.LoadedFile.Database.AccountVersions
                                .Replace(@event.OldVersion, @event.NewVersion),
                            AuditingActions = _fileState.Value.LoadedFile.Database.AuditingActions
                                .Add(@event.Action)
                        },
                        HasChanges  = true
                    }
                });

        public IObservable<ReactiveCollectionAction<T>> ObserveCurrentVersions<T>(
                Func<VersionEntity, bool>                                           filterPredicate,
                Func<IEnumerable<VersionEntity>, IOrderedEnumerable<VersionEntity>> orderByClause,
                Func<VersionEntity, T>                                              selector)
            => _fileState.Events
                .StartWith(null as FileStateEvent)
                .WithLatestFrom(
                    Observable.Zip(
                        CurrentVersions.StartWith(ImmutableList<VersionEntity>.Empty),
                        CurrentVersions,
                        static (prior, current) => (prior, current)),
                    (@event, versions) => @event switch
                {
                    null or FileLoadedEvent or NewFileLoadedEvent
                        => Observable.Return(ReactiveCollectionAction.Reset(versions.current
                            .Where(filterPredicate)
                            .ApplyOrderByClause(orderByClause)
                            .Select(selector)
                            .ToArray())),
                    FileClosedEvent
                        => Observable.Return(ReactiveCollectionAction.Clear<T>()),
                    AccountCreatedEvent creation
                        => filterPredicate.Invoke(creation.Version)
                            ? Observable.Return(ReactiveCollectionAction.Insert(
                                index:  versions.current
                                    .Where(filterPredicate)
                                    .ApplyOrderByClause(orderByClause)
                                    .IndexOf(creation.Version),
                                item:   selector.Invoke(creation.Version)))
                            : Observable.Empty<ReactiveCollectionAction<T>>(),
                    AccountMutatedEvent mutation
                        => mutation switch
                        {
                            _ when filterPredicate.Invoke(mutation.NewVersion)
                                => filterPredicate.Invoke(mutation.OldVersion)
                                    ? Observable.Return((
                                            oldIndex:   versions.prior
                                                .Where(filterPredicate)
                                                .ApplyOrderByClause(orderByClause)
                                                .IndexOf(mutation.OldVersion),
                                            newIndex:   versions.current
                                                .Where(filterPredicate)
                                                .ApplyOrderByClause(orderByClause)
                                                .IndexOf(mutation.NewVersion)))
                                        .Where(movement => movement.oldIndex != movement.newIndex)
                                        .Select(movement => ReactiveCollectionAction.Move<T>(movement.oldIndex, movement.newIndex))
                                    : Observable.Return(ReactiveCollectionAction.Insert(
                                        index:  versions.current
                                            .Where(filterPredicate)
                                            .ApplyOrderByClause(orderByClause)
                                            .IndexOf(mutation.NewVersion),
                                        item:   selector.Invoke(mutation.NewVersion))),
                            _ when filterPredicate.Invoke(mutation.OldVersion)
                                => Observable.Return(ReactiveCollectionAction.Remove<T>(versions.prior
                                    .Where(filterPredicate)
                                    .ApplyOrderByClause(orderByClause)
                                    .IndexOf(mutation.OldVersion))),
                            _   => Observable.Empty<ReactiveCollectionAction<T>>()
                        },
                    _   => Observable.Empty<ReactiveCollectionAction<T>>()
                })
                .Merge();

        private readonly Data.Auditing.Repository                   _auditingRepository;
        private readonly IObservable<IReadOnlyList<VersionEntity>>  _currentVersions;
        private readonly FileStateStore                             _fileState;
        private readonly IObservable<ulong>                         _nextAccountId;
        private readonly IObservable<ulong>                         _nextVersionId;
        private readonly SystemClock                                _systemClock;
    }
}
