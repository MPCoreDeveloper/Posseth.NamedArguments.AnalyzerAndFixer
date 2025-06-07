using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyCS = Posseth.NamedArguments.AnalyzerAndFixer.Test.CSharpCodeFixVerifier<
    Posseth.NamedArguments.AnalyzerAndFixer.NamedArgumentsAnalyzer,
    Posseth.NamedArguments.AnalyzerAndFixer.NamedArgumentsCodeFixProvider>;
namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    [TestClass]
    public class RecordAnalyzerTests
    {
       

        [TestMethod]
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
            var test = new VerifyCS.Test
            {
                TestCode = testCode,
            };
            test.TestState.Sources.Add(isExternalInitStub);

            // Verwacht diagnostics
            test.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithSpan(10, 33, 10, 39).WithArguments("Name"));
            test.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithSpan(10, 41, 10, 43).WithArguments("Age"));

            await test.RunAsync();
        }
    }
}