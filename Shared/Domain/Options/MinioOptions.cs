namespace Domain.Options;

/// <summary>
/// Конфиг для минио
/// </summary>
public class MinioOptions
{
    /// <summary>
    /// Сетевой адрес минио
    /// </summary>
    public string Endpoint { get; set; }
    /// <summary>
    /// открытый ключ
    /// </summary>
    public string AccessKey { get; set; }
    /// <summary>
    /// закрытый ключ
    /// </summary>
    public string SecretKey { get; set; }
    /// <summary>
    /// название бакета с отчетами
    /// </summary>
    public string ReportsBucketName { get; set; }
    /// <summary>
    /// название бакета с обложками
    /// </summary>
    public string CoversBucketName { get; set; }

    public string ArchiveBooksBucketName { get; set; }
    public string ReportsAdministrationBucketName { get; set; }
    /// <summary>
    /// время истечения ссылки на файл
    /// </summary>
    public int ExpInSeconds { get; set; }
}