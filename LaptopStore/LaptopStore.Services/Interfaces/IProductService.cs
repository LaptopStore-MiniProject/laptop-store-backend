using LaptopStore.Services.DTOs.Common;
using LaptopStore.Services.DTOs.Product;

namespace LaptopStore.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllAsync();
        Task<ProductResponseDto> GetByIdAsync(int id);
        Task<ProductResponseDto> CreateAsync(ProductRequestDto dto);
        Task<bool> UpdateAsync(int id, ProductRequestDto dto);
        Task<bool> DeleteAsync(int id);
        // [IProductService] : Lấy danh sách sản phẩm có hỗ trợ search, filter, sort và paging.
        Task<PagedResultDto<ProductResponseDto>> GetProductsAsync(ProductQueryParametersDto query);
    }
}
