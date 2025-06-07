using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Posseth.NamedArguments.AnalyzerAndFixer.Test.CSharpCodeFixVerifier<
    Posseth.NamedArguments.AnalyzerAndFixer.NamedArgumentsAnalyzer,
    Posseth.NamedArguments.AnalyzerAndFixer.NamedArgumentsCodeFixProvider>;

namespace Posseth.NamedArguments.AnalyzerAndFixer.Test
{
    [TestClass]
    public class NestedInvocationTests
    {
        [TestMethod]
        public async Task NestedInvocations_AreFixed()
        {
            // Configuratie om Info-diagnostieken te negeren
            var test = new VerifyCS.Test
            {
                TestCode = @"
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
}",
                FixedCode = @"
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
}",
                ExpectedDiagnostics = {
                    VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(39, 17).WithArguments("bucketName"),
                    VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(40, 17).WithArguments("objectKey"),
                    VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(41, 17).WithArguments("expires"),
                    VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(41, 53).WithArguments("value"),
                    VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(38, 13).WithArguments("uriString"),
                },
            };
            
            // Negeer Info-diagnostieken
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
[*.cs]
dotnet_analyzer_diagnostic.severity = warning
"));

            await test.RunAsync();
        }
    }
}