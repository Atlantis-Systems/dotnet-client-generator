using System.CommandLine;
using DotnetClientGenerator;

var inputOption = new Option<string>(
    "--input",
    "This is the path to the OpenAPI specification file or URL")
{
    IsRequired = true
};
inputOption.AddAlias("-i");

var outputOption = new Option<string>(
    "--output", 
    "Output file path for the generated C# client")
{
    IsRequired = true
};
outputOption.AddAlias("-o");

var classNameOption = new Option<string>(
    "--class-name",
    () => "ApiClient",
    "Name of the generated client class");
classNameOption.AddAlias("-c");

var namespaceOption = new Option<string>(
    "--namespace",
    () => "GeneratedClient",
    "Namespace for the generated client");
namespaceOption.AddAlias("-n");

var watchOption = new Option<bool>(
    "--watch",
    "Watch the input file for changes and regenerate automatically");
watchOption.AddAlias("-w");

var rootCommand = new RootCommand("A tool for generating C# API clients from OpenAPI specifications")
{
    inputOption,
    outputOption,
    classNameOption,
    namespaceOption,
    watchOption
};

rootCommand.SetHandler(async (input, output, className, namespaceName, watch) =>
{
    try
    {
        await GenerateClient(input, output, className, namespaceName);

        if (watch)
        {
            Console.WriteLine($"👀 Watching {input} for changes...");
            
            using var watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(input)) ?? ".", Path.GetFileName(input));
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
}, inputOption, outputOption, classNameOption, namespaceOption, watchOption);

return await rootCommand.InvokeAsync(args);

static async Task GenerateClient(string input, string output, string className, string namespaceName)
{
    Console.WriteLine("🚀 Generating C# API client...");
    Console.WriteLine($"📥 Input: {input}");
    Console.WriteLine($"📄 Output: {output}");

    Console.WriteLine("📖 Parsing OpenAPI specification...");
    var parser = new OpenApiParser();
    var spec = parser.ParseSpecification(input);

    Console.WriteLine($"🏗️  Generating code for {spec.Schemas.Count} models and {spec.Endpoints.Count} endpoints...");
    var generator = new CSharpClientGenerator();
    var options = new ClientGeneratorOptions
    {
        ClassName = className,
        Namespace = namespaceName
    };

    var clientCode = generator.GenerateClient(spec, options);

    var outputDir = Path.GetDirectoryName(output);
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
    {
        Console.WriteLine($"📁 Creating output directory: {outputDir}");
        Directory.CreateDirectory(outputDir);
    }

    Console.WriteLine("💾 Writing generated code to file...");
    await File.WriteAllTextAsync(output, clientCode);
    
    Console.WriteLine("✅ C# API client generated successfully!");
}
