namespace HealthCheckupReservationReception.Common
{
    public class OperationResult
    {
        protected OperationResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message ?? string.Empty;
        }

        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }

        public static OperationResult Success()
        {
            return new OperationResult(true, string.Empty);
        }

        public static OperationResult Failure(string message)
        {
            return new OperationResult(false, message);
        }
    }

    public sealed class OperationResult<T> : OperationResult
    {
        private OperationResult(bool isSuccess, string message, T value)
            : base(isSuccess, message)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public static OperationResult<T> Success(T value)
        {
            return new OperationResult<T>(true, string.Empty, value);
        }

        public new static OperationResult<T> Failure(string message)
        {
            return new OperationResult<T>(false, message, default(T));
        }
    }
}
