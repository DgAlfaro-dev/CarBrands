using System.ComponentModel.DataAnnotations;

namespace PruebaBackend.Api.Dtos;

/// <summary>Input contract used to create or update a car brand.</summary>
public sealed class MarcaAutoRequest
{
    /// <summary>Brand name; required and limited to 100 characters.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;
}
