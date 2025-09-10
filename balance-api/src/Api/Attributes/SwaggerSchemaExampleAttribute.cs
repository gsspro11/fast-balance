using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Attributes;

/// <summary>
/// 
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Parameter |
    AttributeTargets.Property |
    AttributeTargets.Enum,
    AllowMultiple = false)]
public abstract class SwaggerSchemaExampleAttribute : Attribute
{
    public string Example { get; set; }

    protected SwaggerSchemaExampleAttribute(string example)
    {
        switch (example)
        {
            case "UUID":
                Example = Guid.NewGuid().ToString();
                break;
            case "DateOnly":
            {
                var tomorrow = DateTime.Now.AddDays(1);
                Example = tomorrow.ToString("yyyy-MM-dd");
                break;
            }
            default:
                Example = example;
                break;
        }
    }
}

public abstract class SwaggerSchemaExampleFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo != null)
        {
            var schemaAttribute = context.MemberInfo.GetCustomAttributes<SwaggerSchemaExampleAttribute>()
                .FirstOrDefault();
            if (schemaAttribute != null)
                ApplySchemaAttribute(schema, schemaAttribute);
        }
    }

    private static void ApplySchemaAttribute(OpenApiSchema schema, SwaggerSchemaExampleAttribute schemaAttribute)
    {
        if (schemaAttribute.Example != null)
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiString(schemaAttribute.Example);
        }
    }
}