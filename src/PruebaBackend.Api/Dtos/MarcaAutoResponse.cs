namespace PruebaBackend.Api.Dtos;

/// <summary>
/// Output contract of the brands endpoint. A DTO is exposed instead of the
/// entity so the HTTP contract is not coupled to the persistence model.
/// </summary>
public record MarcaAutoResponse(int Id, string Nombre);
