using System;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
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
        : DisposableBase,
            IHostedModel
    {
        public FileWorkspaceModel(
            FileStateStore  fileState,
            ModelFactory    modelFactory,
            Repository      repository)
        {
            _closeRequested = new();
            _hostRequested  = new();
            _repository     = repository;

            _closeFileCommand = ReactiveActionCommand.Create(
                canExecute:         fileState
                    .Select(static fileState => fileState.LoadedFile != FileEntity.None)
                    .DistinctUntilChanged(),
                executeOperation:   executeRequested => executeRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySaveIfNeeded)
                    .SelectUnit()
                    .ApplyOperation(repository.CloseFile)
                    .SelectUnit());

            _file = fileState
                .Select(fileState => fileState.LoadedFile != FileEntity.None)
                .DistinctUntilChanged()
                .Select(isFileOpen => isFileOpen
                    ? modelFactory.CreateFileView()
                    : null)
                .ToReactiveReadOnlyValue();

            _newFileCommand = ReactiveActionCommand.Create(
                executeOperation: executeRequested => executeRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySaveIfNeeded)
                    .SelectUnit()
                    .ApplyOperation(repository.LoadNewFile)
                    .SelectUnit());

            _openFileCommand = ReactiveActionCommand.Create(
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

            _saveFileCommand = ReactiveActionCommand.Create(
                canExecute: fileState
                    .Select(static fileState => fileState.LoadedFile != FileEntity.None && fileState.LoadedFile.HasChanges)
                    .DistinctUntilChanged(),
                executeOperation: executeRequested => executeRequested
                    .WithLatestFrom(fileState, static (_, fileState) => fileState.LoadedFile)
                    .ApplyOperation(TrySave)
                    .SelectUnit());

            _title = ReactiveReadOnlyValue.Create("Simple Accounting and Finance Toolkit");
        }

        public IObservable<Unit> Closed
            => _closeRequested;

        public IReactiveActionCommand CloseFileCommand
            => _closeFileCommand;

        public ReactiveReadOnlyValue<FileViewModel?> File
            => _file;

        public IReactiveActionCommand NewFileCommand
            => _newFileCommand;

        public IObserver<Unit> OnCloseRequested
            => _closeRequested;

        public IReactiveActionCommand OpenFileCommand
            => _openFileCommand;

        public IObservable<IHostedModel> HostRequested
            => _hostRequested;

        public IReactiveActionCommand SaveFileCommand
            => _saveFileCommand;

        public ReactiveReadOnlyValue<string> Title
            => _title;

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
            {
                _closeRequested.OnCompleted();
                _hostRequested.OnCompleted();

                _closeFileCommand   .Dispose();
                _closeRequested     .Dispose();
                _hostRequested      .Dispose();
                _newFileCommand     .Dispose();
                _openFileCommand    .Dispose();
                _saveFileCommand    .Dispose();
            }
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

        private readonly ReactiveActionCommand                  _closeFileCommand;
        private readonly Subject<Unit>                          _closeRequested;
        private readonly ReactiveReadOnlyValue<FileViewModel?>  _file;
        private readonly Subject<IHostedModel>                  _hostRequested;
        private readonly ReactiveActionCommand                  _newFileCommand;
        private readonly ReactiveActionCommand                  _openFileCommand;
        private readonly Repository                             _repository;
        private readonly ReactiveActionCommand                  _saveFileCommand;
        private readonly ReactiveReadOnlyValue<string>          _title;

        private const string _filePromptFilter
            = "Simple Accounting Database (*.saaft)|*.saaft";
    }
}
