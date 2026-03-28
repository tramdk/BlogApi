namespace BlogApi.Application.Common.Constants;

public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public static class PolicyConstants
{
    public const string MustBeAdmin = "MustBeAdmin";
    public const string MustBeAuthor = "MustBeAuthor";
}

public static class CorsConstants
{
    public const string AllowFrontend = "AllowFrontend";
}

public static class ConfigurationKeys
{
    public const string DefaultConnection = "DefaultConnection";
    public const string JwtSecret = "Jwt:Secret";
    public const string JwtIssuer = "Jwt:Issuer";
    public const string JwtAudience = "Jwt:Audience";
    public const string FileStorageUploadFolder = "FileStorage:UploadFolder";
    public const string FileStorageAllowedExtensions = "FileStorage:AllowedExtensions";
    public const string FileStorageMaxFileSizeInBytes = "FileStorage:MaxFileSizeInBytes";
    public const string IpRateLimiting = "IpRateLimiting";
    public const string Redis = "Redis";
}
