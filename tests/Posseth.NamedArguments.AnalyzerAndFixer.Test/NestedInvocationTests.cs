using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.Threading.Tasks;

namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    public class NestedInvocationTests
    {
        [Fact]
        public async Task NestedInvocations_AreFixed()
        {
            // Configuratie om Info-diagnostieken te negeren
            var test = new CSharpCodeFixTest<NamedArgumentsAnalyzer, NamedArgumentsCodeFixProvider, DefaultVerifier>();
            test.TestState.Sources.Add(@"
using System;

class TimeProvider 
{
    public DateTime GetLocalNow() => DateTime.Now;
}

class S3Provider 
{
    public string GetGetPresignedUrl(string bucketName, string objectKey, DateTime expires) => $""{bucketName}/{objectKey}/{expires}"";
}

class AppConfiguration 
{
    public class AmazonConfig 
    {
        public class S3Config 
        {
            public string BucketName { get; set; } = ""my-bucket"";
        }
        
        public S3Config S3 { get; set; } = new S3Config();
    }
    
    public AmazonConfig Amazon { get; set; } = new AmazonConfig();
}

class Program
{
    public Uri GetGetPresignedUrl(string objectKey)
    {
        var s3Provider = new S3Provider();
        var appConfiguration = new AppConfiguration();
        var timeProvider = new TimeProvider();
        
        return new Uri(
            s3Provider.GetGetPresignedUrl(
                appConfiguration.Amazon.S3.BucketName,
                objectKey,
                timeProvider.GetLocalNow().AddHours(24)
            )
        );
    }
}");
            test.FixedState.Sources.Add(@"
using System;

class TimeProvider 
{
    public DateTime GetLocalNow() => DateTime.Now;
}

class S3Provider 
{
    public string GetGetPresignedUrl(string bucketName, string objectKey, DateTime expires) => $""{bucketName}/{objectKey}/{expires}"";
}

class AppConfiguration 
{
    public class AmazonConfig 
    {
        public class S3Config 
        {
            public string BucketName { get; set; } = ""my-bucket"";
        }
        
        public S3Config S3 { get; set; } = new S3Config();
    }
    
    public AmazonConfig Amazon { get; set; } = new AmazonConfig();
}

class Program
{
    public Uri GetGetPresignedUrl(string objectKey)
    {
        var s3Provider = new S3Provider();
        var appConfiguration = new AppConfiguration();
        var timeProvider = new TimeProvider();
        
        return new Uri(
            uriString: s3Provider.GetGetPresignedUrl(
                bucketName: appConfiguration.Amazon.S3.BucketName,
                objectKey: objectKey,
                expires: timeProvider.GetLocalNow().AddHours(value: 24)
            )
        );
    }
}");
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(39, 17).WithArguments("bucketName", "S3Provider.GetGetPresignedUrl"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(40, 17).WithArguments("objectKey", "S3Provider.GetGetPresignedUrl"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(41, 17).WithArguments("expires", "S3Provider.GetGetPresignedUrl"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(41, 53).WithArguments("value", "System.DateTime.AddHours"));
            test.TestState.ExpectedDiagnostics.Add(
                DiagnosticResult.CompilerWarning(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(38, 13).WithArguments("uriString", "System.Uri..ctor"));
            
            // Negeer Info-diagnostieken
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
[*.cs]
dotnet_analyzer_diagnostic.severity = warning
"));

            await test.AssertTestPassesAsync(TestContext.Current.CancellationToken);
        }
    }
}
