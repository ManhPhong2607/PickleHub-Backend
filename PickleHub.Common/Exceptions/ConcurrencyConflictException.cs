namespace PickleHub.Common.Exceptions
{
    // Exception trung lập, không phụ thuộc EF Core — đại diện cho việc dữ liệu bị thay đổi bởi thao tác khác trong lúc đang xử lý (optimistic concurrency conflict).

    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }
}