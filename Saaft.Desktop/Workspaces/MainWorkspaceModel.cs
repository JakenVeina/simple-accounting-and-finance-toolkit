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
using Saaft.Data.Database;
using Saaft.Desktop.Database;

namespace Saaft.Desktop.Workspaces
{
    public sealed class MainWorkspaceModel
        : ModelBase,
            IDisposable
    {
        public MainWorkspaceModel(
            DataStore               dataStore,
            Database.ModelFactory   modelFactory)
        {
            _closeFileCommandExecuted   = new();
            _newFileCommandExecuted     = new();
            _openFileCommandExecuted    = new();
            _promptRequested            = new();
            _saveFileCommandExecuted    = new();
            _subscriptions              = new();

            _closeFileCommandExecuted
                .WithLatestFrom(dataStore, (_, file) => file)
                .ApplyOperation(TrySaveIfNeeded)
                .Subscribe(_ => dataStore.Value = null)
                .DisposeWith(_subscriptions);

            _closeFileCommand = ReactiveCommand.Create(
                onExecuted: _closeFileCommandExecuted,
                canExecute: dataStore
                    .Select(file => file is not null)
                    .DistinctUntilChanged());

            _file = dataStore
                .Select(file => file is not null)
                .DistinctUntilChanged()
                .Select(isFileOpen => isFileOpen
                    ? modelFactory.CreateFileView()
                    : null)
                .ToReactiveProperty();

            _newFileCommandExecuted
                .WithLatestFrom(dataStore, (_, file) => file)
                .ApplyOperation(TrySaveIfNeeded)
                .Subscribe(_ => dataStore.Value = FileEntity.New)
                .DisposeWith(_subscriptions);

            _newFileCommand = ReactiveCommand.Create(
                onExecuted: _newFileCommandExecuted);

            _openFileCommandExecuted
                .WithLatestFrom(dataStore, (_, file) => file)
                .ApplyOperation(TrySaveIfNeeded)
                .Select(file => ReactiveDisposable
                    .Create(() => new OpenFilePromptModel()
                    {
                        Filter          = _filePromptFilter,
                        InitialFilePath = file?.FilePath
                    })
                    .Do(prompt => _promptRequested.OnNext(prompt))
                    .Select(prompt => prompt.Result)
                    .Switch()
                    .Select(filePath => Observable.FromAsync(async cancellationToken =>
                    {
                        using var fileStream = System.IO.File.OpenRead(filePath);
                    
                        return new FileEntity()
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
                .Subscribe(file => dataStore.Value = file)
                .DisposeWith(_subscriptions);

            _openFileCommand = ReactiveCommand.Create(
                onExecuted: _openFileCommandExecuted);

            _saveFileCommandExecuted
                .WithLatestFrom(dataStore, (_, file) => file)
                .WhereNotNull()
                .ApplyOperation(TrySave)
                .Subscribe(file => dataStore.Value = file)
                .DisposeWith(_subscriptions);

            _saveFileCommand = ReactiveCommand.Create(
                onExecuted: _saveFileCommandExecuted,
                canExecute: dataStore
                    .Select(file => (file is not null) && file.HasChanges)
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

        private IObservable<FileEntity?> TrySaveIfNeeded(IObservable<FileEntity?> file)
            => file
                .Select(file => ((file is not null) && file.HasChanges)
                    ? ReactiveDisposable
                        .Create(() => new DecisionPromptModel()
                        {
                            Message = $"The file \"{((file.FilePath is null) ? FileEntity.DefaultFilename : Path.GetFileName(file.FilePath))}\' has unsaved changes. Would you like to save before continuing?",
                            Title   = "Save changes?"
                        })
                        .Do(prompt => _promptRequested.OnNext(prompt))
                        .Select(prompt => prompt.Result)
                        .Switch()
                        .Select(promptResult => promptResult
                            ? Observable
                                .Return(file)
                                .ApplyOperation(TrySave)
                            : Observable.Return(file))
                        .Switch()
                    : Observable.Return(file))
                .Switch();

        private IObservable<FileEntity> TrySave(IObservable<FileEntity> file)
            => file
                .Select(file => (file.FilePath is string targetFilePath)
                    ? Observable.Return((file, targetFilePath))
                    : ReactiveDisposable
                        .Create(() => new SaveFilePromptModel()
                        {
                            Filter          = _filePromptFilter,
                            InitialFilePath = FileEntity.DefaultFilename,
                        })
                        .Do(prompt => _promptRequested.OnNext(prompt))
                        .Select(prompt => prompt.Result
                            .Select(targetFilePath => (file, targetFilePath)))
                        .Switch())
                .Switch()
                .Select(@params => Observable.FromAsync(async cancellationToken =>
                {
                    using var fileStream = System.IO.File.Open(@params.targetFilePath, FileMode.Create);
                    
                    await JsonSerializer.SerializeAsync(
                        utf8Json:           fileStream,
                        value:              @params.file.Database,
                        cancellationToken:  cancellationToken);

                    return @params.file with
                    {
                        FilePath    = @params.targetFilePath,
                        HasChanges  = false
                    };
                }))
                .Switch()
                .ObserveOn(DispatcherScheduler.Current);

        private readonly ReactiveCommand                    _closeFileCommand;
        private readonly Subject<Unit>                      _closeFileCommandExecuted;
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
