using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Extensions.Logging;

namespace Caramel.GRPC.Interceptors;

public sealed class GrpcClientLoggingInterceptor(ILogger<GrpcClientLoggingInterceptor> logger) : Interceptor
{
  private readonly ILogger _logger = logger;

   public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
       TRequest request,
       ClientInterceptorContext<TRequest, TResponse> context,
       AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
   {
     if (_logger.IsEnabled(LogLevel.Information))
     {
       GrpcLogs.LogStartingCall(_logger, context.Host ?? string.Empty, context.Method.Type.ToString(), context.Method.Name);
     }
     try
     {
       var response = continuation(request, context);
       if (_logger.IsEnabled(LogLevel.Information))
       {
         GrpcLogs.LogCallSucceeded(_logger, context.Host ?? string.Empty, context.Method.Type.ToString(), context.Method.Name, response);
       }
       return response;
     }
     catch (Exception ex)
     {
       if (_logger.IsEnabled(LogLevel.Error))
       {
         GrpcLogs.LogCallFailed(_logger, context.Host ?? string.Empty, context.Method.Type.ToString(), context.Method.Name, ex);
       }
       throw;
     }
   }
}
