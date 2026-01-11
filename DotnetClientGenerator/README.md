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
- ✅ Type-safe response handling with pattern matching
- ✅ Optional interface generation for dependency injection

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

### Generate with Interface
```bash
dotnet-client-generator -i openapi.json -o ApiClient.cs --generate-interface
```

## Generated Client Usage

```csharp
// Dependency injection setup
services.AddHttpClient<IApiClient, ApiClient>();

// Manual setup
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
var client = new ApiClient(httpClient);

// Use the client with pattern matching
var response = await client.GetUser(userId);

switch (response)
{
    case GetUserResponse.Ok ok:
        Console.WriteLine($"Found user: {ok.Value.Name}");
        break;
    case GetUserResponse.NotFound:
        Console.WriteLine("User not found");
        break;
    case GetUserResponse.BadRequest:
        Console.WriteLine("Invalid request");
        break;
    case GetUserResponse.Unexpected unexpected:
        Console.WriteLine($"Unexpected status: {unexpected.StatusCode}");
        break;
}

// Or use switch expressions
var message = response switch
{
    GetUserResponse.Ok { Value: var user } => $"Hello, {user.Name}!",
    GetUserResponse.NotFound => "User not found",
    GetUserResponse.Unauthorized => "Please log in",
    GetUserResponse.Unexpected { StatusCode: var code } => $"Error: {code}",
    _ => "Unknown response"
};
```

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--input` | `-i` | OpenAPI spec file path or URL | (required) |
| `--output` | `-o` | Output C# file path | (required) |
| `--class-name` | `-c` | Generated class name | `ApiClient` |
| `--namespace` | `-n` | Generated namespace | `GeneratedClient` |
| `--generate-interface` | | Generate interface for the client | `false` |
| `--watch` | `-w` | Watch for changes and regenerate | `false` |

## Requirements

- .NET 10.0 or later
- OpenAPI 3.0 specification

## More Information

For detailed documentation, examples, and contribution guidelines, visit the [main repository](https://github.com/your-username/dotnet-client-generator).