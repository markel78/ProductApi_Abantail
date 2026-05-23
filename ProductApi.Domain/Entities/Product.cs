using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Domain.Entities
{
    public sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }
}
