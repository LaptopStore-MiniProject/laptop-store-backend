using AutoMapper;
using LaptopStore.Repositories.Entities;
using LaptopStore.Repositories.Interfaces;
using LaptopStore.Services.DTOs.Product;
using LaptopStore.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LaptopStore.Services.Implements
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProductResponseDto> CreateAsync(ProductRequestDto dto)
        {
            _logger.LogInformation($"[ProductService] : Bắt đầu tạo mới sản phẩm: {dto.Name}.");

            var product = _mapper.Map<Product>(dto);
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            var createdProduct = await _unitOfWork.Products.GetAsync(
                p => p.Id == product.Id,
                includeProperties: "Brand,Category,ProductImages",
                tracked: false);

            _logger.LogInformation($"[ProductService] : Tạo thành công sản phẩm mới với Id = {product.Id}.");
            return _mapper.Map<ProductResponseDto>(createdProduct);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation($"[ProductService] : Bắt đầu xóa mềm sản phẩm Id = {id}.");

            var existingProduct = await _unitOfWork.Products.GetAsync(p => p.Id == id);
            if (existingProduct == null)
            {
                _logger.LogWarning($"[ProductService] : Thất bại khi xóa. Không tìm thấy sản phẩm Id = {id}.");
                return false;
            }

            existingProduct.IsDeleted = true;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"[ProductService] : Đã xóa mềm thành công sản phẩm Id = {id}.");
            return true;
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
        {
            _logger.LogInformation("[ProductService] : Bắt đầu lấy danh sách toàn bộ sản phẩm.");

            var products = await _unitOfWork.Products.GetAllAsync(
                includeProperties: "Brand,Category,ProductImages",
                tracked: false);

            _logger.LogInformation($"[ProductService] : Đã lấy thành công {products.Count} sản phẩm.");
            return _mapper.Map<IEnumerable<ProductResponseDto>>(products);
        }

        public async Task<ProductResponseDto> GetByIdAsync(int id)
        {
            _logger.LogInformation($"[ProductService] : Bắt đầu tìm kiếm sản phẩm với Id = {id}.");

            var product = await _unitOfWork.Products.GetAsync(
                p => p.Id == id,
                includeProperties: "Brand,Category,ProductImages",
                tracked: false);

            if (product == null)
            {
                _logger.LogWarning($"[ProductService] : Không tìm thấy sản phẩm với Id = {id}.");
                return null!;
            }

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<bool> UpdateAsync(int id, ProductRequestDto dto)
        {
            _logger.LogInformation($"[ProductService] : Bắt đầu cập nhật sản phẩm Id = {id}.");

            var existingProduct = await _unitOfWork.Products.GetAsync(p => p.Id == id);
            if (existingProduct == null)
            {
                _logger.LogWarning($"[ProductService] : Thất bại khi cập nhật. Không tìm thấy sản phẩm Id = {id}.");
                return false;
            }

            _mapper.Map(dto, existingProduct);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"[ProductService] : Cập nhật thành công sản phẩm Id = {id}.");
            return true;
        }
    }
}
