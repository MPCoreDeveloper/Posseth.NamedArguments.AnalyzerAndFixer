# Posseth NamedArguments AnalyzerAndFixer

This is a Roslyn analyzer and code fix provider that enforces the use of named arguments in C# method calls and object creations. It helps improve code readability by requiring named arguments for methods with multiple parameters, while allowing exclusions for commonly used methods where positional arguments are preferred.

## Features

- **Diagnostic**: Warns when positional arguments are used in method calls or object creations with multiple parameters.
- **Code Fix**: Automatically adds named arguments to fix the diagnostic.
- **Configurable**: Supports configuration via .editorconfig files to customize behavior.
- **Exclusions**: Built-in list of excluded methods where positional arguments are acceptable, with options to add custom exclusions.
- **Record Support**: Optional mode to only analyze record types.



## Update notes
 - **v1.1.8** - 2025-06-15
 -Analyzer & code-fix test suite fully modernized to .NET 10, xUnit v3, Microsoft Testing Platform and the latest Roslyn testing
 -framework (1.1.2) using the current DefaultVerifier/TestState pattern — zero warnings, fully compatible with Visual Studio 2026
## Installation

### NuGet Package

Install the analyzer via NuGet:

```bash
dotnet add package Posseth.NamedArguments.AnalyzerAndFixer
```

Or search for "Posseth.NamedArguments.AnalyzerAndFixer" in the NuGet Package Manager.

### Manual Installation

1. Clone the repository.
2. Build the project.
3. Reference the analyzer DLLs in your project or use the VSIX for Visual Studio integration.

## Usage

The analyzer will automatically run during compilation and show warnings for method calls that should use named arguments.

### Example

**Before (triggers diagnostic):**
```csharp
public void Example()
{
    var result = string.Replace("hello world", "world", "universe");
}
```

**After (fixed):**
```csharp
public void Example()
{
    var result = string.Replace(oldValue: "world", newValue: "universe", input: "hello world");
}
```

Note: `string.Replace` is excluded by default, so this example won't trigger in practice. Use custom methods or disable exclusions to see the behavior.

## Configuration

The analyzer supports several configuration options via .editorconfig files.

### Options

- **OnlyForRecords** (boolean, default: false): If true, only analyzes method calls on record types.
- **UseDefaultExcludedMethods** (boolean, default: true): If true, uses the built-in list of excluded methods.
- **ExcludedMethodNames** (string, default: ""): Comma-separated list of additional method names to exclude.

If `UseDefaultExcludedMethods` is true, the following methods are excluded by default:
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

### Configuration Methods

#### Option 1: Using .editorconfig File (Recommended)

1. Create or open an .editorconfig file in the root of your solution or project.
2. Add the following configuration entries:

```ini
# Named Arguments Analyzer Configuration
[*.{cs,vb}]

# Configure OnlyForRecords option
dotnet_diagnostic.PNA1000.OnlyForRecords = true  # or false
dotnet_diagnostic.PNA1000.UseDefaultExcludedMethods = true  # or false

# Configure ExcludedMethodNames option
dotnet_diagnostic.PNA1000.ExcludedMethodNames = ToString,Equals,GetHashCode
```

#### Option 2: Configure in Visual Studio

1. Right-click on your solution in Solution Explorer.
2. Select **Analyze > Configure Code Analysis > For Solution**.
3. In the dialog that appears, find the analyzer (PNA1000).
4. Configure the settings for OnlyForRecords and ExcludedMethodNames.

#### Option 3: Configure in Project File (.csproj)

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

The most common and recommended approach is Option 1 with the .editorconfig file, as it's the standard way to configure analyzer settings in modern .NET projects.

## Contributing

Contributions are welcome! Please open issues or submit pull requests on GitHub.

## License

This project is licensed under the MIT License.
