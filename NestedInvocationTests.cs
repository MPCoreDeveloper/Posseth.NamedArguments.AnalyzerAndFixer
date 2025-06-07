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