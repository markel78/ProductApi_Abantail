using ProductApi.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductResponseDto>> GetAllAsync(int page, int pageSize, string? nameFilter, string? sortBy, bool sortDescending, CancellationToken ct = default);
        Task<ProductResponseDto> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
        Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
        Task<ProductResponseDto> PatchAsync(int id, PatchProductDto dto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
