
//Michel Posseth 2025-05-17
//Multiple code fixes for the same diagnostic ID 
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Posseth.NamedArguments.AnalyzerAndFixer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamedArgumentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PNA1000"; // Posseth NamedArguments analyzer
        private static readonly LocalizableString Title = "Use named arguments";
        private static readonly LocalizableString MessageFormat = "Argument '{0}' should be named";
        private static readonly LocalizableString Description = "All arguments should be named";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        [RuleConfigurationOption("OnlyForRecords",
            "true: Only analyze method calls on record types; false: Analyze all method calls",
            "false")]
        public bool OnlyForRecords { get; set; } = false;

        [RuleConfigurationOption("ExcludedMethodNames",
            "Comma-separated list of method names to exclude from analysis",
            "")]
        public string ExcludedMethodNames { get; set; } = "";

        // Common methods to exclude by default (in addition to user-specified ones)
        private readonly HashSet<string> DefaultExcludedMethods = new HashSet<string>
        {
            "Where", "Select", "FirstOrDefault", "First", "Any",
            "OrderBy", "OrderByDescending", "GroupBy", "ToList", "ToArray",
            "Contains", "ElementAt", "ElementAtOrDefault", "IsNullOrEmpty", "IsNullOrWhiteSpace"
        };

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for invocation expressions instead of arguments directly
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            
            if (methodSymbol == null)
                return;

            // Check if the method is excluded
            if (IsMethodExcluded(methodSymbol.Name))
                return;

            // If OnlyForRecords is enabled, check if the containing type is a record
            if (OnlyForRecords)
            {
                var containingType = GetContainingType(invocation, context.SemanticModel);
                bool isRecord = containingType != null && IsRecord(containingType);

                if (!isRecord)
                    return;
            }

            // Analyze the arguments
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                if (arg.NameColon == null)
                {
                    // Get parameter for this argument to include in diagnostic message
                    string paramName = GetParameterName(arg, invocation, methodSymbol, context.SemanticModel);
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        var diagnostic = Diagnostic.Create(Rule, arg.GetLocation(), paramName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static  string GetParameterName(ArgumentSyntax arg, InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol, SemanticModel semanticModel)
        {
            int argIndex = invocation.ArgumentList.Arguments.IndexOf(arg);
            if (argIndex < methodSymbol.Parameters.Length)
            {
                return methodSymbol.Parameters[argIndex].Name;
            }
            return arg.Expression.ToString();
        }

        private static  INamedTypeSymbol GetContainingType(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
                return typeInfo.Type as INamedTypeSymbol;
            }
            return null;
        }

        private bool IsMethodExcluded(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return false;
                
            if (DefaultExcludedMethods.Contains(methodName))
                return true;
                
            if (string.IsNullOrEmpty(ExcludedMethodNames))
                return false;

            var excludedMethods = ExcludedMethodNames.Split(',').Select(m => m.Trim());
            return excludedMethods.Contains(methodName);
        }

        private static  bool IsRecord(INamedTypeSymbol type)
        {
            if (type == null)
                return false;
                
            // Method 1: Check for EqualityContract property (most reliable for .NET Standard 2.0)
            foreach (var member in type.GetMembers())
            {
                if (member.Name == "EqualityContract" && member is IPropertySymbol)
                {
                    return member.GetAttributes().Any(attr => 
                        attr.AttributeClass?.Name == "CompilerGeneratedAttribute" ||
                        (attr.AttributeClass?.ContainingNamespace?.Name == "CompilerServices" &&
                         attr.AttributeClass?.ContainingNamespace?.ContainingNamespace?.Name == "Runtime"));
                }
            }
            
            // Method 2: Check for other record characteristics
            bool hasClone = type.GetMembers().Any(m => m.Name == "<Clone>$" && m is IMethodSymbol);
            bool hasPrintMembers = type.GetMembers().Any(m => m.Name == "PrintMembers" && m is IMethodSymbol);
            bool hasDeconstruct = type.GetMembers().Any(m => m.Name == "Deconstruct" && m is IMethodSymbol);
            
            return hasClone || hasPrintMembers || hasDeconstruct;
        }
    }
}
