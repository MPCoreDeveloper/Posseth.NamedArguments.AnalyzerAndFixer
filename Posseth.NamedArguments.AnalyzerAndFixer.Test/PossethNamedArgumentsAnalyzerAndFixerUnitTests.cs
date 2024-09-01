using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Posseth.NamedArguments.AnalyzerAndFixer.Test.CSharpCodeFixVerifier<
    Posseth.NamedArguments.AnalyzerAndFixer.NamedArgumentsAnalyzer,
    Posseth.NamedArguments.AnalyzerAndFixer.NamedArgumentsCodeFixProvider>;
//using VerifyCS = Posseth.NamedArguments.AnalyzeAndFix.Test.CSharpCodeFixVerifier<
//    Posseth.NamedArguments.AnalyzeAndFix.PossethNamedArgumentsAnalyzeAndFixAnalyzer,
//    Posseth.NamedArguments.AnalyzeAndFix.PossethNamedArgumentsAnalyzeAndFixCodeFixProvider>;
namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    [TestClass]
    public class PossethNamedArgumentsAnalyzeAndFixUnitTest
    {
        
        [TestMethod]
        public async Task Test_MethodWithoutNamedArguments_ShouldTriggerDiagnostic()
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

            var expectedDiagnostics = new[]
            {
        VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
            .WithSpan(8, 20, 8, 21)
            .WithArguments("1"),
        VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
            .WithSpan(8, 23, 8, 24)
            .WithArguments("2"),
    };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expectedDiagnostics);
        }

        [TestMethod]
        public async Task Test_MethodWithoutNamedArguments_ShouldFixToUseNamedArguments()
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

            var expectedDiagnostics = new[]
            {
        VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
            .WithSpan(8, 20, 8, 21)
            .WithArguments("1"),
        VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
            .WithSpan(8, 23, 8, 24)
            .WithArguments("2"),
    };

            var fixedCode = @"
using System;

class Program
{
    void TestMethod(int x, int y)
    {
        TestMethod(x: 1, y: 2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(testCode, expectedDiagnostics, fixedCode);
        }



        
    }
}
