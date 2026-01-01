# .NET Client Generator

A .NET tool for generating C# API clients from OpenAPI specifications. This tool creates HttpClient-based clients with async/await patterns, JSON serialization, and proper error handling.

## Features

- **OpenAPI 3.0 Support**: Parse JSON OpenAPI specifications from files or URLs
- **Modern C# Code**: Generates clients using HttpClient with async/await patterns
- **Customizable Output**: Configure class names, namespaces, and output paths
- **Watch Mode**: Automatically regenerate clients when OpenAPI specs change
- **CLI Tool**: Easy to use command-line interface with rich options
- **NuGet Package**: Distribute and install as a .NET global tool

## Installation

Install as a global .NET tool:

```bash
dotnet tool install --global DotnetClientGenerator
```

Or install locally in a project:

```bash
dotnet tool install DotnetClientGenerator
```

## Usage

### Basic Usage

Generate a C# client from an OpenAPI specification:

```bash
dotnet-client-generator --input openapi.json --output ApiClient.cs
```

### Advanced Usage

Customize the generated client:

```bash
dotnet-client-generator \
  --input https://petstore.swagger.io/v2/swagger.json \
  --output PetStoreClient.cs \
  --class-name PetStoreClient \
  --namespace PetStore.Api
```

### Watch Mode

Automatically regenerate when the OpenAPI spec changes:

```bash
dotnet-client-generator --input openapi.json --output ApiClient.cs --watch
```

## Command Line Options

| Option | Alias | Description | Required |
|--------|-------|-------------|----------|
| `--input` | `-i` | Path to OpenAPI specification file or URL | Yes |
| `--output` | `-o` | Output file path for generated C# client | Yes |
| `--class-name` | `-c` | Name of the generated client class | No (default: "ApiClient") |
| `--namespace` | `-n` | Namespace for the generated client | No (default: "GeneratedClient") |
| `--watch` | `-w` | Watch input file for changes and regenerate | No |

## Generated Client Features

The generated C# client includes:

- **HttpClient Integration**: Uses dependency injection-friendly HttpClient
- **Async/await Pattern**: All methods return Task or Task<T>
- **JSON Serialization**: Uses System.Text.Json for request/response handling
- **Error Handling**: Throws exceptions for HTTP error responses
- **Type Safety**: Strongly typed method parameters where possible
- **Query Parameters**: Automatic query string building
- **Path Parameters**: Automatic URL path interpolation
- **Request Bodies**: JSON serialization for POST/PUT operations

## Example Generated Client

```csharp
using System.Text.Json;
using System.Text;

namespace PetStore;

public class PetStoreClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public PetStoreClient(HttpClient httpClient, string baseUrl = "")
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<object?> ListPets(int? limit = null)
    {
        var queryParams = new List<string>();
        if (limit != null)
            queryParams.Add($"limit={limit}");
        var path = "/pets" + (queryParams.Any() ? "?" + string.Join("&", queryParams) : "");
        return await SendRequestAsync<object>(path, HttpMethod.Get);
    }

    public async Task<object?> CreatePet(object body)
    {
        var path = $"/pets";
        return await SendRequestAsync<object>(path, HttpMethod.Post, body);
    }

    // ... other generated methods
}
```

## Using the Generated Client

```csharp
// Register with DI container
services.AddHttpClient<PetStoreClient>();

// Or create manually
var httpClient = new HttpClient();
var client = new PetStoreClient(httpClient, "https://petstore.swagger.io/v2");

// Use the client
var pets = await client.ListPets(limit: 10);
await client.CreatePet(new { name = "Fluffy", tag = "cat" });
```

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet run --project DotnetClientGenerator -- --input sample-openapi.json --output TestClient.cs
```

### Packaging

```bash
dotnet pack
```

## Versioning

This project uses [GitVersion](https://gitversion.net/) for semantic versioning. By default, commits to the `main` branch increment the patch version, and commits to the `develop` branch increment the minor version.

### Default Behavior (No Special Syntax Required)

For most commits, you don't need any special syntax. Just write your commit message normally:

```bash
# Normal commit on main branch → increments patch version (1.0.0 → 1.0.1)
git commit -m "Fix JSON serialization issue"

# Normal commit on develop branch → increments minor version (1.0.0 → 1.1.0)
git commit -m "Add new endpoint support"
```

### Overriding Version Increments (Optional)

If you need to override the default increment, you can add one of these optional keywords anywhere in your commit message:

| Keyword | Effect | Example |
|---------|--------|---------|
| `+semver: major` or `+semver: breaking` | Increments major version (e.g., 1.0.0 → 2.0.0) | `Add breaking API change +semver: major` |
| `+semver: minor` or `+semver: feature` | Increments minor version (e.g., 1.0.0 → 1.1.0) | `Add new feature +semver: minor` |
| `+semver: patch` or `+semver: fix` | Increments patch version (e.g., 1.0.0 → 1.0.1) | `Fix bug +semver: patch` |

### Example: When to Use Override Keywords

```bash
# On main branch, force a minor version bump instead of patch
git commit -m "Add support for OpenAPI 3.1 +semver: minor"

# On any branch, force a major version bump for breaking changes
git commit -m "Rename output parameter +semver: breaking"
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License.