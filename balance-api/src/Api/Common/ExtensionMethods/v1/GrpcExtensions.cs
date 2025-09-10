using Api.Common.Interceptors.v1;
using Google.Protobuf.Reflection;
using Google.Rpc;
using Microsoft.AspNetCore.Grpc.JsonTranscoding;

namespace Api.Common.ExtensionMethods.v1;

public static class GrpcExtensions
{
    public static void AddGrpcOptions(this IServiceCollection services)
    {
        services
            .AddGrpc(options =>
            {
                options.Interceptors.Add<ExceptionInterceptor>();
                options.EnableDetailedErrors = true;
            }).AddJsonTranscoding(options =>
            {
                // Register all necessary Protobuf types
                options.TypeRegistry = TypeRegistry.FromMessages(
                    Status.Descriptor, // google.rpc.Status
                    ErrorInfo.Descriptor, // google.rpc.ErrorInfo
                    BadRequest.Descriptor // google.rpc.BadRequest
                );
                options.JsonSettings = new GrpcJsonSettings
                {
                    WriteIndented = true
                };
            });
    }
}