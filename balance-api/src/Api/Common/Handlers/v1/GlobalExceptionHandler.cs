using System.Reflection;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Npgsql;
using Status = Google.Rpc.Status;

namespace Api.Common.Handlers.v1
{
    public static class GlobalExceptionHandler
    {
        private const string GenericMessage = "An error occurred";

        public static RpcException Handle<T>(this Exception exception, ILogger<T> logger,
            Guid correlationId) =>
            exception switch
            {
                TaskCanceledException taskCanceledException => HandleTimeoutException(taskCanceledException, logger,
                    correlationId),
                TimeoutException timeoutException => HandleTimeoutException(timeoutException, logger,
                    correlationId),
                NpgsqlException npgsqlException => HandlePostgresException(npgsqlException, logger, correlationId),
                RpcException rpcException => HandleRpcException(rpcException, logger, correlationId),
                _ => HandleDefault(exception, logger, correlationId)
            };

        private static RpcException HandleTimeoutException<T>(Exception exception, ILogger<T> logger,
            Guid correlationId)
        {
            logger.LogError(exception, "A timeout occurred");

            return CreateStatus(StatusCode.Internal.GetHashCode(), "A timeout occurred", exception, correlationId)
                .ToRpcException();
        }

        private static RpcException HandlePostgresException<T>(NpgsqlException exception, ILogger<T> logger,
            Guid correlationId)
        {
            logger.LogError(exception, "An SQL error occurred");

            if (exception.ErrorCode == -2)
            {
                return CreateStatus(StatusCode.DeadlineExceeded.GetHashCode(), "An SQL error occurred", exception,
                        correlationId)
                    .ToRpcException();
            }

            return CreateStatus(StatusCode.Internal.GetHashCode(), "An SQL error occurred", exception, correlationId)
                .ToRpcException();
        }

        private static RpcException HandleRpcException<T>(RpcException exception, ILogger<T> logger, Guid correlationId)
        {
            logger.LogError(exception, GenericMessage);

            return CreateStatus(exception.StatusCode.GetHashCode(), GenericMessage, exception, correlationId)
                .ToRpcException();
        }

        private static RpcException HandleDefault<T>(Exception exception, ILogger<T> logger,
            Guid correlationId)
        {
            logger.LogError(exception, "An error occurred");

            return CreateStatus(StatusCode.Internal.GetHashCode(), "An error occurred", exception, correlationId)
                .ToRpcException();
        }

        private static Status CreateStatus(int code, string message, Exception ex, Guid correlationId)
        {
            var errorInfo = new ErrorInfo
            {
                Domain = Assembly.GetExecutingAssembly().GetName().Name, Reason = ex.Message
            };

            errorInfo.Metadata.Add("CorrelationId", correlationId.ToString());

            return new Status
            {
                Code = code,
                Message = message,
                Details =
                {
                    Any.Pack(errorInfo)
                }
            };
        }
    }
}