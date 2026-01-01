using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace DotnetClientGenerator;

public class OpenApiParser
{
    public async Task<ParsedApiSpec> ParseSpecificationAsync(string specPath)
    {
        ReadResult result;

        if (Uri.TryCreate(specPath, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            using var httpClient = new HttpClient();
            using var stream = await httpClient.GetStreamAsync(uri);
            result = await OpenApiDocument.LoadAsync(stream);
        }
        else
        {
            result = await OpenApiDocument.LoadAsync(specPath);
        }

        if (result.Document == null)
        {
            var errors = result.Diagnostic?.Errors?.Select(e => e.Message) ?? ["Unknown error"];
            throw new InvalidOperationException($"OpenAPI parsing errors: {string.Join(", ", errors)}");
        }

        if (result.Diagnostic?.Errors?.Count > 0)
        {
            throw new InvalidOperationException($"OpenAPI parsing errors: {string.Join(", ", result.Diagnostic.Errors.Select(e => e.Message))}");
        }

        var document = result.Document;
        var endpoints = new List<ApiEndpoint>();

        if (document.Paths != null)
        {
            foreach (var path in document.Paths)
            {
                if (path.Value?.Operations == null) continue;
                
                foreach (var operation in path.Value.Operations)
                {
                    var endpoint = new ApiEndpoint
                    {
                        Path = path.Key,
                        Method = operation.Key.ToString().ToUpper(),
                        OperationId = operation.Value.OperationId,
                        RequestBody = operation.Value.RequestBody as OpenApiRequestBody,
                        Responses = operation.Value.Responses ?? new OpenApiResponses(),
                        Tags = operation.Value.Tags?.Select(t => t.Name).ToList() ?? new List<string?>(),
                        Parameters = operation.Value.Parameters?.ToList() ?? new List<IOpenApiParameter>()
                    };
                    endpoints.Add(endpoint);
                }
            }
        }

        return new ParsedApiSpec
        {
            Info = document.Info ?? new OpenApiInfo(),
            Endpoints = endpoints,
            Schemas = document.Components?.Schemas?.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value as OpenApiSchema) 
                ?? new Dictionary<string, OpenApiSchema?>()
        };
    }
}
