//Michel Posseth 2025-05-17 last mod 
//multiple code fixes due to feedback from the community
using System.Linq;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var argumentList = argument.Parent as ArgumentListSyntax;
            if (argumentList == null)
                return document.WithSyntaxRoot(root);

            var invocation = argumentList.Parent;

            // Handle both direct invocations and object creation expressions
            if (invocation == null ||
                !(invocation is InvocationExpressionSyntax ||
                 invocation is ObjectCreationExpressionSyntax))
            {
                return document.WithSyntaxRoot(root);
            }

            // Find the corresponding argument in the current root
            var currentArgument = FindCorrespondingNode(root, argument);
            if (currentArgument == null)
                return document.WithSyntaxRoot(root);

            IMethodSymbol methodSymbol = null;

            if (invocation is InvocationExpressionSyntax invocationExpr)
            {
                methodSymbol = semanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;
            }
            else if (invocation is ObjectCreationExpressionSyntax creationExpr)
            {
                methodSymbol = semanticModel.GetSymbolInfo(creationExpr).Symbol as IMethodSymbol;
            }

            if (methodSymbol == null)
            {
                return document.WithSyntaxRoot(root);
            }

            int parameterIndex = argumentList.Arguments.IndexOf(argument);
            if (parameterIndex >= methodSymbol.Parameters.Length)
            {
                return document.WithSyntaxRoot(root);
            }

            var parameter = methodSymbol.Parameters[parameterIndex];

            if (parameter == null)
            {
                return document.WithSyntaxRoot(root);
            }

            // Preserve the original trivia (whitespace, indentation, etc.)
            SyntaxTriviaList leadingTrivia = currentArgument.GetLeadingTrivia();
            SyntaxTriviaList trailingTrivia = currentArgument.GetTrailingTrivia();

            // Create the name colon with no trivia - trivia will be applied to the entire argument
            var nameColon = SyntaxFactory.NameColon(parameter.Name);

            // Create a new argument node with a NameColon, preserving original trivia
            var namedArgument = SyntaxFactory.Argument(
                nameColon,
                currentArgument.RefOrOutKeyword,
                currentArgument.Expression)
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);

            var newRoot = root.ReplaceNode(currentArgument, namedArgument);

            return document.WithSyntaxRoot(newRoot);
        }

        // Helper method to find a corresponding node in a new syntax tree
        private ArgumentSyntax FindCorrespondingNode(SyntaxNode root, ArgumentSyntax originalNode)
        {
            // Find the node at the same position
            var nodeAtSamePosition = root.FindNode(originalNode.Span);
            if (nodeAtSamePosition is ArgumentSyntax arg)
                return arg;

            // If location-based search failed, try finding by structure
            var parentList = originalNode.Parent as ArgumentListSyntax;
            if (parentList != null)
            {
                int index = parentList.Arguments.IndexOf(originalNode);
                var newParentList = root.DescendantNodes()
                    .OfType<ArgumentListSyntax>()
                    .FirstOrDefault(a => a.Span.Contains(parentList.Span));

                if (newParentList != null && index >= 0 && index < newParentList.Arguments.Count)
                    return newParentList.Arguments[index];
            }

            return null;
        }

    }
       
      
}
