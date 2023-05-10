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

using Saaft.Data.Database;
using Saaft.Desktop.Prompts;

namespace Saaft.Desktop.Database
{
    public sealed class FileWorkspaceModel
        : HostedModelBase,
            IDisposable
    {
        public FileWorkspaceModel(
            FileStateStore  fileState,
            ModelFactory    modelFactory,
            Repository      repository)
        {
            _closeFileCommandExecuted   = new();
            _hostRequested              = new();
            _newFileCommandExecuted     = new();
            _openFileCommandExecuted    = new();
            _repository                 = repository;
            _saveFileCommandExecuted    = new();
            _subscriptions              = new();

            _closeFileCommandExecuted
                .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(static _ => Unit.Default)
                .ApplyOperation(repository.CloseFile)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _closeFileCommand = ReactiveCommand.Create(
                onExecuted: _closeFileCommandExecuted,
                canExecute: fileState
                    .Select(static fileState => fileState.LoadedFile != FileEntity.None)
                    .DistinctUntilChanged());

            _file = fileState
                .Select(fileState => fileState.LoadedFile != FileEntity.None)
                .DistinctUntilChanged()
                .Select(isFileOpen => isFileOpen
                    ? modelFactory.CreateFileView()
                    : null)
                .ToReactiveReadOnlyProperty();

            _newFileCommandExecuted
                .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(static _ => Unit.Default)
                .ApplyOperation(repository.LoadNewFile)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _newFileCommand = ReactiveCommand.Create(
                onExecuted: _newFileCommandExecuted);

            _openFileCommandExecuted
                .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(loadedFile => ReactiveDisposable
                    .Create(() => new OpenFilePromptModel(
                        title:              "Open",
                        initialFilePath:    loadedFile.FilePath,
                        filter:             _filePromptFilter))
                    .Do(prompt => _hostRequested.OnNext(prompt))
                    .Select(static prompt => prompt.Result)
                    .Switch()
                    .Select(static filePath => Observable.FromAsync(async cancellationToken =>
                    {
                        using var fileStream = System.IO.File.OpenRead(filePath);

                        return new FileEntity()
                        {
                            Database    = await JsonSerializer.DeserializeAsync<Entity>(
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
                .ApplyOperation(repository.LoadFile)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _openFileCommand = ReactiveCommand.Create(
                onExecuted: _openFileCommandExecuted);

            _saveFileCommandExecuted
                .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                .ApplyOperation(TrySave)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _saveFileCommand = ReactiveCommand.Create(
                onExecuted: _saveFileCommandExecuted,
                canExecute: fileState
                    .Select(static fileState => fileState.LoadedFile != FileEntity.None && fileState.LoadedFile.HasChanges)
                    .DistinctUntilChanged());

            _title = ReactiveReadOnlyProperty.Create("Simple Accounting and Finance Toolkit");
        }

        public ReactiveCommand CloseFileCommand
            => _closeFileCommand;

        public ReactiveReadOnlyProperty<FileViewModel?> File
            => _file;

        public ReactiveCommand NewFileCommand
            => _newFileCommand;

        public ReactiveCommand OpenFileCommand
            => _openFileCommand;

        public IObservable<HostedModelBase> HostRequested
            => _hostRequested;

        public ReactiveCommand SaveFileCommand
            => _saveFileCommand;

        public override ReactiveReadOnlyProperty<string> Title
            => _title;

        protected override void OnDisposing(DisposalType type)
        {
            _subscriptions.Dispose();

            _closeFileCommandExecuted.OnCompleted();
            _hostRequested.OnCompleted();
            _newFileCommandExecuted.OnCompleted();
            _openFileCommandExecuted.OnCompleted();
            _saveFileCommandExecuted.OnCompleted();

            _closeFileCommandExecuted.Dispose();
            _hostRequested.Dispose();
            _newFileCommandExecuted.Dispose();
            _openFileCommandExecuted.Dispose();
            _saveFileCommandExecuted.Dispose();
        }

        private IObservable<FileEntity> TrySaveIfNeeded(IObservable<FileEntity> loadedFile)
            => loadedFile
                .Select(loadedFile => loadedFile != FileEntity.None && loadedFile.HasChanges
                    ? ReactiveDisposable
                        .Create(() => new DecisionPromptModel(
                            title:      "Save changes?",
                            message:    $"The file \"{(loadedFile.FilePath is null ? FileEntity.DefaultFilename : Path.GetFileName(loadedFile.FilePath))}\' has unsaved changes. Would you like to save before continuing?"))
                        .Do(prompt => _hostRequested.OnNext(prompt))
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

        private IObservable<FileEntity> TrySave(IObservable<FileEntity> loadedFile)
            => loadedFile
                .Select(loadedFile => Observable.Return(loadedFile.FilePath is string targetFilePath
                        ? Observable.Return((loadedFile, targetFilePath))
                        : ReactiveDisposable
                            .Create(static () => new SaveFilePromptModel(
                                title:              "Save",
                                initialFilePath:    FileEntity.DefaultFilename,
                                filter:             _filePromptFilter))
                            .Do(prompt => _hostRequested.OnNext(prompt))
                            .Select(static prompt => prompt.Result)
                            .Switch()
                            .Select(targetFilePath => (loadedFile, targetFilePath)))
                    .Switch()
                    .Select(static @params => Observable.FromAsync(async cancellationToken =>
                    {
                        using var fileStream = System.IO.File.Open(@params.targetFilePath, FileMode.Create);

                        await JsonSerializer.SerializeAsync(
                            utf8Json: fileStream,
                            value: @params.loadedFile.Database,
                            cancellationToken: cancellationToken);

                        return @params.targetFilePath;
                    }))
                    .Switch()
                    .ObserveOn(DispatcherScheduler.Current)
                    .ApplyOperation(_repository.SaveFile)
                    .Select(_ => loadedFile))
                .Switch();

        private readonly ReactiveCommand                            _closeFileCommand;
        private readonly Subject<Unit>                              _closeFileCommandExecuted;
        private readonly ReactiveReadOnlyProperty<FileViewModel?>   _file;
        private readonly Subject<HostedModelBase>                   _hostRequested;
        private readonly ReactiveCommand                            _newFileCommand;
        private readonly Subject<Unit>                              _newFileCommandExecuted;
        private readonly ReactiveCommand                            _openFileCommand;
        private readonly Subject<Unit>                              _openFileCommandExecuted;
        private readonly Repository                                 _repository;
        private readonly ReactiveCommand                            _saveFileCommand;
        private readonly Subject<Unit>                              _saveFileCommandExecuted;
        private readonly CompositeDisposable                        _subscriptions;
        private readonly ReactiveReadOnlyProperty<string>           _title;

        private const string _filePromptFilter
            = "Simple Accounting Database (*.saaft)|*.saaft";
    }
}
