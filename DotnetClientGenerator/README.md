# DotnetClientGenerator

A .NET tool for generating C# API clients from OpenAPI specifications.

## Quick Start

Install the tool globally:

```bash
dotnet tool install --global DotnetClientGenerator
```

Generate a client:

```bash
dotnet-client-generator --input openapi.json --output ApiClient.cs
```

## Features

- ✅ OpenAPI 3.0 specification support
- ✅ Modern C# with HttpClient and async/await
- ✅ JSON serialization with System.Text.Json
- ✅ Customizable class names and namespaces
- ✅ Watch mode for automatic regeneration
- ✅ Support for URLs and local files
- ✅ Query parameters and path parameters
- ✅ Request body handling

## Usage Examples

### Basic Generation
```bash
dotnet-client-generator -i openapi.json -o MyClient.cs
```

### Custom Class and Namespace
```bash
dotnet-client-generator \
  --input https://petstore.swagger.io/v2/swagger.json \
  --output PetStoreClient.cs \
  --class-name PetStoreClient \
  --namespace PetStore.Api
```

### Watch Mode
```bash
dotnet-client-generator -i openapi.json -o ApiClient.cs --watch
```

## Generated Client Usage

```csharp
// Dependency injection setup
services.AddHttpClient<ApiClient>();

// Manual setup
var httpClient = new HttpClient();
var client = new ApiClient(httpClient, "https://api.example.com");

// Use the client
var result = await client.GetUsersAsync();
await client.CreateUserAsync(userData);
```

## Command Line Options

- `--input, -i`: OpenAPI spec file path or URL (required)
- `--output, -o`: Output C# file path (required) 
- `--class-name, -c`: Generated class name (default: "ApiClient")
- `--namespace, -n`: Generated namespace (default: "GeneratedClient")
- `--watch, -w`: Watch for changes and regenerate

## Requirements

- .NET 9.0 or later
- OpenAPI 3.0 specification

## More Information

For detailed documentation, examples, and contribution guidelines, visit the [main repository](https://github.com/your-username/dotnet-client-generator).