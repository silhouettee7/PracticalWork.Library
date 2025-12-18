namespace PracticalWork.Library.Options;

public class MinioOptions
{
    public string Endpoint { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string ReportsBucketName { get; set; }
    public string CoversBucketName { get; set; }
    public int ExpInSeconds { get; set; }
}