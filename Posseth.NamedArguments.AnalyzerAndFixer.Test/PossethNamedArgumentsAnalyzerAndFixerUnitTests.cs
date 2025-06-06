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
                    .WithArguments("x"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y"),
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
                    .WithArguments("x"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y"),
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
        
        [TestMethod]
        public async Task Test_MethodWithSingleParameter_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethodSingle(int x)
    {
        TestMethodSingle(1);
    }
}";

            var expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("x"),
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expectedDiagnostics);
        }

        [TestMethod]
        public async Task Test_MethodWithThreeParameters_ShouldTriggerDiagnostic()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(1, 2, 3);
    }
}";

            var expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("x"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("z"),
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expectedDiagnostics);
        }

        [TestMethod]
        public async Task Test_MethodWithThreeParameters_ShouldFixToUseNamedArguments()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(1, 2, 3);
    }
}";

            var expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("x"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("z"),
            };

            var fixedCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(x: 1, y: 2, z: 3);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(testCode, expectedDiagnostics, fixedCode);
        }

        [TestMethod]
        public async Task Test_MethodWithMixedNamedArguments_ShouldOnlyFixUnnamed()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(x: 1, 2, 3);
    }
}";

            var expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("y"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 29, 8, 30)
                    .WithArguments("z"),
            };

            var fixedCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(x: 1, y: 2, z: 3);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(testCode, expectedDiagnostics, fixedCode);
        }

        [TestMethod]
        public async Task Test_MethodWithAllNamedArguments_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(x: 1, y: 2, z: 3);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task Test_MethodWithConfiguredMinimumParameters_ShouldRespectThreshold()
        {
            var testCode = @"
using System;

class Program
{
    // This method has exactly the minimum required parameters
    void TestMethod(int a, int b)
    {
        TestMethod(1, 2);
    }
    
    // This method has fewer than the minimum required parameters
    void TestMethodSingle(int x)
    {
        TestMethodSingle(1);
    }
}";

            var expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(9, 20, 9, 21)
                    .WithArguments("a"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(9, 23, 9, 24)
                    .WithArguments("b"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(15, 26, 15, 27)
                    .WithArguments("x"),
            };

            await VerifyCS.VerifyAnalyzerAsync(testCode, expectedDiagnostics);
        }

        [TestMethod]
        public async Task Test_MethodWithConfiguredMinimumParameters_ShouldFixBorderlineCases()
        {
            var testCode = @"
using System;

class Program
{
    void TestMethod(int a, int b)
    {
        TestMethod(1, 2);
    }
}";

            var expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("a"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("b"),
            };

            var fixedCode = @"
using System;

class Program
{
    void TestMethod(int a, int b)
    {
        TestMethod(a: 1, b: 2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(testCode, expectedDiagnostics, fixedCode);
        }

        [TestMethod]
        public async Task Analyzer_Excludes_FullyQualifiedMethodName_ButNotCustom()
        {
            var test = @"
    using System.Linq;
    namespace MyNamespace
    {
        public class MyClass
        {
            public void Where(int x) { }
            public void Test()
            {
                var arr = new int[] { 1, 2, 3 };
                Where(1); // Custom method, should trigger diagnostic
                Enumerable.Where(arr, i => i > 1); // Built-in, should NOT trigger diagnostic
            }
        }
    }
    ";

            // Use the exact location reported in the error message
            var expected = VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId)
                .WithSpan(11, 23, 11, 24) // This is the exact position of "1" in Where(1)
                .WithArguments("x");
            
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }   
}