namespace DotnetClientGenerator;

public class ClientGeneratorOptions
{
    public string? ClassName { get; init; }
    public string? Namespace { get; init; }
    public bool GenerateInterface { get; init; }
}