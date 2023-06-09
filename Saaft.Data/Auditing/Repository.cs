﻿using System;
using System.Linq;
using System.Reactive.Linq;

using Saaft.Data.Database;

namespace Saaft.Data.Auditing
{
    public class Repository
    {
        public Repository(FileStateStore fileState)
            => _nextActionId = fileState.Events
                .StartWith(null as FileStateEvent)
                .WithLatestFrom(
                    fileState.Select(static fileState => fileState.LoadedFile.Database.AuditingActions),
                    static (@event, actions) => (@event, actions))
                .Scan(1UL, static (nextActionId, @params) => @params.@event switch
                {
                    null or FileLoadedEvent or NewFileLoadedEvent or FileClosedEvent
                        => @params.actions
                            .Select(action => action.Id)
                            .DefaultIfEmpty()
                            .Max() + 1,
                    AuditedActionEvent actionEvent
                        => actionEvent.Action.Id + 1,
                    _   => nextActionId
                })
                .DistinctUntilChanged()
                .ShareReplay(1);

        public IObservable<ulong> NextActionId
            => _nextActionId;

        private readonly IObservable<ulong> _nextActionId;
    }
}
