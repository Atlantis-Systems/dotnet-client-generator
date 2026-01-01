using Microsoft.OpenApi.Models;

namespace DotnetClientGenerator;

public class ApiEndpoint
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? OperationId { get; set; }
    public OpenApiRequestBody? RequestBody { get; set; }
    public OpenApiResponses Responses { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<OpenApiParameter> Parameters { get; set; } = new();
}