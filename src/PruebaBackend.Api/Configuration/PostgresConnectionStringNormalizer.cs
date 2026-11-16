using Npgsql;

namespace PruebaBackend.Api.Configuration;

/// <summary>
/// Normalizes the PostgreSQL connection string read from configuration so both
/// shapes in use are accepted: key/value pairs (appsettings.json, docker
/// compose) and PostgreSQL URIs. PaaS providers such as Render hand out
/// <c>postgres://user:pass@host:5432/db</c> URLs, which
/// NpgsqlConnectionStringBuilder rejects with "Format of the initialization
/// string does not conform to specification starting at index 0", so the URI
/// form is translated to key/value pairs before the provider sees it.
/// </summary>
public static class PostgresConnectionStringNormalizer
{
    /// <summary>
    /// Trims accidental whitespace/quotes, converts a <c>postgres://</c> URI to
    /// its key/value equivalent and validates that the result can be parsed.
    /// </summary>
    /// <returns>The connection string in key/value format.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value is neither a valid PostgreSQL URI nor a valid key/value
    /// connection string.
    /// </exception>
    public static string Normalize(string value)
    {
        var normalized = value.Trim().Trim('"').Trim();

        if (normalized.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = FromUri(normalized);
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(normalized);
        }
        catch (ArgumentException ex)
        {
            // The raw value is not echoed on purpose: it may contain the
            // database password, which must never reach the logs.
            throw new InvalidOperationException(
                "La cadena de conexión 'Postgres' no es válida. Usa el formato " +
                "Host=...;Port=...;Database=...;Username=...;Password=... o una URL " +
                "postgres:// de tu proveedor de base de datos.", ex);
        }

        return normalized;
    }

    /// <summary>
    /// Converts a PostgreSQL URI into an equivalent key/value connection string.
    /// Only the optional <c>sslmode</c> query parameter is honoured (the one
    /// providers such as Render append to their external database URLs); any
    /// other parameter is ignored.
    /// </summary>
    private static string FromUri(string uriValue)
    {
        var uri = new Uri(uriValue);

        var userInfo = uri.UserInfo;
        var separatorIndex = userInfo.IndexOf(':');
        var username = separatorIndex >= 0 ? userInfo[..separatorIndex] : userInfo;
        var password = separatorIndex >= 0 ? userInfo[(separatorIndex + 1)..] : string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            // A URI without an explicit port means the scheme default; for the
            // postgres:// scheme that is 5432 (Uri.Port reports -1 instead).
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(username),
            Password = Uri.UnescapeDataString(password),
        };

        var sslMode = GetQueryParameter(uri, "sslmode");
        if (sslMode is not null && Enum.TryParse<SslMode>(sslMode, ignoreCase: true, out var parsed))
        {
            builder.SslMode = parsed;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Returns the value of a query-string parameter, or <c>null</c> when the
    /// parameter is absent or has no value (both cases leave the SSL mode at
    /// its default of <see cref="SslMode.Prefer"/>).
    /// </summary>
    private static string? GetQueryParameter(Uri uri, string name)
    {
        var query = uri.Query.TrimStart('?');

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], name, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }
}
