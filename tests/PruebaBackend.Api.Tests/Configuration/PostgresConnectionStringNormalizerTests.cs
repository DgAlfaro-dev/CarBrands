using Npgsql;
using PruebaBackend.Api.Configuration;

namespace PruebaBackend.Api.Tests.Configuration;

/// <summary>
/// Unit tests for the connection-string normalizer: PaaS providers hand out
/// postgres:// URIs (which Npgsql rejects) while local configuration uses
/// key/value pairs, and both must be accepted.
/// </summary>
public class PostgresConnectionStringNormalizerTests
{
    [Fact]
    public void Normalize_RenderInternalUri_ConvertsToKeyValues()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://carbrands:secret123@dpg-abc123-5432.oregon-postgres.render.com/carbrands");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("dpg-abc123-5432.oregon-postgres.render.com", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("carbrands", builder.Database);
        Assert.Equal("carbrands", builder.Username);
        Assert.Equal("secret123", builder.Password);
    }

    [Fact]
    public void Normalize_UriWithSslModeParameter_SetsRequire()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgresql://user:pass@db.internal:5432/carbrands?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal("db.internal", builder.Host);
    }

    [Fact]
    public void Normalize_QuotedUri_TrimsQuotesAndConverts()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "\"postgres://user:pass@db.internal/carbrands\"");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("db.internal", builder.Host);
        Assert.Equal("carbrands", builder.Database);
    }

    [Fact]
    public void Normalize_UriWithSurroundingWhitespace_Trims()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "\n  postgres://user:pass@db.internal/carbrands \n");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("db.internal", builder.Host);
    }

    [Fact]
    public void Normalize_KeyValueString_ReturnsItUnchanged()
    {
        const string keyValue =
            "Host=localhost;Port=5432;Database=prueba_backend;Username=postgres;Password=postgres";

        var result = PostgresConnectionStringNormalizer.Normalize(keyValue);

        Assert.Equal(keyValue, result);
    }

    [Fact]
    public void Normalize_UriWithoutPort_DefaultsTo5432()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://user:pass@db.internal/carbrands");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(5432, builder.Port);
    }

    [Fact]
    public void Normalize_UriWithoutPassword_LeavesPasswordEmpty()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://user@db.internal/carbrands");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("user", builder.Username);
        Assert.True(string.IsNullOrEmpty(builder.Password));
    }

    [Fact]
    public void Normalize_UnknownSslMode_KeepsTheDefault()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://user:pass@db.internal/carbrands?sslmode=bogus");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(SslMode.Prefer, builder.SslMode);
    }

    [Fact]
    public void Normalize_UriWithSslModeWithoutValue_KeepsTheDefault()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://user:pass@db.internal/carbrands?sslmode");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(SslMode.Prefer, builder.SslMode);
    }

    [Fact]
    public void Normalize_UriWithUnrelatedQueryParameter_KeepsTheDefault()
    {
        // Only sslmode is honoured, so any other parameter must neither break
        // normalization nor change the SSL mode.
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://user:pass@db.internal/carbrands?application_name=api");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(SslMode.Prefer, builder.SslMode);
        Assert.Equal("db.internal", builder.Host);
        Assert.Equal("carbrands", builder.Database);
    }

    [Fact]
    public void Normalize_PercentEncodedPassword_DecodesIt()
    {
        var result = PostgresConnectionStringNormalizer.Normalize(
            "postgres://user:p%40ss%2Fw0rd@db.internal/carbrands");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("p@ss/w0rd", builder.Password);
    }

    [Fact]
    public void Normalize_InvalidValue_ThrowsDescriptiveError()
    {
        // The message must not echo the raw value: it can contain a password.
        var exception = Assert.Throws<InvalidOperationException>(
            () => PostgresConnectionStringNormalizer.Normalize("not a connection string"));

        Assert.Contains("Postgres", exception.Message);
        Assert.Contains("postgres://", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.IsType<ArgumentException>(exception.InnerException);
    }
}
