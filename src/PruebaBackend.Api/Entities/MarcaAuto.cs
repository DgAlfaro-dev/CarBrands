namespace PruebaBackend.Api.Entities;

/// <summary>
/// A car brand in the catalog. The table name (MarcasAutos) and the minimum
/// seed are fixed by the test statement, so the Spanish domain names are kept.
/// </summary>
public class MarcaAuto
{
    public int Id { get; set; }

    /// <summary>Display name of the brand, for example "Toyota".</summary>
    public string Nombre { get; set; } = string.Empty;
}
