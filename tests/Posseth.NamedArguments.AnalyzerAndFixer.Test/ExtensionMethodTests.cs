using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    /// <summary>
    /// Tests for extension method invocations (e.g. EF Core's <c>Include</c>), covering
    /// the fully-qualified method path in the diagnostic message and exclusions by
    /// fully-qualified extension-method names (see issue #1).
    /// </summary>
    public class ExtensionMethodTests
    {
        private const string TestCode = @"
using System.Linq;
using MyExtensions;

namespace MyExtensions
{
    public static class QueryExtensions
    {
        public static IQueryable<T> Include<T>(this IQueryable<T> source, string navigationPropertyPath) => source;
    }
}

class Program
{
    void Test()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        _ = source.Include(""Navigation"");
    }
}
";

        [Fact]
        public async Task ExtensionMethodCall_ReportsFullMethodPathAndParameterName()
        {
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(TestCode);
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(18, 28, 18, 40)
                    .WithArguments("navigationPropertyPath", "MyExtensions.QueryExtensions.Include"));

            await test.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ExtensionMethodCall_IsFixedToNamedArgument()
        {
            const string fixedCode = @"
using System.Linq;
using MyExtensions;

namespace MyExtensions
{
    public static class QueryExtensions
    {
        public static IQueryable<T> Include<T>(this IQueryable<T> source, string navigationPropertyPath) => source;
    }
}

class Program
{
    void Test()
    {
        var source = new[] { 1, 2, 3 }.AsQueryable();
        _ = source.Include(navigationPropertyPath: ""Navigation"");
    }
}
";

            var test = new CSharpCodeFixTest<NamedArgumentsAnalyzer, NamedArgumentsCodeFixProvider, DefaultVerifier>();
            test.TestState.Sources.Add(TestCode);
            test.FixedState.Sources.Add(fixedCode);
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(18, 28, 18, 40)
                    .WithArguments("navigationPropertyPath", "MyExtensions.QueryExtensions.Include"));

            await test.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ExtensionMethodCall_ExcludedByFullyQualifiedName()
        {
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(TestCode);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
[*.cs]
dotnet_diagnostic.PNA1000.ExcludedMethodNames = MyExtensions.QueryExtensions.Include
dotnet_diagnostic.PNA1000_Info.severity = none
dotnet_diagnostic.PNA1000_DefaultMethods.severity = none
"));
            await test.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ExtensionMethodCall_ExcludedByGlobalPrefixedName()
        {
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(TestCode);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
[*.cs]
dotnet_diagnostic.PNA1000.ExcludedMethodNames = global::MyExtensions.QueryExtensions.Include
dotnet_diagnostic.PNA1000_Info.severity = none
dotnet_diagnostic.PNA1000_DefaultMethods.severity = none
"));
            await test.RunAsync(TestContext.Current.CancellationToken);
        }
    }
}
