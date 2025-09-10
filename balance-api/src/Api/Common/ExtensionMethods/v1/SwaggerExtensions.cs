using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Api.Common.ExtensionMethods.v1
{
    public static class SwaggerExtensions
    {
        public static void AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddGrpcReflection();
            services.AddGrpcSwagger();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Balance API",
                    Version = "v1",
                    Description =
                        @"It's very important to note that this is a GRPC API and you should not integrate using REST+JSON. This is only available for easing browser testing and validation"
                });

                var filePath = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, filePath));
                c.IncludeGrpcXmlComments(Path.Combine(AppContext.BaseDirectory, filePath),
                    includeControllerXmlComments: true);
            });
        }
    }
}