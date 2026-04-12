using AutoMapper;
using LaptopStore.Repositories.Entities;
using LaptopStore.Services.DTOs.Category;
using LaptopStore.Services.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.Mappings
{
    // [AutoMapperProfile] : Lớp cấu hình quy tắc chuyển đổi(Mapping) giữa Entities và DTOs.
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() 
        {

            // CategoryRequestDto => Entity Category
            CreateMap<CategoryRequestDto, Category>();
            // [AutoMapperProfile] : Cấu hình ánh xạ từ Entity Category sang CategoryResponseDto.
            CreateMap<Category, CategoryResponseDto>();

            CreateMap<ProductRequestDto, Product>();

            CreateMap<ProductImage, ProductImageDto>();

            CreateMap<Product, ProductResponseDto>()
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand.Name))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));


        }
    }
}
