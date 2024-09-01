
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
namespace Posseth.NamedArguments.AnalyzerAndFixer
{
 [DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamedArgumentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NamedArguments";
        private static readonly LocalizableString Title = "Use named arguments";
        private static readonly LocalizableString MessageFormat = "Argument '{0}' should be named";
        private static readonly LocalizableString Description = "All arguments should be named";
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.Argument);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var argumentSyntax = (ArgumentSyntax)context.Node;

            // Check if the argument is named
            if (argumentSyntax.NameColon == null)
            {
                var diagnostic = Diagnostic.Create(Rule, argumentSyntax.GetLocation(), argumentSyntax.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
