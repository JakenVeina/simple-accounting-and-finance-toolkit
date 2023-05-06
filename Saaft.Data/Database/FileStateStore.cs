namespace Saaft.Data.Database
{
    public sealed class FileStateStore
        : StateStore<FileStateEntity, FileStateEvent>
    {
        public FileStateStore()
            : base(FileStateEntity.Default)
        { }
    }
}
