using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Saaft.Data.Database
{
    public class Repository
    {
        public Repository(DataStateStore dataState)
        {
            _dataState = dataState;

            _loadedDatabase = dataState
                .Select(state => state.LoadedFile.Database)
                .DistinctUntilChanged()
                .ShareReplay(1);
        }

        public IObservable<Entity> LoadedDatabase
            => _loadedDatabase;

        public IObservable<FileClosedEvent> CloseFile(IObservable<Unit> closeFileRequested)
            => closeFileRequested
                .Select(_ => new FileClosedEvent()
                {
                    File = _dataState.Value.LoadedFile
                })
                .Do(@event => _dataState.Value = _dataState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = FileEntity.None
                });

        public IObservable<FileLoadedEvent> LoadFile(IObservable<FileEntity> loadFileRequested)
            => loadFileRequested
                .Select(newFile => new FileLoadedEvent()
                {
                    NewFile = newFile,
                    OldFile = _dataState.Value.LoadedFile
                })
                .Do(@event => _dataState.Value = _dataState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = @event.NewFile
                });

        public IObservable<NewFileLoadedEvent> LoadNewFile(IObservable<Unit> loadNewFileRequested)
            => loadNewFileRequested
                .Select(_ => new NewFileLoadedEvent()
                {
                    OldFile = _dataState.Value.LoadedFile
                })
                .Do(@event => _dataState.Value = _dataState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = FileEntity.New
                });

        public IObservable<FileSavedEvent> SaveFile(IObservable<string> saveFileRequested)
            => saveFileRequested
                .Select(filePath => new FileSavedEvent()
                {
                    NewFilePath = filePath,
                    OldFilePath = _dataState.Value.LoadedFile.FilePath
                })
                .Do(@event => _dataState.Value = _dataState.Value with
                {
                    LatestEvent = @event,
                    LoadedFile  = _dataState.Value.LoadedFile with
                    {
                        FilePath    = @event.NewFilePath,
                        HasChanges  = false
                    }
                });

        private readonly DataStateStore         _dataState;
        private readonly IObservable<Entity>    _loadedDatabase;
    }
}
