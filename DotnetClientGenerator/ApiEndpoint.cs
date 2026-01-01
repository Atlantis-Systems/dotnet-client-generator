using Microsoft.OpenApi;

namespace DotnetClientGenerator;

public class ApiEndpoint
{
    public string Path { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string? OperationId { get; init; }
    public OpenApiRequestBody? RequestBody { get; init; }
    public OpenApiResponses Responses { get; init; } = new();
    public List<IOpenApiParameter> Parameters { get; init; } = [];
}