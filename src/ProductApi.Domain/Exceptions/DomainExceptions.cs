namespace ProductApi.Domain.Exceptions;

public class ProductNotFoundException : Exception
{
    public int ProductId { get; }

    public ProductNotFoundException(int id)
        : base($"Product with id '{id}' was not found.")
    {
        ProductId = id;
    }
}

public class ProductConcurrencyException : Exception
{
    public ProductConcurrencyException(int id)
        : base($"Concurrency conflict detected for product '{id}'. The resource was modified by another request.")
    {
    }
}

public class DuplicateProductException : Exception
{
    public DuplicateProductException(string name)
        : base($"A product with the name '{name}' already exists.")
    {
    }
}
