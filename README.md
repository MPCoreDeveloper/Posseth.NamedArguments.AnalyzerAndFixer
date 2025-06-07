# Posseth NamedArguments AnalyzerAndFixer

This is a tool that can be used to analyze and fix named arguments in codebase.
It is implemented as a Analyzer and CodeFix provider for Roslyn.

## Installation
There should be a NuGet package available for this project.

## configuration
<b>How to Configure Settings for NamedArgumentsAnalyzer</b>

OnlyForRecords is a boolean and standard value is false.
setting that determines whether the analyzer should only apply to record types. If set to true,
the analyzer will only analyze named arguments in record types. If set to false,
it will analyze named arguments in all types.

UseDefaultExcludedMethods is a boolean and standard value is true.
if UseDefaultExcludedMethods is true, the following methods will be excluded by default:
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


ExcludedMethodNames is a comma-separated list of method names you want to exclude from the analyzer.
default value is "" thus empty.


You can configure the OnlyForRecords and ExcludedMethodNames settings using an .editorconfig file. Here's how to do it:

Option 1: Using .editorconfig File

1.	Create or open an .editorconfig file in the root of your solution or project.
2.	Add the following configuration entries:

```ini
# Named Arguments Analyzer Configuration
[*.{cs,vb}]

# Configure OnlyForRecords option
dotnet_diagnostic.PNA1000.OnlyForRecords = true  # or false
dotnet_diagnostic.PNA1000.UseDefaultExcludedMethods = true  # or false

# Configure ExcludedMethodNames option
dotnet_diagnostic.PNA1000.ExcludedMethodNames = ToString,Equals,GetHashCode
```
Option 2: Configure in Visual Studio

1.	Right-click on your solution in Solution Explorer
2.	Select Analyze > Configure Code Analysis > For Solution
3.	In the dialog that appears, find your analyzer (PNA1000)
4.	Configure the settings for OnlyForRecords and ExcludedMethodNames

Option 3: Configure in Project File (.csproj)
You can also add these settings to your project file:
```xml
<PropertyGroup>
  <AnalysisMode>All</AnalysisMode>
</PropertyGroup>
<ItemGroup>
  <EditorConfigFiles Include=".editorconfig" />
</ItemGroup>
<PropertyGroup>
  <AnalyzerConfigFiles Include="$(MSBuildProjectDirectory)\.analyzer.config" />
</PropertyGroup>
```
Then create a .analyzer.config file with:
```ini
is_global = true
PNA1000.OnlyForRecords = true
PNA1000.UseDefaultExcludedMethods = true
PNA1000.ExcludedMethodNames = ToString,Equals,GetHashCode
```

The most common and recommended approach is Option 1 with the .editorconfig file,
as it's the standard way to configure analyzer settings in modern .NET projects.
