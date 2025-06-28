using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace DotnetClientGenerator;

public class OpenApiParser
{
    public ParsedApiSpec ParseSpecification(string specPath)
    {
        string specContent;
        if (Uri.TryCreate(specPath, UriKind.Absolute, out var uri))
        {
            using var httpClient = new HttpClient();
            specContent = httpClient.GetStringAsync(uri).Result;
        }
        else
        {
            specContent = File.ReadAllText(specPath);
        }

        var reader = new OpenApiStringReader();
        var document = reader.Read(specContent, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            throw new InvalidOperationException($"OpenAPI parsing errors: {string.Join(", ", diagnostic.Errors.Select(e => e.Message))}");
        }

        var endpoints = new List<ApiEndpoint>();

        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var endpoint = new ApiEndpoint
                {
                    Path = path.Key,
                    Method = operation.Key.ToString().ToUpper(),
                    OperationId = operation.Value.OperationId,
                    RequestBody = operation.Value.RequestBody,
                    Responses = operation.Value.Responses,
                    Tags = operation.Value.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                    Parameters = operation.Value.Parameters?.ToList() ?? new List<OpenApiParameter>()
                };
                endpoints.Add(endpoint);
            }
        }

        return new ParsedApiSpec
        {
            Info = document.Info,
            Endpoints = endpoints,
            Schemas = document.Components?.Schemas ?? new Dictionary<string, OpenApiSchema>()
        };
    }
}

public class ParsedApiSpec
{
    public OpenApiInfo Info { get; set; } = new();
    public List<ApiEndpoint> Endpoints { get; set; } = new();
    public IDictionary<string, OpenApiSchema> Schemas { get; set; } = new Dictionary<string, OpenApiSchema>();
}

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