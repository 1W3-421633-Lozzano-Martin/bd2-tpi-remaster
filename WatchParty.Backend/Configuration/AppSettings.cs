namespace WatchParty.Backend.Configuration;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "WatchParty";
    public string Audience { get; set; } = "WatchParty";
    public int ExpirationDays { get; set; } = 7;
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "watchparty";
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class AppSettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;
}
