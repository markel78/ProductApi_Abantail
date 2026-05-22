namespace ProductApi.Domain.Entities;

/// <summary>
/// Entidad raíz del dominio. Contiene el RowVersion para control de concurrencia optimista.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    /// <summary>
    /// Versión usada para concurrencia optimista (ETag).
    /// Se incrementa en cada modificación.
    /// </summary>
    public uint RowVersion { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
