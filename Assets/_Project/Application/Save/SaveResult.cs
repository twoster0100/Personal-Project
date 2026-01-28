namespace MyGame.Application.Save
{
    public enum SaveLoadStatus
    {
        Success,
        NotFound,
        Corrupt,
        VersionTooNew,
        TypeMismatch,
        MigrationMissing,
        IOError
    }

    public readonly struct SaveOpResult
    {
        public readonly bool Success;
        public readonly SaveLoadStatus Status;
        public readonly string Message;

        public SaveOpResult(bool success, SaveLoadStatus status, string message)
        {
            Success = success;
            Status = status;
            Message = message;
        }

        public static SaveOpResult Ok() => new SaveOpResult(true, SaveLoadStatus.Success, null);
        public static SaveOpResult Fail(SaveLoadStatus status, string message) => new SaveOpResult(false, status, message);
    }

    public readonly struct SaveLoadResult<T>
    {
        public readonly SaveLoadStatus Status;
        public readonly T Data;
        public readonly string Message;

        public bool Success => Status == SaveLoadStatus.Success;

        public SaveLoadResult(SaveLoadStatus status, T data, string message)
        {
            Status = status;
            Data = data;
            Message = message;
        }

        public static SaveLoadResult<T> Ok(T data) => new SaveLoadResult<T>(SaveLoadStatus.Success, data, null);
        public static SaveLoadResult<T> Fail(SaveLoadStatus status, string message) => new SaveLoadResult<T>(status, default, message);
    }
}
