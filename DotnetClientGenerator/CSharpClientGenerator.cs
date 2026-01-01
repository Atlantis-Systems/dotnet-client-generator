using Microsoft.OpenApi.Models;
using System.Text;

namespace DotnetClientGenerator;

public class CSharpClientGenerator
{
    public string GenerateClient(ParsedApiSpec spec, ClientGeneratorOptions options)
    {
        var sb = new StringBuilder();
        var className = options.ClassName ?? "ApiClient";
        var namespaceName = options.Namespace ?? "GeneratedClient";

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
        var methodName = GenerateMethodName(endpoint);
        var httpMethod = endpoint.Method.ToUpper() switch
        {
            "GET" => "HttpMethod.Get",
            "POST" => "HttpMethod.Post",
            "PUT" => "HttpMethod.Put",
            "DELETE" => "HttpMethod.Delete",
            "PATCH" => "HttpMethod.Patch",
            _ => "HttpMethod.Get"
        };

        var hasRequestBody = endpoint.RequestBody?.Content?.Any() == true;
        var hasResponse = endpoint.Responses.Any(r => r.Key.StartsWith("2"));

        var parameters = new List<string>();
        var pathParameters = new List<string>();

        foreach (var parameter in endpoint.Parameters)
        {
            if (parameter.In == ParameterLocation.Path)
            {
                pathParameters.Add(parameter.Name);
                parameters.Add($"{GetCSharpType(parameter.Schema)} {ToCamelCase(parameter.Name)}");
            }
            else if (parameter.In == ParameterLocation.Query)
            {
                parameters.Add($"{GetCSharpType(parameter.Schema)}? {ToCamelCase(parameter.Name)} = null");
            }
        }

        if (hasRequestBody)
        {
            var requestBodyType = GetRequestBodyType(endpoint.RequestBody);
            parameters.Add($"{requestBodyType} body");
        }

        var parametersString = string.Join(", ", parameters);
        var returnType = GetResponseType(endpoint.Responses);

        sb.AppendLine($"    public async {returnType} {methodName}({parametersString})");
        sb.AppendLine("    {");

        var path = endpoint.Path;
        foreach (var pathParam in pathParameters)
        {
            path = path.Replace($"{{{pathParam}}}", $"{{{ToCamelCase(pathParam)}}}");
        }

        var queryParams = endpoint.Parameters.Where(p => p.In == ParameterLocation.Query).ToList();
        if (queryParams.Any())
        {
            sb.AppendLine("        var queryParams = new List<string>();");
            foreach (var param in queryParams)
            {
                var paramName = ToCamelCase(param.Name);
                sb.AppendLine($"        if ({paramName} != null)");
                sb.AppendLine($"            queryParams.Add($\"{param.Name}={{{paramName}}}\");");
            }
            sb.AppendLine($"        var path = \"{path}\" + (queryParams.Any() ? \"?\" + string.Join(\"&\", queryParams) : \"\");");
        }
        else
        {
            sb.AppendLine($"        var path = $\"{path}\";");
        }

        var responseModelType = GetResponseModelType(endpoint.Responses);
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

    private string GetCSharpType(OpenApiSchema? schema)
    {
        if (schema == null)
            return "object";

        return schema.Type switch
        {
            "string" => "string",
            "integer" => "int",
            "number" => "decimal",
            "boolean" => "bool",
            "array" => "object[]",
            _ => "object"
        };
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

    private void GenerateModelClasses(StringBuilder sb, IDictionary<string, OpenApiSchema> schemas)
    {
        foreach (var schema in schemas)
        {
            GenerateModelClass(sb, schema.Key, schema.Value);
            sb.AppendLine();
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

    private string GetCSharpTypeFromSchema(OpenApiSchema schema)
    {
        if (schema.Reference != null)
        {
            return schema.Reference.Id;
        }

        if (schema.Type == "array" && schema.Items != null)
        {
            var itemType = GetCSharpTypeFromSchema(schema.Items);
            return $"List<{itemType}>";
        }

        return schema.Type switch
        {
            "string" => schema.Format switch
            {
                "date-time" => "DateTime",
                "date" => "DateOnly",
                "time" => "TimeOnly",
                "uuid" => "Guid",
                _ => "string"
            },
            "integer" => schema.Format switch
            {
                "int64" => "long",
                _ => "int"
            },
            "number" => schema.Format switch
            {
                "float" => "float",
                "double" => "double",
                _ => "decimal"
            },
            "boolean" => "bool",
            "object" => "object",
            _ => "object"
        };
    }

    private bool IsNullableType(OpenApiSchema schema)
    {
        return schema.Type switch
        {
            "string" => true,
            "object" => true,
            "array" => true,
            _ => false
        };
    }

    private string GetRequestBodyType(OpenApiRequestBody? requestBody)
    {
        if (requestBody?.Content == null)
            return "object";

        var jsonContent = requestBody.Content.FirstOrDefault(c => c.Key.Contains("json"));
        if (jsonContent.Value?.Schema?.Reference != null)
        {
            return jsonContent.Value.Schema.Reference.Id;
        }

        if (jsonContent.Value?.Schema != null)
        {
            return GetCSharpTypeFromSchema(jsonContent.Value.Schema);
        }

        return "object";
    }

    private string GetResponseType(OpenApiResponses responses)
    {
        var responseModelType = GetResponseModelType(responses);
        if (!string.IsNullOrEmpty(responseModelType))
        {
            return $"Task<{responseModelType}?>";
        }
        return "Task";
    }

    private string GetResponseModelType(OpenApiResponses responses)
    {
        var successResponse = responses.FirstOrDefault(r => r.Key.StartsWith("2"));
        if (successResponse.Value?.Content == null)
            return string.Empty;

        var jsonContent = successResponse.Value.Content.FirstOrDefault(c => c.Key.Contains("json"));
        if (jsonContent.Value?.Schema?.Reference != null)
        {
            return jsonContent.Value.Schema.Reference.Id;
        }

        if (jsonContent.Value?.Schema != null)
        {
            return GetCSharpTypeFromSchema(jsonContent.Value.Schema);
        }

        return string.Empty;
    }
}
