using System;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Windows.Input;

using Saaft.Data;
using Saaft.Desktop.Database;

namespace Saaft.Desktop.Workspaces
{
    public sealed class MainWorkspaceModel
        : ModelBase,
            IDisposable
    {
        public MainWorkspaceModel(
            Data.Database.Repository    databaseRepository,
            DataStateStore              dataState,
            Database.ModelFactory       modelFactory)
        {
            _closeFileCommandExecuted   = new();
            _databaseRepository         = databaseRepository;
            _newFileCommandExecuted     = new();
            _openFileCommandExecuted    = new();
            _promptRequested            = new();
            _saveFileCommandExecuted    = new();
            _subscriptions              = new();

            _closeFileCommandExecuted
                .WithLatestFrom(dataState, static (_, dataState) => dataState.LoadedFile)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(static _ => Unit.Default)
                .ApplyOperation(databaseRepository.CloseFile)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _closeFileCommand = ReactiveCommand.Create(
                onExecuted: _closeFileCommandExecuted,
                canExecute: dataState
                    .Select(static file => file is not null)
                    .DistinctUntilChanged());

            _file = dataState
                .Select(dataState => dataState.LoadedFile != Data.Database.FileEntity.None)
                .DistinctUntilChanged()
                .Select(isFileOpen => isFileOpen
                    ? modelFactory.CreateFileView()
                    : null)
                .ToReactiveProperty();

            _newFileCommandExecuted
                .WithLatestFrom(dataState, static (_, dataState) => dataState.LoadedFile)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(static _ => Unit.Default)
                .ApplyOperation(databaseRepository.LoadNewFile)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _newFileCommand = ReactiveCommand.Create(
                onExecuted: _newFileCommandExecuted);

            _openFileCommandExecuted
                .WithLatestFrom(dataState, static (_, dataState) => dataState.LoadedFile)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(loadedFile => ReactiveDisposable
                    .Create(() => new OpenFilePromptModel()
                    {
                        Filter          = _filePromptFilter,
                        InitialFilePath = loadedFile.FilePath
                    })
                    .Do(prompt => _promptRequested.OnNext(prompt))
                    .Select(static prompt => prompt.Result)
                    .Switch()
                    .Select(static filePath => Observable.FromAsync(async cancellationToken =>
                    {
                        using var fileStream = System.IO.File.OpenRead(filePath);
                    
                        return new Data.Database.FileEntity()
                        {
                            Database    = await JsonSerializer.DeserializeAsync<Data.Database.Entity>(
                                    utf8Json:           fileStream,
                                    cancellationToken:  cancellationToken)
                                ?? throw new InvalidOperationException("File is empty"),
                            FilePath    = filePath,
                            HasChanges  = false
                        };
                    }))
                    .Switch()
                    .ObserveOn(DispatcherScheduler.Current))
                .Switch()
                .ApplyOperation(databaseRepository.LoadFile)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _openFileCommand = ReactiveCommand.Create(
                onExecuted: _openFileCommandExecuted);

            _saveFileCommandExecuted
                .WithLatestFrom(dataState, static (_, dataState) => dataState.LoadedFile)
                .ApplyOperation(TrySave)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _saveFileCommand = ReactiveCommand.Create(
                onExecuted: _saveFileCommandExecuted,
                canExecute: dataState
                    .Select(static dataState => (dataState.LoadedFile != Data.Database.FileEntity.None) && dataState.LoadedFile.HasChanges)
                    .DistinctUntilChanged());

            _title = ReactiveProperty.Create("Simple Accounting and Finance Toolkit");
        }

        public ReactiveCommand CloseFileCommand
            => _closeFileCommand;

        public ReactiveProperty<FileViewModel?> File
            => _file;

        public ReactiveCommand NewFileCommand
            => _newFileCommand;

        public ReactiveCommand OpenFileCommand
            => _openFileCommand;

        public IObservable<object> PromptRequested
            => _promptRequested;

        public ReactiveCommand SaveFileCommand
            => _saveFileCommand;

        public override ReactiveProperty<string> Title
            => _title;

        public void Dispose()
        {
            _closeFileCommandExecuted.OnCompleted();
            _closeFileCommandExecuted.Dispose();
            _newFileCommandExecuted.OnCompleted();
            _newFileCommandExecuted.Dispose();
            _openFileCommandExecuted.OnCompleted();
            _openFileCommandExecuted.Dispose();
            _promptRequested.OnCompleted();
            _promptRequested.Dispose();
            _saveFileCommandExecuted.OnCompleted();
            _saveFileCommandExecuted.Dispose();
            _subscriptions.Dispose();
        }

        private IObservable<Data.Database.FileEntity> TrySaveIfNeeded(IObservable<Data.Database.FileEntity> loadedFile)
            => loadedFile
                .Select(loadedFile => ((loadedFile != Data.Database.FileEntity.None) && loadedFile.HasChanges)
                    ? ReactiveDisposable
                        .Create(() => new DecisionPromptModel()
                        {
                            Message = $"The file \"{((loadedFile.FilePath is null) ? Data.Database.FileEntity.DefaultFilename : Path.GetFileName(loadedFile.FilePath))}\' has unsaved changes. Would you like to save before continuing?",
                            Title   = "Save changes?"
                        })
                        .Do(prompt => _promptRequested.OnNext(prompt))
                        .Select(static prompt => prompt.Result)
                        .Switch()
                        .Select(promptResult => promptResult
                            ? Observable
                                .Return(loadedFile)
                                .ApplyOperation(TrySave)
                            : Observable.Return(loadedFile))
                        .Switch()
                    : Observable.Return(loadedFile))
                .Switch();

        private IObservable<Data.Database.FileEntity> TrySave(IObservable<Data.Database.FileEntity> loadedFile)
            => loadedFile
                .Select(loadedFile => Observable.Return((loadedFile.FilePath is string targetFilePath)
                        ? Observable.Return((loadedFile, targetFilePath))
                        : ReactiveDisposable
                            .Create(static () => new SaveFilePromptModel()
                            {
                                Filter          = _filePromptFilter,
                                InitialFilePath = Data.Database.FileEntity.DefaultFilename,
                            })
                            .Do(prompt => _promptRequested.OnNext(prompt))
                            .Select(static prompt => prompt.Result)
                            .Switch()
                            .Select(targetFilePath => (loadedFile, targetFilePath)))
                    .Switch()
                    .Select(static @params => Observable.FromAsync(async cancellationToken =>
                    {
                        using var fileStream = System.IO.File.Open(@params.targetFilePath, FileMode.Create);
                    
                        await JsonSerializer.SerializeAsync(
                            utf8Json:           fileStream,
                            value:              @params.loadedFile.Database,
                            cancellationToken:  cancellationToken);

                        return @params.targetFilePath;
                    }))
                    .Switch()
                    .ObserveOn(DispatcherScheduler.Current)
                    .ApplyOperation(_databaseRepository.SaveFile)
                    .Select(_ => loadedFile))
                .Switch();

        private readonly ReactiveCommand                    _closeFileCommand;
        private readonly Subject<Unit>                      _closeFileCommandExecuted;
        private readonly Data.Database.Repository           _databaseRepository;
        private readonly ReactiveProperty<FileViewModel?>   _file;
        private readonly ReactiveCommand                    _newFileCommand;
        private readonly Subject<Unit>                      _newFileCommandExecuted;
        private readonly ReactiveCommand                    _openFileCommand;
        private readonly Subject<Unit>                      _openFileCommandExecuted;
        private readonly Subject<object>                    _promptRequested;
        private readonly ReactiveCommand                    _saveFileCommand;
        private readonly Subject<Unit>                      _saveFileCommandExecuted;
        private readonly CompositeDisposable                _subscriptions;
        private readonly ReactiveProperty<string>           _title;

        private const string _filePromptFilter
            = "Simple Accounting Database (*.saaft)|*.saaft";
    }
}
