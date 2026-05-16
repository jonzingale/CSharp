namespace Finance.Core.Data;

public sealed class DbOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "finance";
    public string Username { get; init; } = "postgres";
    public string Password { get; init; } = "postgres";

    public string ToConnectionString()
        => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}
