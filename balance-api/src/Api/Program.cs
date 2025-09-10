using Api.Common.ExtensionMethods.v1;
using Api.Services;
using Infrastructure.Database;

namespace Api
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.AddTelemetry();
            builder.Services.AddGrpcOptions();
            builder.Services.AddSwaggerDocumentation();
            builder.Services.AddDatabaseModule(builder.Configuration);
            
            var app = builder.Build();

            app.MapGrpcService<BalanceService>();
            app.MapGrpcReflectionService();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internal Balance API V1"); });
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production for scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.Run();
        }
    }
}