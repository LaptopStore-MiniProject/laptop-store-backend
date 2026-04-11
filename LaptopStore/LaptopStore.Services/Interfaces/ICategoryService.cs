using LaptopStore.Repositories.Entities;
using LaptopStore.Services.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
        Task<CategoryResponseDto> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto);
        Task<bool> UpdateAsync(int id, CategoryRequestDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
