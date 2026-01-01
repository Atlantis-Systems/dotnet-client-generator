using System.Text;
using Microsoft.OpenApi;

namespace DotnetClientGenerator;

public class CSharpClientGenerator
{
    public string GenerateClient(ParsedApiSpec spec, ClientGeneratorOptions options)
    {
        StringBuilder sb = new();
        string className = options.ClassName ?? "ApiClient";
        string namespaceName = options.Namespace ?? "GeneratedClient";

        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        GenerateModelClasses(sb, spec.Schemas);

        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly HttpClient _httpClient;");
        sb.AppendLine("    private readonly string _baseUrl;");
        sb.AppendLine();
        sb.AppendLine($"    public {className}(HttpClient httpClient, string baseUrl = \"\")");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpClient = httpClient;");
        sb.AppendLine("        _baseUrl = baseUrl;");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var endpoint in spec.Endpoints)
        {
            GenerateEndpointMethod(sb, endpoint);
        }

        sb.AppendLine("    private async Task<T?> SendRequestAsync<T>(string path, HttpMethod method, object? body = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var request = new HttpRequestMessage(method, _baseUrl + path);");
        sb.AppendLine();
        sb.AppendLine("        if (body != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var json = JsonSerializer.Serialize(body);");
        sb.AppendLine("            request.Content = new StringContent(json, Encoding.UTF8, \"application/json\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var response = await _httpClient.SendAsync(request);");
        sb.AppendLine("        response.EnsureSuccessStatusCode();");
        sb.AppendLine();
        sb.AppendLine("        var responseContent = await response.Content.ReadAsStringAsync();");
        sb.AppendLine("        ");
        sb.AppendLine("        if (string.IsNullOrEmpty(responseContent))");
        sb.AppendLine("            return default;");
        sb.AppendLine();
        sb.AppendLine("        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions");
        sb.AppendLine("        {");
        sb.AppendLine("            PropertyNameCaseInsensitive = true");
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    private async Task SendRequestAsync(string path, HttpMethod method, object? body = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var request = new HttpRequestMessage(method, _baseUrl + path);");
        sb.AppendLine();
        sb.AppendLine("        if (body != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var json = JsonSerializer.Serialize(body);");
        sb.AppendLine("            request.Content = new StringContent(json, Encoding.UTF8, \"application/json\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var response = await _httpClient.SendAsync(request);");
        sb.AppendLine("        response.EnsureSuccessStatusCode();");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateEndpointMethod(StringBuilder sb, ApiEndpoint endpoint)
    {
        string methodName = GenerateMethodName(endpoint);
        string httpMethod = endpoint.Method.ToUpper() switch
        {
            "GET" => "HttpMethod.Get",
            "POST" => "HttpMethod.Post",
            "PUT" => "HttpMethod.Put",
            "DELETE" => "HttpMethod.Delete",
            "PATCH" => "HttpMethod.Patch",
            _ => "HttpMethod.Get"
        };

        bool hasRequestBody = endpoint.RequestBody?.Content?.Any() == true;
        bool hasResponse = endpoint.Responses.Any(r => r.Key.StartsWith("2"));

        List<string> parameters = new List<string>();
        List<string> pathParameters = new List<string>();

        foreach (var parameter in endpoint.Parameters)
        {
            string paramName = parameter.Name ?? "param";
            switch (parameter)
            {
                case { In: ParameterLocation.Path }:
                    pathParameters.Add(paramName);
                    parameters.Add($"{GetCSharpTypeFromSchema(parameter.Schema)} {ToCamelCase(paramName)}");
                    break;
                case { In: ParameterLocation.Query }:
                    parameters.Add($"{GetCSharpTypeFromSchema(parameter.Schema)}? {ToCamelCase(paramName)} = null");
                    break;
            }
        }

        if (hasRequestBody)
        {
            string requestBodyType = GetRequestBodyType(endpoint.RequestBody);
            parameters.Add($"{requestBodyType} body");
        }

        string parametersString = string.Join(", ", parameters);
        string returnType = GetResponseType(endpoint.Responses);

        sb.AppendLine($"    public async {returnType} {methodName}({parametersString})");
        sb.AppendLine("    {");

        string path = pathParameters.Aggregate(endpoint.Path, (current, pathParam) => current.Replace($"{{{pathParam}}}", $"{{{ToCamelCase(pathParam)}}}"));

        List<IOpenApiParameter> queryParams = endpoint.Parameters.Where(p => p.In == ParameterLocation.Query).ToList();
        if (queryParams.Any())
        {
            sb.AppendLine("        var queryParams = new List<string>();");
            foreach (var param in queryParams)
            {
                var paramName = ToCamelCase(param.Name ?? "param");
                sb.AppendLine($"        if ({paramName} != null)");
                sb.Append("            queryParams.Add($\"");
                sb.Append(param.Name);
                sb.Append("={");
                sb.Append(paramName);
                sb.AppendLine("}\");");
            }
            sb.AppendLine($"        var path = \"{path}\" + (queryParams.Any() ? \"?\" + string.Join(\"&\", queryParams) : \"\");");
        }
        else
        {
            sb.AppendLine($"        var path = $\"{path}\";");
        }

        string responseModelType = GetResponseModelType(endpoint.Responses);
        if (hasResponse && !string.IsNullOrEmpty(responseModelType))
        {
            if (hasRequestBody)
            {
                sb.AppendLine($"        return await SendRequestAsync<{responseModelType}>(path, {httpMethod}, body);");
            }
            else
            {
                sb.AppendLine($"        return await SendRequestAsync<{responseModelType}>(path, {httpMethod});");
            }
        }
        else
        {
            if (hasRequestBody)
            {
                sb.AppendLine($"        await SendRequestAsync(path, {httpMethod}, body);");
            }
            else
            {
                sb.AppendLine($"        await SendRequestAsync(path, {httpMethod});");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private string GenerateMethodName(ApiEndpoint endpoint)
    {
        if (!string.IsNullOrEmpty(endpoint.OperationId))
        {
            return ToPascalCase(endpoint.OperationId);
        }

        var pathParts = endpoint.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("{"))
            .ToArray();

        var cleanPath = string.Join("", pathParts.Select(ToPascalCase));
        return $"{ToPascalCase(endpoint.Method.ToLower())}{cleanPath}";
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToLowerInvariant(input[0]) + input[1..];
    }

    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    private void GenerateModelClasses(StringBuilder sb, IDictionary<string, OpenApiSchema?> schemas)
    {
        foreach (var schema in schemas)
        {
            if (schema.Value != null)
            {
                GenerateModelClass(sb, schema.Key, schema.Value);
                sb.AppendLine();
            }
        }
    }

    private void GenerateModelClass(StringBuilder sb, string className, OpenApiSchema schema)
    {
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                var propertyName = ToPascalCase(property.Key);
                var propertyType = GetCSharpTypeFromSchema(property.Value);
                var isRequired = schema.Required?.Contains(property.Key) == true;
                
                if (!isRequired && !propertyType.EndsWith("?") && IsNullableType(property.Value))
                {
                    propertyType += "?";
                }

                sb.AppendLine($"    public {propertyType} {propertyName} {{ get; set; }}");
            }
        }

        sb.AppendLine("}");
    }

    private string GetCSharpTypeFromSchema(IOpenApiSchema? schema)
    {
        switch (schema)
        {
            case null:
            {
                return "object";
            }
            case OpenApiSchemaReference schemaRef:
                return schemaRef.Reference.Id ?? "object";
        }

        OpenApiSchema? openApiSchema = schema as OpenApiSchema;
        if (openApiSchema == null)
            return "object";

        if (openApiSchema.Type != JsonSchemaType.Array || openApiSchema.Items == null)
            return openApiSchema.Type switch
            {
                JsonSchemaType.String => openApiSchema.Format switch
                {
                    "date-time" => "DateTime",
                    "date" => "DateOnly",
                    "time" => "TimeOnly",
                    "uuid" => "Guid",
                    _ => "string"
                },
                JsonSchemaType.Integer => openApiSchema.Format switch
                {
                    "int64" => "long",
                    _ => "int"
                },
                JsonSchemaType.Number => openApiSchema.Format switch
                {
                    "float" => "float",
                    "double" => "double",
                    _ => "decimal"
                },
                JsonSchemaType.Boolean => "bool",
                _ => "object"
            };
        string itemType = GetCSharpTypeFromSchema(openApiSchema.Items);
        return $"List<{itemType}>";

    }

    private static bool IsNullableType(IOpenApiSchema? schema)
    {
        if (schema == null)
        {
            return false;
        }

        OpenApiSchema? openApiSchema = schema as OpenApiSchema;
        return openApiSchema == null
            ? schema is OpenApiSchemaReference
            : // References are nullable
            openApiSchema.Type switch
            {
                JsonSchemaType.String => true,
                JsonSchemaType.Object => true,
                JsonSchemaType.Array => true,
                _ => false
            };
    }

    private string GetRequestBodyType(OpenApiRequestBody? requestBody)
    {
        if (requestBody?.Content == null)
            return "object";

        KeyValuePair<string, IOpenApiMediaType> jsonContent = requestBody.Content.FirstOrDefault(c => c.Key.Contains("json"));
        if (jsonContent.Value?.Schema == null) return "object";
        if (jsonContent.Value.Schema is OpenApiSchemaReference schemaRef)
        {
            return schemaRef.Reference.Id ?? "object";
        }

        return GetCSharpTypeFromSchema(jsonContent.Value.Schema);
    }

    private string GetResponseType(OpenApiResponses responses)
    {
        string responseModelType = GetResponseModelType(responses);
        return !string.IsNullOrEmpty(responseModelType) ? $"Task<{responseModelType}?>" : "Task";
    }

    private string GetResponseModelType(OpenApiResponses responses)
    {
        var successResponse = responses.FirstOrDefault(r => r.Key.StartsWith("2"));
        if (successResponse.Value?.Content == null)
            return string.Empty;

        KeyValuePair<string, IOpenApiMediaType> jsonContent = successResponse.Value.Content.FirstOrDefault(c => c.Key.Contains("json"));
        if (jsonContent.Value?.Schema == null) return string.Empty;
        if (jsonContent.Value.Schema is OpenApiSchemaReference schemaRef)
        {
            return schemaRef.Reference.Id ?? string.Empty;
        }

        return GetCSharpTypeFromSchema(jsonContent.Value.Schema);

    }
}
