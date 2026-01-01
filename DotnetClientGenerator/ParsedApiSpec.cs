using Microsoft.OpenApi.Models;

namespace DotnetClientGenerator;

public class ParsedApiSpec
{
    public OpenApiInfo Info { get; set; } = new();
    public List<ApiEndpoint> Endpoints { get; set; } = new();
    public IDictionary<string, OpenApiSchema> Schemas { get; set; } = new Dictionary<string, OpenApiSchema>();
}
