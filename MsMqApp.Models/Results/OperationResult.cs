namespace MsMqApp.Models.Results;

/// <summary>
/// Represents the result of an operation with success/failure status
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred, if any
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates a successful operation result
    /// </summary>
    public static OperationResult Successful()
    {
        return new OperationResult { Success = true };
    }

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static OperationResult Failure(string errorMessage, Exception? exception = null)
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
/// <typeparam name="T">The type of the return value</typeparam>
public class OperationResult<T> : OperationResult
{
    /// <summary>
    /// Gets or sets the result data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Creates a successful operation result with data
    /// </summary>
    public static OperationResult<T> Successful(T data)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public new static OperationResult<T> Failure(string errorMessage, Exception? exception = null)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}
