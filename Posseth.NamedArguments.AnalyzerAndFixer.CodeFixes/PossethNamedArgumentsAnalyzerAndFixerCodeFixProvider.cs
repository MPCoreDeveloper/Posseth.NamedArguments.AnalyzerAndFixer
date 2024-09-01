
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Xml.Linq;
namespace Posseth.NamedArguments.AnalyzerAndFixer
{
   [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamedArgumentsCodeFixProvider)), Shared]
public class NamedArgumentsCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Use named argument";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NamedArgumentsAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var argument = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => UseNamedArgumentAsync(context.Document, argument, c),
                    equivalenceKey: Title),
                diagnostic);
        }
        private async Task<Document> UseNamedArgumentAsync(Document document, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var argumentList = argument.Parent as ArgumentListSyntax;
            var invocation = argumentList?.Parent as InvocationExpressionSyntax;

            if (invocation == null)
            {
                // Als de parent geen InvocationExpressionSyntax is, kunnen we de fix niet toepassen
                return document;
            }

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                // Als methodSymbol null is, kunnen we de fix niet toepassen
                return document;
            }

            var parameter = methodSymbol.Parameters[argumentList.Arguments.IndexOf(argument)];

            if (parameter == null)
            {
                // Als parameter null is, kunnen we de fix niet toepassen
                return document;
            }

            // Maak een nieuwe argument node met een NameColon
            var namedArgument = SyntaxFactory.Argument(
                SyntaxFactory.NameColon(parameter.Name), // Hier voeg je de naam van de parameter toe
                argument.RefOrOutKeyword,
                argument.Expression);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(argument, namedArgument);

            return document.WithSyntaxRoot(newRoot);
        }

    }
}
