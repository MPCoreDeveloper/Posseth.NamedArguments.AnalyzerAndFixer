using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    public class PossethNamedArgumentsAnalyzeAndFixUnitTest
    {
        [Fact]
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

            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("x", "Program.TestMethod"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y", "Program.TestMethod"));

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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

            var fixedCode = @"
using System;

class Program
{
    void TestMethod(int x, int y)
    {
        TestMethod(x: 1, y: 2);
    }
}";

            var test = new CSharpCodeFixTest<NamedArgumentsAnalyzer, NamedArgumentsCodeFixProvider, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.FixedState.Sources.Add(fixedCode);
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("x", "Program.TestMethod"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y", "Program.TestMethod"));

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }
        
        [Fact]
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
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("x", "Program.TestMethodSingle"),
            };

            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("x", "Program.TestMethod"),
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y", "Program.TestMethod"),
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("z", "Program.TestMethod"),
            };

            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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

            var fixedCode = @"
using System;

class Program
{
    void TestMethod(int x, int y, int z)
    {
        TestMethod(x: 1, y: 2, z: 3);
    }
}";

            var test = new CSharpCodeFixTest<NamedArgumentsAnalyzer, NamedArgumentsCodeFixProvider, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.FixedState.Sources.Add(fixedCode);
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("x", "Program.TestMethod"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("y", "Program.TestMethod"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("z", "Program.TestMethod"));

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 26, 8, 27)
                    .WithArguments("y", "Program.TestMethod"),
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 29, 8, 30)
                    .WithArguments("z", "Program.TestMethod"),
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
            var test = new CSharpCodeFixTest<NamedArgumentsAnalyzer, NamedArgumentsCodeFixProvider, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.FixedState.Sources.Add(fixedCode);
            test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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

            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            // No expected diagnostics

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(9, 20, 9, 21)
                    .WithArguments("a", "Program.TestMethod"),
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(9, 23, 9, 24)
                    .WithArguments("b", "Program.TestMethod"),
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(15, 26, 15, 27)
                    .WithArguments("x", "Program.TestMethodSingle"),
            };

            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
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
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 20, 8, 21)
                    .WithArguments("a", "Program.TestMethod"),
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                    .WithSpan(8, 23, 8, 24)
                    .WithArguments("b", "Program.TestMethod"),
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

            var test = new CSharpCodeFixTest<NamedArgumentsAnalyzer, NamedArgumentsCodeFixProvider, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.FixedState.Sources.Add(fixedCode);
            test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Analyzer_Excludes_FullyQualifiedMethodName_ButNotCustom()
        {
            var testCode = @"
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
            var expected = DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId)
                .WithSpan(11, 23, 11, 24) // This is the exact position of "1" in Where(1)
                .WithArguments("x", "MyNamespace.MyClass.Where");
            
            var test = new CSharpAnalyzerTest<NamedArgumentsAnalyzer, DefaultVerifier>();
            test.TestState.Sources.Add(testCode);
            test.TestState.ExpectedDiagnostics.Add(expected);
            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }
    }   
}
