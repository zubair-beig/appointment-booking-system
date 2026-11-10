using Npgsql;

namespace AppointmentBook.Api.Data;

/// <summary>Resolves the PostgreSQL connection string for the appointment book from configuration.</summary>
public static class AppointmentBookConfigurationExtensions
{
    /// <summary>Name of the connection string under the "ConnectionStrings" configuration section.</summary>
    public const string ConnectionStringName = "AppointmentBook";

    /// <summary>
    /// Environment variables injected by the Vercel Supabase integration, in order of preference.
    /// The non-pooling URL is preferred because this API is a long-running container with its own
    /// connection pool; the pooled URL (Supavisor transaction mode, port 6543) is the fallback.
    /// </summary>
    private static readonly string[] SupabaseUrlVariableNames = ["POSTGRES_URL_NON_POOLING", "POSTGRES_URL"];

    private const int DefaultPostgresPort = 5432;
    private const int SupavisorTransactionModePort = 6543;
    private const string DefaultDatabaseName = "postgres";

    /// <summary>
    /// Returns an Npgsql connection string, checking (in order) <c>ConnectionStrings:AppointmentBook</c>,
    /// then <c>POSTGRES_URL_NON_POOLING</c>, then <c>POSTGRES_URL</c>.
    /// </summary>
    /// <param name="configuration">Application configuration (appsettings + environment variables).</param>
    /// <returns>An Npgsql key/value connection string, or <see langword="null"/> when nothing is configured.</returns>
    /// <remarks>
    /// Values may be either Npgsql key/value strings (<c>Host=...;Username=...</c>) or
    /// <c>postgres://user:password@host:port/database?sslmode=require</c> URLs as provided by Supabase.
    /// </remarks>
    /// <exception cref="FormatException">A configured URL is not a valid postgres:// URL.</exception>
    public static string? GetAppointmentBookConnectionString(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredValue = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            configuredValue = SupabaseUrlVariableNames
                .Select(variableName => configuration[variableName])
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        return string.IsNullOrWhiteSpace(configuredValue) ? null : ToNpgsqlConnectionString(configuredValue);
    }

    /// <summary>Converts a postgres:// URL to Npgsql format; key/value strings are returned unchanged.</summary>
    private static string ToNpgsqlConnectionString(string connectionValue)
    {
        var isUrlFormat = connectionValue.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || connectionValue.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
        if (!isUrlFormat)
        {
            return connectionValue;
        }

        if (!Uri.TryCreate(connectionValue, UriKind.Absolute, out var connectionUrl))
        {
            // The value itself is deliberately left out of the message because it contains the password.
            throw new FormatException(
                "The PostgreSQL connection URL is not a valid URL. Expected postgres://user:password@host:port/database.");
        }

        // User info is "user:password", each part percent-encoded; the password may itself contain ':'.
        var userInfoParts = connectionUrl.UserInfo.Split(':', 2);
        var queryParameters = ParseQueryParameters(connectionUrl.Query);
        var port = connectionUrl.IsDefaultPort || connectionUrl.Port < 0 ? DefaultPostgresPort : connectionUrl.Port;
        var databaseName = Uri.UnescapeDataString(connectionUrl.AbsolutePath.Trim('/'));

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = connectionUrl.Host,
            Port = port,
            Database = string.IsNullOrEmpty(databaseName) ? DefaultDatabaseName : databaseName,
            Username = Uri.UnescapeDataString(userInfoParts[0]),
            Password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : null,
            // Supabase only accepts encrypted connections; honour an explicit sslmode from the URL if present.
            SslMode = queryParameters.TryGetValue("sslmode", out var sslModeText)
                && Enum.TryParse<SslMode>(sslModeText, ignoreCase: true, out var parsedSslMode)
                    ? parsedSslMode
                    : SslMode.Require,
        };

        // A transaction-mode pooler shares server connections between clients, so Npgsql must not
        // send its per-connection reset (DISCARD ALL) when returning connections to its own pool.
        var isTransactionModePooler = port == SupavisorTransactionModePort
            || (queryParameters.TryGetValue("pgbouncer", out var pgBouncerText) && bool.TryParse(pgBouncerText, out var isPgBouncer) && isPgBouncer);
        if (isTransactionModePooler)
        {
            connectionStringBuilder.NoResetOnClose = true;
        }

        return connectionStringBuilder.ConnectionString;
    }

    /// <summary>Parses "?a=1&amp;b=2" into a case-insensitive dictionary (unknown keys such as "supa" are ignored by callers).</summary>
    private static Dictionary<string, string> ParseQueryParameters(string query)
    {
        return query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => Uri.UnescapeDataString(parts[0]), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => Uri.UnescapeDataString(group.Last()[1]), StringComparer.OrdinalIgnoreCase);
    }
}
