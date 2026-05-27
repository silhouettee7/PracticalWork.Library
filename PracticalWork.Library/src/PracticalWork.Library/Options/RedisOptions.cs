namespace PracticalWork.Library.Options;

public class RedisOptions
{
    public string RedisCacheConnection { get; set; }
    public string RedisCachePrefix { get; set; }
    public BooksCacheConfig Books { get; set; }
    public ReadersCacheConfig Readers { get; set; }
    public ReportsCacheConfig Reports { get; set; }
}

public class BooksCacheConfig
{
    public CacheEntryConfig BooksList { get; set; }
    public CacheEntryConfig LibraryBooks { get; set; }
    public CacheEntryConfig BookDetails { get; set; }
    public string VersionKey { get; set; }
}

public class ReadersCacheConfig
{
    public CacheEntryConfig ReaderBooks { get; set; }
    public string VersionKey { get; set; }
}

public class ReportsCacheConfig
{
    public CacheEntryConfig ReportsList { get; set; }
    public string VersionKey { get; set; }
}

public class CacheEntryConfig
{
    public string Prefix { get; set; }
    public int TtlInMinutes { get; set; }
}