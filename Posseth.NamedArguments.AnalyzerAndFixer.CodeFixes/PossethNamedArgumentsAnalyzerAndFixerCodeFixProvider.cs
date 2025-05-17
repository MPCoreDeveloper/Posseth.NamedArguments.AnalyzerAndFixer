//Michel Posseth 2025-05-17 last mod 
//multiple code fixes due to feedback from the community
using System.Linq;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
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
            
            // First, process any nested invocations within this argument
            var processedRoot = ProcessNestedInvocations(root, argument, semanticModel);
            
            // Then process the current argument
            var argumentList = argument.Parent as ArgumentListSyntax;
            if (argumentList == null)
                return document.WithSyntaxRoot(processedRoot);
                
            var invocation = argumentList.Parent;
            
            // Handle both direct invocations and object creation expressions
            if (invocation == null || 
                !(invocation is InvocationExpressionSyntax || 
                 invocation is ObjectCreationExpressionSyntax))
            {
                return document.WithSyntaxRoot(processedRoot);
            }

            // Find the corresponding argument in the processed root
            var currentArgument = FindCorrespondingNode(processedRoot, argument);
            if (currentArgument == null)
                return document.WithSyntaxRoot(processedRoot);

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
                return document.WithSyntaxRoot(processedRoot);
            }

            int parameterIndex = argumentList.Arguments.IndexOf(argument);
            if (parameterIndex >= methodSymbol.Parameters.Length)
            {
                return document.WithSyntaxRoot(processedRoot);
            }

            var parameter = methodSymbol.Parameters[parameterIndex];

            if (parameter == null)
            {
                return document.WithSyntaxRoot(processedRoot);
            }

            // Create a new argument node with a NameColon
            var namedArgument = SyntaxFactory.Argument(
                SyntaxFactory.NameColon(parameter.Name),
                currentArgument.RefOrOutKeyword,
                currentArgument.Expression);

            var newRoot = processedRoot.ReplaceNode(currentArgument, namedArgument);

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

        // New method to process nested invocations
        private SyntaxNode ProcessNestedInvocations(SyntaxNode root, ArgumentSyntax argument, SemanticModel semanticModel)
        {
            // Find all nested invocations within this argument
            var nestedInvocations = argument.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .ToList();
                
            if (nestedInvocations.Count == 0)
                return root;
                
            // Process each nested invocation
            return root.ReplaceNodes(
                nestedInvocations,
                (original, _) => ProcessNestedInvocation(original, original, semanticModel));
        }

        private SyntaxNode ProcessNestedInvocation(SyntaxNode original, SyntaxNode rewritten, SemanticModel semanticModel)
        {
            var invocation = rewritten as InvocationExpressionSyntax;
            if (invocation == null)
                return rewritten;
                
            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                return rewritten;
                
            var argList = invocation.ArgumentList;
            var newArgs = new List<ArgumentSyntax>();
            bool changed = false;
            
            for (int i = 0; i < argList.Arguments.Count; i++)
            {
                var arg = argList.Arguments[i];
                if (arg.NameColon == null && i < methodSymbol.Parameters.Length)
                {
                    changed = true;
                    newArgs.Add(SyntaxFactory.Argument(
                        SyntaxFactory.NameColon(methodSymbol.Parameters[i].Name),
                        arg.RefOrOutKeyword,
                        arg.Expression));
                }
                else
                {
                    newArgs.Add(arg);
                }
            }
            
            if (!changed)
                return rewritten;
                
            return invocation.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArgs)));
        }
    }
}
