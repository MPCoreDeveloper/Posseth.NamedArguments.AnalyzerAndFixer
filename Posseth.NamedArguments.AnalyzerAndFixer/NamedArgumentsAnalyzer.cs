//Michel Posseth 2025-05-17
//Multiple code fixes for the same diagnostic ID 
//Michel Posseth 2025-06-03
//Extra options and record fix 
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
        private static readonly LocalizableString MessageFormat = "Argument '{0}' in '{1}' should be named";
        private static readonly LocalizableString Description = "All arguments should be named.";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly DiagnosticDescriptor InfoRule = new DiagnosticDescriptor(
            DiagnosticId + "_Info",
            "NamedArgumentsAnalyzer options",
            "NamedArgumentsAnalyzer options: OnlyForRecords={0}, ExcludedMethodNames={1}, UseDefaultExcludedMethods={2}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Shows the current analyzer options."
        );

        private static readonly DiagnosticDescriptor DefaultMethodsRule = new DiagnosticDescriptor(
            DiagnosticId + "_DefaultMethods",
            "Default excluded methods",
            "Default excluded methods: {0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Shows the list of default excluded methods."
        );

        private bool _infoReported=true;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(Rule, InfoRule, DefaultMethodsRule);

        [RuleConfigurationOption("OnlyForRecords",
            "true: Only analyze method calls on record types; false: Analyze all method calls",
            "false")]
        public bool OnlyForRecords { get; set; } = false;

        [RuleConfigurationOption("ExcludedMethodNames",
            "Comma-separated list of method names or fully qualified names (e.g., System.Linq.Enumerable.Where) to exclude from analysis",
            "")]
        public string ExcludedMethodNames { get; set; } = "";

        [RuleConfigurationOption("UseDefaultExcludedMethods",
            "true: Use the built-in list of default excluded methods; false: Do not use the default excluded methods",
            "true")]
        public bool UseDefaultExcludedMethods { get; set; } = true;

        // Common methods to exclude by default (in addition to user-specified ones)
        private readonly HashSet<string> DefaultExcludedMethods = new HashSet<string>
        {
"System.Char.Equals",
"System.Char.CompareTo",
"System.Char.GetUnicodeCategory",
"System.Char.IsControl",
"System.Char.IsDigit",
"System.Char.IsLetter",
"System.Char.IsLetterOrDigit",
"System.Char.IsLower",
"System.Char.IsNumber",
"System.Char.IsPunctuation",
"System.Char.IsSeparator",
"System.Char.IsSurrogate",
"System.Char.IsSymbol",
"System.Char.IsUpper",
"System.Char.IsWhiteSpace",
"System.Char.ToLower",
"System.Char.ToLowerInvariant",
"System.Char.ToUpper",
"System.Char.ToUpperInvariant",
"System.Char.GetNumericValue",
"System.String.Contains",
"System.String.EndsWith",
"System.String.StartsWith",
"System.String.Equals",
"System.String.IndexOf",
"System.String.LastIndexOf",
"System.String.Replace",
"System.String.Split",
"System.String.Trim",
"System.String.TrimStart",
"System.String.TrimEnd",
"System.String.Insert",
"System.String.Remove",
"System.String.Substring",
"System.String.IsNullOrEmpty",
"System.String.IsNullOrWhiteSpace",
"System.Text.StringBuilder.Append",
"System.Text.StringBuilder.AppendLine",
"System.Text.StringBuilder.Insert",
"System.Text.StringBuilder.Replace",
"System.Text.StringBuilder.Remove",
"System.Globalization.CharUnicodeInfo.GetUnicodeCategory",
"System.Globalization.CharUnicodeInfo.GetDigitValue",
"System.Globalization.CharUnicodeInfo.GetNumericValue",
"System.Linq.Enumerable.Where",
"System.Linq.Enumerable.Select",
"System.Linq.Enumerable.FirstOrDefault",
"System.Linq.Enumerable.First",
"System.Linq.Enumerable.Any",
"System.Linq.Enumerable.OrderBy",
"System.Linq.Enumerable.OrderByDescending",
"System.Linq.Enumerable.GroupBy",
"System.Linq.Enumerable.ToList",
"System.Linq.Enumerable.ToArray",
"System.Linq.Enumerable.Contains",
"System.Linq.Enumerable.ElementAt",
"System.Linq.Enumerable.ElementAtOrDefault",
"System.Linq.Enumerable.All",
"System.Linq.Enumerable.Count",
"System.Linq.Enumerable.Last",
"System.Linq.Enumerable.LastOrDefault"



        };
        private static readonly char[] separator = new[] { ',' };

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
            
            // Register for object creation expressions (for records and classes)
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            // Report info diagnostic once per compilation (first invocation)
            if (!_infoReported)
            {
                ReportInfoDiagnostic(context);
                _infoReported = true;
            }

            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol))
                return;

            // Check if the method is excluded (pass methodSymbol for full name check)
            if (IsMethodExcluded(methodSymbol.Name, methodSymbol))
                return;

            // If OnlyForRecords is enabled, check if the containing type is a record
            if (OnlyForRecords)
            {
                var containingType = GetContainingType(invocation, context.SemanticModel);
                bool isRecord = containingType != null && IsRecord(containingType);

                if (!isRecord)
                    return;
            }

            var methodFullName = (methodSymbol.ContainingType?.ToDisplayString() ?? "") + "." + methodSymbol.Name;

            // Analyze the arguments
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                if (arg.NameColon == null)
                {
                    // Get parameter for this argument to include in diagnostic message
                    string paramName = GetParameterName(arg, invocation, methodSymbol, context.SemanticModel);
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        var diagnostic = Diagnostic.Create(Rule, arg.GetLocation(), paramName, methodFullName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            // Report info diagnostic once per compilation (first invocation)
            if (!_infoReported)
            {
                ReportInfoDiagnostic(context);
                _infoReported = true;
            }

            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            
            if (!(context.SemanticModel.GetSymbolInfo(objectCreation).Symbol is IMethodSymbol constructorSymbol))
                return;
                
            // Check if the method is excluded (pass constructorSymbol for full name check)
            string constructorName = constructorSymbol.ContainingType.Name + "." + constructorSymbol.Name;
            if (IsMethodExcluded(constructorName, constructorSymbol))
                return;
                
            // If OnlyForRecords is enabled, check if the type is a record
            if (OnlyForRecords)
            {
                var typeSymbol = constructorSymbol.ContainingType;
                bool isRecord = typeSymbol != null && IsRecord(typeSymbol);

                if (!isRecord)
                    return;
            }
            
            var ctorFullName = (constructorSymbol.ContainingType?.ToDisplayString() ?? "") + "." + constructorSymbol.Name;

            // Analyze the arguments
            if (objectCreation.ArgumentList != null)
            {
                foreach (var arg in objectCreation.ArgumentList.Arguments)
                {
                    if (arg.NameColon == null)
                    {
                        // Get parameter for this argument to include in diagnostic message
                        string paramName = GetParameterNameForConstructor(arg, objectCreation, constructorSymbol, context.SemanticModel);
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            var diagnostic = Diagnostic.Create(Rule, arg.GetLocation(), paramName, ctorFullName);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static string GetParameterNameForConstructor(ArgumentSyntax arg, ObjectCreationExpressionSyntax objectCreation, 
            IMethodSymbol constructorSymbol, SemanticModel semanticModel)
        {
            int argIndex = objectCreation.ArgumentList.Arguments.IndexOf(arg);
            if (argIndex < constructorSymbol.Parameters.Length)
            {
                return constructorSymbol.Parameters[argIndex].Name;
            }
            return arg.Expression.ToString();
        }

        /// <summary>
        /// Reports an info diagnostic to inform the user about the current analyzer options.
        /// </summary>
        private void ReportInfoDiagnostic(SyntaxNodeAnalysisContext context)
        {
            // Attach to the root node so it appears once per file/compilation
            var location = context.Node.SyntaxTree.GetRoot().GetLocation();
            
            // Build the diagnostic message
            var diagnostic = Diagnostic.Create(
                InfoRule,
                location,
                OnlyForRecords.ToString(),
                string.IsNullOrEmpty(ExcludedMethodNames) ? "(none)" : ExcludedMethodNames,
                UseDefaultExcludedMethods.ToString()
            );
            
            context.ReportDiagnostic(diagnostic);
            
            // If default excluded methods are enabled, report them in a separate diagnostic
            if (UseDefaultExcludedMethods && DefaultExcludedMethods.Count > 0)
            {
                var defaultMethodsStr = string.Join(", ", DefaultExcludedMethods);
                var defaultMethodsInfo = Diagnostic.Create(
                    DefaultMethodsRule,
                    location,
                    defaultMethodsStr
                );
                
                context.ReportDiagnostic(defaultMethodsInfo);
            }
        }

        private static string GetParameterName(ArgumentSyntax arg, InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol, SemanticModel semanticModel)
        {
            int argIndex = invocation.ArgumentList.Arguments.IndexOf(arg);
            if (argIndex < methodSymbol.Parameters.Length)
            {
                return methodSymbol.Parameters[argIndex].Name;
            }
            return arg.Expression.ToString();
        }

        private static INamedTypeSymbol GetContainingType(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            // Get the method symbol from the invocation
            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                return null;
                
            // For extension methods, the real containing type is the first parameter type
            if (methodSymbol.IsExtensionMethod && methodSymbol.Parameters.Length > 0)
            {
                return methodSymbol.Parameters[0].Type as INamedTypeSymbol;
            }
            
            // For regular methods, return the containing type
            return methodSymbol.ContainingType;
        }

        private bool IsMethodExcluded(string methodName, IMethodSymbol methodSymbol = null)
        {
            if (string.IsNullOrEmpty(methodName))
                return false;

            // Check default excluded methods (by fully qualified name) if enabled
            if (UseDefaultExcludedMethods && methodSymbol != null)
            {
                var fullName = methodSymbol.ContainingType?.ToDisplayString() + "." + methodSymbol.Name;
                if (DefaultExcludedMethods.Contains(fullName))
                    return true;
            }

            if (string.IsNullOrEmpty(ExcludedMethodNames))
                return false;

            // Parse exclusion list: allow both simple and fully qualified names
            var excludedMethods = ExcludedMethodNames
                .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .ToImmutableHashSet();

            // Check for simple name
            if (excludedMethods.Contains(methodName))
                return true;

            // Check for fully qualified name (Namespace.Type.Method)
            if (methodSymbol != null)
            {
                var fullName = methodSymbol.ContainingType?.ToDisplayString() + "." + methodSymbol.Name;
                if (excludedMethods.Contains(fullName))
                    return true;
            }

            return false;
        }

        private static bool IsRecord(INamedTypeSymbol type)
        {
            if (type == null)
                return false;
                
            // Method 1: Check IsRecord property directly through reflection
            // This works for C# 9+ record declarations in newer Roslyn versions
            try
            {
                var propertyInfo = type.GetType().GetProperty("IsRecord");
                if (propertyInfo != null)
                {
                    var isRecordValue = propertyInfo.GetValue(type);
                    if (isRecordValue is bool isRecord && isRecord)
                        return true;
                }
            }
            catch
            {
                // Reflection failed, continue with other detection methods
            }
            
            // Method 2: Check for record keyword in declaration syntax (for C# 9+)
            if (type.DeclaringSyntaxReferences.Length > 0)
            {
                try
                {
                    var syntax = type.DeclaringSyntaxReferences[0].GetSyntax();
                    var syntaxType = syntax.GetType();
                    var isRecordProperty = syntaxType.GetProperty("IsRecord");
                    if (isRecordProperty != null)
                    {
                        var isRecordValue = isRecordProperty.GetValue(syntax);
                        if (isRecordValue is bool isSyntaxRecord && isSyntaxRecord)
                            return true;
                    }
                }
                catch
                {
                    // Reflection failed, continue with other detection methods
                }
            }
            
            // Method 3: Check for record runtime characteristics
            
            // Check for record's generated Equals/GetHashCode overrides
            bool hasSpecialEquals = type.GetMembers()
                .Where(m => m.Name == "Equals" && m is IMethodSymbol)
                .Any(m => ((IMethodSymbol)m).Parameters.Length == 1 && 
                          ((IMethodSymbol)m).GetAttributes().Any(attr => 
                              attr.AttributeClass?.Name == "CompilerGeneratedAttribute"));
                              
            // Check for EqualityContract property (most reliable for .NET Standard 2.0)
            bool hasEqualityContract = false;
            foreach (var member in type.GetMembers())
            {
                if (member.Name == "EqualityContract" && member is IPropertySymbol)
                {
                    hasEqualityContract = member.GetAttributes().Any(attr => 
                        attr.AttributeClass?.Name == "CompilerGeneratedAttribute" ||
                        (attr.AttributeClass?.ContainingNamespace?.Name == "CompilerServices" &&
                         attr.AttributeClass?.ContainingNamespace?.ContainingNamespace?.Name == "Runtime"));
                         
                    if (hasEqualityContract)
                        return true;
                }
            }
            
            // Method 4: Check for other record characteristics
            bool hasClone = type.GetMembers().Any(m => m.Name == "<Clone>$" && m is IMethodSymbol);
            bool hasPrintMembers = type.GetMembers().Any(m => m.Name == "PrintMembers" && m is IMethodSymbol);
            bool hasDeconstruct = type.GetMembers().Any(m => m.Name == "Deconstruct" && m is IMethodSymbol);
            
            // Method 5: Check for record-specific ToString override pattern
            bool hasSpecialToString = type.GetMembers()
                .Where(m => m.Name == "ToString" && m is IMethodSymbol)
                .Any(m => ((IMethodSymbol)m).Parameters.Length == 0 && 
                          ((IMethodSymbol)m).GetAttributes().Any(attr => 
                              attr.AttributeClass?.Name == "CompilerGeneratedAttribute"));
            
            // Method 6: Check for property pattern with init-only setters (common in records)
            bool hasInitOnlyProperties = type.GetMembers()
                .Where(m => m is IPropertySymbol)
                .Cast<IPropertySymbol>()
                .Any(p => p.SetMethod != null && p.SetMethod.IsInitOnly);
            
            return hasClone || hasPrintMembers || hasDeconstruct || hasSpecialToString || 
                   hasSpecialEquals || hasInitOnlyProperties;
        }
    }
}
