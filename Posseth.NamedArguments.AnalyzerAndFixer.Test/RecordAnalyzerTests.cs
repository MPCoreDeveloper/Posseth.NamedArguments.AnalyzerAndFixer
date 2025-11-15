using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
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

            await test.RunAsync();
        }
    }
}