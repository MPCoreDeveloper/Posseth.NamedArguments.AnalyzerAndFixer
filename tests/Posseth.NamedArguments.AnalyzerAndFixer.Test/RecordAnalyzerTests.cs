using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    public class RecordAnalyzerTests
    {
       

        [Fact]
        public async Task Test_RecordUsage_ShouldRequireNamedArguments()
        {
            var testCode = @"
using System;

class Program
{
    public record Person(string Name, int Age);

    void TestMethod()
    {
        var person = new Person(""John"", 30);
    }
}";

            // Voeg de IsExternalInit-stub toe
            var isExternalInitStub = @"
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}";

            // Stel de test in
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.Sources.Add(isExternalInitStub);

            // Verwacht diagnostics
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithSpan(10, 33, 10, 39).WithArguments("Name", "Program.Person..ctor"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithSpan(10, 41, 10, 43).WithArguments("Age", "Program.Person..ctor"));

            await test.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Test_OnlyForRecords_IgnoresNonRecordTypes()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethod(int x, int y)
    {
        TestMethod(1, 2);
    }
}";

            // With OnlyForRecords enabled, method calls on ordinary classes must not be reported.
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
[*.cs]
dotnet_diagnostic.PNA1000.OnlyForRecords = true
dotnet_diagnostic.PNA1000_Info.severity = none
dotnet_diagnostic.PNA1000_DefaultMethods.severity = none
"));

            await test.RunAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Test_OnlyForRecords_AnalyzesRecordTypes()
        {
            var testCode = @"
using System;

class Program
{
    public record Person(string Name, int Age);

    void TestMethod()
    {
        var person = new Person(""John"", 30);
    }
}";

            var isExternalInitStub = @"
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}";

            // With OnlyForRecords enabled, object creation of a record type must still be reported.
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.Sources.Add(isExternalInitStub);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
[*.cs]
dotnet_diagnostic.PNA1000.OnlyForRecords = true
dotnet_diagnostic.PNA1000_Info.severity = none
dotnet_diagnostic.PNA1000_DefaultMethods.severity = none
"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithSpan(10, 33, 10, 39).WithArguments("Name", "Program.Person..ctor"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithSpan(10, 41, 10, 43).WithArguments("Age", "Program.Person..ctor"));

            await test.RunAsync(TestContext.Current.CancellationToken);
        }
    }
}