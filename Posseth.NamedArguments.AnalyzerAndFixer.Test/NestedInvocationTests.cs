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
            // Create code with nested method calls
            var test = @"
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
}";

            var fixedTest = @"
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
bucketName: appConfiguration.Amazon.S3.BucketName,
objectKey: objectKey,
expires: timeProvider.GetLocalNow().AddHours(value: 24))
        );
    }
}";

            // Expected diagnostics
            var expected = new[]
            {
                // Arguments in the GetGetPresignedUrl method
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(39, 17).WithArguments("bucketName"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(40, 17).WithArguments("objectKey"),
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(41, 17).WithArguments("expires"),
                
                // Arguments in the AddHours method
                VerifyCS.Diagnostic(NamedArgumentsAnalyzer.DiagnosticId).WithLocation(41, 53).WithArguments("value"),
            };

            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedTest);
        }
    }
}