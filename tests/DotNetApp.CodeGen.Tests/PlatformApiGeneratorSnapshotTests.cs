using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DotNetApp.CodeGen.Tests;

public class PlatformApiGeneratorSnapshotTests
{
    private static (Compilation compilation, GeneratorDriver driver, GeneratorDriverRunResult result) Run(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            new[] { syntaxTree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new DotNetApp.CodeGen.ApiClientGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult();
        return (compilation, driver, result);
    }

    [Fact]
    public void Generates_Get_With_Retry_And_Query()
    {
    var source = @"using System.Threading; using System.Threading.Tasks; using DotNetApp.CodeGen; namespace Demo { [ApiContract(""api/sample"")] public interface ISample { [Get(""item""), Retry(2,50)] Task<string?> GetItemAsync(string id, int page, CancellationToken ct = default); } }";
        var (_, _, result) = Run(source);
        var generated = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("SampleClient"))?.GetText()?.ToString();
        generated.Should().NotBeNull();
        generated!.Should().Contain("_attempts = 2");
        generated.Should().Contain("BuildQuery");
        generated.Should().Contain("GetFromJsonAsync");
    }

    [Fact]
    public void Generates_Post_With_Body()
    {
        var source = @"using System.Threading; using System.Threading.Tasks; using DotNetApp.CodeGen; namespace Demo { public record Thing(string Name); [ApiContract(""api/things"")] public interface IThings { [Post(""create"")] Task<Thing?> CreateAsync([Body] Thing t, CancellationToken ct=default); } }";
        var (_, _, result) = Run(source);
        var generated = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("ThingsClient"))?.GetText()?.ToString();
        generated.Should().NotBeNull();
        generated!.Should().Contain("PostAsJsonAsync");
        generated.Should().Contain("ReadFromJsonAsync<Thing>");
    }

    [Fact]
    public void Generates_Put_With_Route_Param()
    {
        var source = @"using System.Threading; using System.Threading.Tasks; using DotNetApp.CodeGen; namespace Demo { public record Thing(string Name); [ApiContract(""api/things"")] public interface IThings { [Put(""update/{id}"")] Task<Thing?> UpdateAsync(string id, [Body] Thing t, CancellationToken ct=default); } }";
        var (_, _, result) = Run(source);
        var generated = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("ThingsClient"))?.GetText()?.ToString();
        generated.Should().NotBeNull();
        generated!.Should().Contain("PutAsJsonAsync");
        generated.Should().Contain("update/{Uri.EscapeDataString(id.ToString()!)}".Replace("{","{")); // ensure interpolation is present
    }
}
