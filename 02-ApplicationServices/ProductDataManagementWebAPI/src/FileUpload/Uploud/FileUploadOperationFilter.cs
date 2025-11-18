using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace FileUpload.Uploud;
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParam = operation.Parameters.FirstOrDefault(p => p.Name == "file");
        if (fileParam != null)
            operation.Parameters.Remove(fileParam);

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        },
                        Required = new HashSet<string> { "file" }
                    }
                }
            }
        };
    }
}