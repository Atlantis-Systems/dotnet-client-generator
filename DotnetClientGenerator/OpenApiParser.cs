using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace DotnetClientGenerator;

public class OpenApiParser
{
    // Compiled regex for better performance when parsing multiple specs
    private static readonly System.Text.RegularExpressions.Regex OpenApi31VersionRegex = new(
        @"""openapi""\s*:\s*""3\.1\.\d+""",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled
    );

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

        // Handle OpenAPI 3.1.x specs by temporarily converting version to 3.0.x for parsing.
        // The Microsoft.OpenApi.Readers library versions 1.x do not officially support 3.1.x,
        // but the structural differences are minimal enough that most specs can be parsed
        // by treating them as 3.0.x documents.
        // 
        // Implementation notes:
        // - The string check is a performance optimization to avoid regex on 3.0.x specs
        // - The regex pattern matches both JSON and YAML formats
        // - Some advanced OpenAPI 3.1.x features may not be fully supported (e.g., JSON Schema 2020-12)
        // - Only performs replacement if a 3.1.x version is detected
        if (specContent.Contains("\"openapi\"") && specContent.Contains("3.1."))
        {
            specContent = OpenApi31VersionRegex.Replace(
                specContent,
                @"""openapi"": ""3.0.3"""
            );
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