namespace PracticalWork.Library.Data.Minio;

public class MinioOptions
{
    public string Endpoint { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public int ExpInSeconds { get; set; }
}