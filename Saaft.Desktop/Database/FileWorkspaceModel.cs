using System;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Windows.Input;

using Saaft.Data.Database;
using Saaft.Desktop.Prompts;

namespace Saaft.Desktop.Database
{
    public sealed class FileWorkspaceModel
        : IHostedModel,
            IDisposable
    {
        public FileWorkspaceModel(
            FileStateStore  fileState,
            ModelFactory    modelFactory,
            Repository      repository)
        {
            _closeRequested = new();
            _hostRequested  = new();
            _repository     = repository;

            _closeFileCommand = ReactiveCommand.Create(
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySaveIfNeeded)
                    .SelectUnit()
                    .ApplyOperation(repository.CloseFile)
                    .SelectUnit(),
                canExecute:         fileState
                    .Select(static fileState => fileState.LoadedFile != FileEntity.None)
                    .DistinctUntilChanged());

            _file = fileState
                .Select(fileState => fileState.LoadedFile != FileEntity.None)
                .DistinctUntilChanged()
                .Select(isFileOpen => isFileOpen
                    ? modelFactory.CreateFileView()
                    : null)
                .ToReactiveReadOnlyValue();

            _newFileCommand = ReactiveCommand.Create(
                executeOperation: onExecuteRequested => onExecuteRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySaveIfNeeded)
                    .SelectUnit()
                    .ApplyOperation(repository.LoadNewFile)
                    .SelectUnit());

            _openFileCommand = ReactiveCommand.Create(
                executeOperation: onExecuteRequested => onExecuteRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySaveIfNeeded)
                    .Select(loadedFile => ReactiveDisposable
                        .Create(() => new OpenFilePromptModel(
                            title:              "Open",
                            initialFilePath:    loadedFile.FilePath,
                            filter:             _filePromptFilter))
                        .Select(prompt => prompt.Result
                            .OnSubscribed(() => _hostRequested.OnNext(prompt)))
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
                    .SelectUnit());

            _saveFileCommand = ReactiveCommand.Create(
                executeOperation: onExecuteRequested => onExecuteRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySave)
                    .SelectUnit(),
                canExecute: fileState
                    .Select(static fileState => fileState.LoadedFile != FileEntity.None && fileState.LoadedFile.HasChanges)
                    .DistinctUntilChanged());

            _title = ReactiveReadOnlyValue.Create("Simple Accounting and Finance Toolkit");
        }

        public IObservable<Unit> Closed
            => _closeRequested;

        public ReactiveCommand CloseFileCommand
            => _closeFileCommand;

        public ReactiveReadOnlyValue<FileViewModel?> File
            => _file;

        public ReactiveCommand NewFileCommand
            => _newFileCommand;

        public IObserver<Unit> OnCloseRequested
            => _closeRequested;

        public ReactiveCommand OpenFileCommand
            => _openFileCommand;

        public IObservable<IHostedModel> HostRequested
            => _hostRequested;

        public ReactiveCommand SaveFileCommand
            => _saveFileCommand;

        public ReactiveReadOnlyValue<string> Title
            => _title;

        public void Dispose()
        {
            _closeRequested.OnCompleted();
            _hostRequested.OnCompleted();

            _closeRequested.Dispose();
            _hostRequested.Dispose();
        }

        private IObservable<FileEntity> TrySaveIfNeeded(IObservable<FileEntity> loadedFile)
            => loadedFile
                .Select(loadedFile => loadedFile != FileEntity.None && loadedFile.HasChanges
                    ? ReactiveDisposable
                        .Create(() => new DecisionPromptModel(
                            title:      "Save changes?",
                            message:    $"The file \"{(loadedFile.FilePath is null ? FileEntity.DefaultFilename : Path.GetFileName(loadedFile.FilePath))}\' has unsaved changes. Would you like to save before continuing?"))
                        .Select(prompt => prompt.Result
                            .OnSubscribed(() => _hostRequested.OnNext(prompt)))
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
                            .Select(prompt => prompt.Result
                                .OnSubscribed(() => _hostRequested.OnNext(prompt)))
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

        private readonly ReactiveCommand                        _closeFileCommand;
        private readonly Subject<Unit>                          _closeRequested;
        private readonly ReactiveReadOnlyValue<FileViewModel?>  _file;
        private readonly Subject<IHostedModel>                  _hostRequested;
        private readonly ReactiveCommand                        _newFileCommand;
        private readonly ReactiveCommand                        _openFileCommand;
        private readonly Repository                             _repository;
        private readonly ReactiveCommand                        _saveFileCommand;
        private readonly ReactiveReadOnlyValue<string>          _title;

        private const string _filePromptFilter
            = "Simple Accounting Database (*.saaft)|*.saaft";
    }
}
