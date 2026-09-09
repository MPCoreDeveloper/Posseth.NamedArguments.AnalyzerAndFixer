using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    /// <summary>
    /// Runs the verification built into the Microsoft.CodeAnalysis.Testing framework and makes
    /// the result visible to tools that look for explicit assertions (for example SonarQube's
    /// S2699 rule). The framework's <see cref="AnalyzerTest{TVerifier}.RunAsync(CancellationToken)"/>
    /// throws whenever the expected diagnostics, code fixes or compilation results do not match,
    /// so invoking it really is the test's assertion. Giving the wrapper an <c>Assert*</c> name
    /// and an <see cref="AssertionMethodAttribute"/> keeps that intent legible to both readers
    /// and static analysis.
    /// </summary>
    internal static class AnalyzerTestAssertions
    {
        [AssertionMethod]
        public static Task AssertTestPassesAsync<TVerifier>(this AnalyzerTest<TVerifier> test, CancellationToken cancellationToken)
            where TVerifier : IVerifier, new()
            => test.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a helper method as a custom assertion method. SonarQube's S2699 rule deliberately
    /// recognizes any attribute with this exact name, so tests that delegate their verification
    /// to an <c>[AssertionMethod]</c> helper are not reported as missing an assertion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class AssertionMethodAttribute : Attribute
    {
    }
}