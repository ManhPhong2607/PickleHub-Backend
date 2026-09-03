namespace PickleHub.Common.Exceptions
{
    // Exception trung lập, đại diện cho việc thao tác này đã được thực hiện trước đó
    // (idempotency backstop qua unique constraint ở DB). Infrastructure chịu trách

    public class DuplicateOperationException : Exception
    {
        public DuplicateOperationException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }
}