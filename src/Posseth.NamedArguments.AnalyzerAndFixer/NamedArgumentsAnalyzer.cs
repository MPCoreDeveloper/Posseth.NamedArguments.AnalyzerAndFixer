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
        private static readonly LocalizableString MessageFormat = "Argument '{0}' in method '{1}' should be named";
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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(Rule, InfoRule, DefaultMethodsRule);

        /// <summary>
        /// The analyzer settings, resolved from the <c>dotnet_diagnostic.PNA1000.*</c> options in
        /// <c>.editorconfig</c>. Settings are captured per analysis callback so the analyzer remains
        /// thread-safe together with <see cref="AnalysisContext.EnableConcurrentExecution"/>.
        /// </summary>
        private sealed class AnalyzerSettings
        {
            public bool OnlyForRecords { get; }
            public string ExcludedMethodNames { get; }
            public bool UseDefaultExcludedMethods { get; }

            public AnalyzerSettings(bool onlyForRecords, string excludedMethodNames, bool useDefaultExcludedMethods)
            {
                OnlyForRecords = onlyForRecords;
                ExcludedMethodNames = excludedMethodNames;
                UseDefaultExcludedMethods = useDefaultExcludedMethods;
            }

            public static AnalyzerSettings From(AnalyzerConfigOptions options)
            {
                return new AnalyzerSettings(
                    onlyForRecords: GetBool(options, "OnlyForRecords", defaultValue: false),
                    excludedMethodNames: GetString(options, "ExcludedMethodNames", defaultValue: string.Empty),
                    useDefaultExcludedMethods: GetBool(options, "UseDefaultExcludedMethods", defaultValue: true));
            }

            public bool IsDefault => !OnlyForRecords && string.IsNullOrEmpty(ExcludedMethodNames) && UseDefaultExcludedMethods;

            private static bool GetBool(AnalyzerConfigOptions options, string optionName, bool defaultValue)
            {
                if (options.TryGetValue("dotnet_diagnostic.PNA1000." + optionName, out var value)
                    && bool.TryParse(value, out var parsed))
                {
                    return parsed;
                }

                return defaultValue;
            }

            private static string GetString(AnalyzerConfigOptions options, string optionName, string defaultValue)
            {
                return options.TryGetValue("dotnet_diagnostic.PNA1000." + optionName, out var value)
                    ? value
                    : defaultValue;
            }
        }

        // Common methods to exclude by default (in addition to user-specified ones)
        private readonly HashSet<string> DefaultExcludedMethods = new HashSet<string>
        {
"char.Equals",
"char.CompareTo",
"char.GetUnicodeCategory",
"char.IsControl",
"char.IsDigit",
"char.IsLetter",
"char.IsLetterOrDigit",
"char.IsLower",
"char.IsNumber",
"char.IsPunctuation",
"char.IsSeparator",
"char.IsSurrogate",
"char.IsSymbol",
"char.IsUpper",
"char.IsWhiteSpace",
"char.ToLower",
"char.ToLowerInvariant",
"char.ToUpper",
"char.ToUpperInvariant",
"char.GetNumericValue",
"Console.Write",
"Console.WriteLine",
"Debug.WriteLine",
"Trace.WriteLine",
"string.Contains",
"string.EndsWith",
"string.StartsWith",
"string.Equals",
"string.IndexOf",
"string.LastIndexOf",
"string.Replace",
"string.Split",
"string.Trim",
"string.TrimStart",
"string.TrimEnd",
"string.Insert",
"string.Remove",
"string.Substring",
"string.IsNullOrEmpty",
"string.IsNullOrWhiteSpace",
"string.Join",
"string.Concat",
"string.Format",
"StringBuilder.Append",
"StringBuilder.AppendLine",
"StringBuilder.Insert",
"StringBuilder.Replace",
"StringBuilder.Remove",
"CharUnicodeInfo.GetUnicodeCategory",
"CharUnicodeInfo.GetDigitValue",
"CharUnicodeInfo.GetNumericValue",
"Math.Min",
"Math.Max",
"List.Add",
"Dictionary.Add",
"Enumerable.Where",
"Enumerable.Select",
"Enumerable.FirstOrDefault",
"Enumerable.First",
"Enumerable.Any",
"Enumerable.OrderBy",
"Enumerable.OrderByDescending",
"Enumerable.GroupBy",
"Enumerable.ToList",
"Enumerable.ToArray",
"Enumerable.Contains",
"Enumerable.ElementAt",
"Enumerable.ElementAtOrDefault",
"Enumerable.All",
"Enumerable.Count",
"Enumerable.Last",
"Enumerable.LastOrDefault"



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

            // Report the info diagnostics once per syntax tree, then register the per-syntax actions.
            // Reporting per tree (instead of from syntax node actions) avoids shared mutable state,
            // which is required for safe use together with EnableConcurrentExecution().
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

            // Register for invocation expressions instead of arguments directly
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

            // Register for object creation expressions (for records and classes)
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            // Attach to the root of the tree so it appears once per file
            var settings = AnalyzerSettings.From(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree));
            ReportInfoDiagnostic(context.Tree.GetRoot().GetLocation(), context.ReportDiagnostic, settings);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var settings = AnalyzerSettings.From(context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.SyntaxTree));

            if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol))
                return;

            // Check if the method is excluded (pass methodSymbol for full name check)
            if (IsMethodExcluded(methodSymbol.Name, methodSymbol, settings))
                return;

            // If OnlyForRecords is enabled, check if the containing type is a record
            if (settings.OnlyForRecords)
            {
                var containingType = GetContainingType(invocation, context.SemanticModel);
                bool isRecord = containingType != null && IsRecord(containingType);

                if (!isRecord)
                    return;
            }

            var methodFullName = GetFullMethodName(methodSymbol);

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
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            var settings = AnalyzerSettings.From(context.Options.AnalyzerConfigOptionsProvider.GetOptions(objectCreation.SyntaxTree));
            
            if (!(context.SemanticModel.GetSymbolInfo(objectCreation).Symbol is IMethodSymbol constructorSymbol))
                return;
                
            // Check if the method is excluded (pass constructorSymbol for full name check)
            string constructorName = constructorSymbol.ContainingType.Name + "." + constructorSymbol.Name;
            if (IsMethodExcluded(constructorName, constructorSymbol, settings))
                return;
                
            // If OnlyForRecords is enabled, check if the type is a record
            if (settings.OnlyForRecords)
            {
                var typeSymbol = constructorSymbol.ContainingType;
                bool isRecord = typeSymbol != null && IsRecord(typeSymbol);

                if (!isRecord)
                    return;
            }
            
            var ctorFullName = GetFullMethodName(constructorSymbol);

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
        /// Reported once per syntax tree via <see cref="SyntaxTreeAnalysisContext"/>.
        /// </summary>
        private void ReportInfoDiagnostic(Location location, Action<Diagnostic> reportDiagnostic, AnalyzerSettings settings)
        {
            // Only surface the options info diagnostics when the analyzer configuration has actually
            // been customized. With all-default settings there is nothing meaningful to report and
            // showing these info diagnostics on every file would only add noise.
            if (settings.IsDefault)
            {
                return;
            }

            // Build the diagnostic message
            var diagnostic = Diagnostic.Create(
                InfoRule,
                location,
                settings.OnlyForRecords.ToString(),
                string.IsNullOrEmpty(settings.ExcludedMethodNames) ? "(none)" : settings.ExcludedMethodNames,
                settings.UseDefaultExcludedMethods.ToString()
            );

            reportDiagnostic(diagnostic);

            // If default excluded methods are enabled, report them in a separate diagnostic
            if (settings.UseDefaultExcludedMethods && DefaultExcludedMethods.Count > 0)
            {
                var defaultMethodsStr = string.Join(", ", DefaultExcludedMethods);
                var defaultMethodsInfo = Diagnostic.Create(
                    DefaultMethodsRule,
                    location,
                    defaultMethodsStr
                );

                reportDiagnostic(defaultMethodsInfo);
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
                
            // For extension methods, the real containing type is the receiver type.
            // Reduced extension methods expose the receiver via the first parameter of ReducedFrom;
            // unreduced symbols expose it as their first parameter.
            if (methodSymbol.ReducedFrom != null)
            {
                if (methodSymbol.ReducedFrom.Parameters.Length > 0)
                    return methodSymbol.ReducedFrom.Parameters[0].Type as INamedTypeSymbol;
            }
            else if (methodSymbol.IsExtensionMethod && methodSymbol.Parameters.Length > 0)
            {
                return methodSymbol.Parameters[0].Type as INamedTypeSymbol;
            }
            
            // For regular methods, return the containing type
            return methodSymbol.ContainingType;
        }

        private bool IsMethodExcluded(string methodName, IMethodSymbol methodSymbol, AnalyzerSettings settings)
        {
            if (string.IsNullOrEmpty(methodName))
                return false;

            // Check default excluded methods (by fully qualified name) if enabled
            if (settings.UseDefaultExcludedMethods && methodSymbol != null)
            {
                var fullName = GetFullMethodName(methodSymbol); // e.g., System.Linq.Enumerable.Where

                // also build minimally qualified and simple variants to match list entries
                var minimalType = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty; // e.g., Enumerable
                var minimalFull = string.IsNullOrEmpty(minimalType) ? methodSymbol.Name : minimalType + "." + methodSymbol.Name; // e.g., Enumerable.Where
                var simpleType = methodSymbol.ContainingType?.Name ?? string.Empty; // e.g., Enumerable
                var simpleFull = string.IsNullOrEmpty(simpleType) ? methodSymbol.Name : simpleType + "." + methodSymbol.Name; // e.g., Enumerable.Where

                if (DefaultExcludedMethods.Contains(fullName) || DefaultExcludedMethods.Contains(minimalFull) || DefaultExcludedMethods.Contains(simpleFull))
                    return true;
            }

            if (string.IsNullOrEmpty(settings.ExcludedMethodNames))
                return false;

            // Parse exclusion list: allow both simple and fully qualified names
            var excludedMethods = settings.ExcludedMethodNames
                .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Select(StripGlobalPrefix) // remove optional global:: prefix if given
                .Where(m => !string.IsNullOrEmpty(m))
                .ToImmutableHashSet();

            // Check for simple name
            if (excludedMethods.Contains(methodName))
                return true;

            // Check for fully qualified name (Namespace.Type.Method)
            if (methodSymbol != null)
            {
                var fullName = GetFullMethodName(methodSymbol);
                if (excludedMethods.Contains(fullName))
                    return true;

                // also check minimally qualified
                var minimalType = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty;
                var minimalFull = string.IsNullOrEmpty(minimalType) ? methodSymbol.Name : minimalType + "." + methodSymbol.Name;
                if (excludedMethods.Contains(minimalFull))
                    return true;
            }

            return false;
        }

        private static bool IsRecord(INamedTypeSymbol type)
        {
            if (type == null)
                return false;

            // INamedTypeSymbol.IsRecord is available since Roslyn 4.1 (C# 9). The specific catch
            // guards hosts that may load an older Roslyn at runtime, where the member does not exist.
            try
            {
                return type.IsRecord;
            }
            catch (MissingMethodException)
            {
                return false;
            }
        }

        private static string GetFullMethodName(IMethodSymbol methodSymbol)
        {
            if (methodSymbol == null) return string.Empty;
            var typeName = GetFullTypeName(methodSymbol.ContainingType);
            return string.IsNullOrEmpty(typeName) ? methodSymbol.Name : typeName + "." + methodSymbol.Name;
        }

        private static string GetFullTypeName(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null) return string.Empty;
            var full = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            // strip global:: prefix if present
            return StripGlobalPrefix(full);
        }

        /// <summary>
        /// Removes a leading "global::" prefix from a (possibly fully qualified) member name.
        /// </summary>
        private static string StripGlobalPrefix(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            const string GlobalPrefix = "global::";
            if (value.StartsWith(GlobalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring(GlobalPrefix.Length);
            }

            return value;
        }
    }
}
