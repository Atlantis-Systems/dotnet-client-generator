using System.CommandLine;
using DotnetClientGenerator;

Option<string> inputOption = new("--input", "-i")
{
    Description = "This is the path to the OpenAPI specification file or URL",
    Required = true
};

Option<string> outputOption = new("--output", "-o")
{
    Description = "Output file path for the generated C# client",
    Required = true
};

Option<string> classNameOption = new("--class-name", "-c")
{
    Description = "Name of the generated client class",
    DefaultValueFactory = _ => "ApiClient"
};

Option<string> namespaceOption = new("--namespace", "-n")
{
    Description = "Namespace for the generated client",
    DefaultValueFactory = _ => "GeneratedClient"
};

Option<bool> watchOption = new("--watch", "-w")
{
    Description = "Watch the input file for changes and regenerate automatically"
};

RootCommand rootCommand = new("A tool for generating C# API clients from OpenAPI specifications");
rootCommand.Options.Add(inputOption);
rootCommand.Options.Add(outputOption);
rootCommand.Options.Add(classNameOption);
rootCommand.Options.Add(namespaceOption);
rootCommand.Options.Add(watchOption);

rootCommand.SetAction(async (parseResult, _) =>
{
    var input = parseResult.GetValue(inputOption)!;
    var output = parseResult.GetValue(outputOption)!;
    var className = parseResult.GetValue(classNameOption)!;
    var namespaceName = parseResult.GetValue(namespaceOption)!;
    var watch = parseResult.GetValue(watchOption);
    
    try
    {
        await GenerateClient(input, output, className, namespaceName);

        if (watch)
        {
            Console.WriteLine($"👀 Watching {input} for changes...");
            
            using FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(input)) ?? ".", Path.GetFileName(input));
            watcher.Changed += async (_, _) =>
            {
                Console.WriteLine("🔄 File changed, regenerating...");
                await GenerateClient(input, output, className, namespaceName);
            };
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("⌨️  Press any key to stop watching...");
            Console.ReadKey();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error: {ex.Message}");
        Environment.Exit(1);
    }
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task GenerateClient(string input, string output, string className, string namespaceName)
{
    Console.WriteLine("🚀 Generating C# API client...");
    Console.WriteLine($"📥 Input: {input}");
    Console.WriteLine($"📄 Output: {output}");

    Console.WriteLine("📖 Parsing OpenAPI specification...");
    OpenApiParser parser = new();
    ParsedApiSpec spec = await parser.ParseSpecificationAsync(input);

    Console.WriteLine($"🏗️  Generating code for {spec.Schemas.Count} models and {spec.Endpoints.Count} endpoints...");
    CSharpClientGenerator generator = new();
    
    ClientGeneratorOptions options = new()
    {
        ClassName = className,
        Namespace = namespaceName
    };

    string clientCode = generator.GenerateClient(spec, options);

    string outputDir = Path.GetDirectoryName(output)!;
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    {
        Console.WriteLine($"📁 Creating output directory: {outputDir}");
        Directory.CreateDirectory(outputDir);
    }

    Console.WriteLine("💾 Writing generated code to file...");
    await File.WriteAllTextAsync(output, clientCode);
    
    Console.WriteLine("✅ C# API client generated successfully!");
}
