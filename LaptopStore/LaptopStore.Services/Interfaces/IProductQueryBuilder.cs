using LaptopStore.Repositories.Entities;
using LaptopStore.Services.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.Interfaces
{
    public interface IProductQueryBuilder
    {
        Expression<Func<Product, bool>> BuildFilter(ProductQueryParametersDto query);
        Func<IQueryable<Product>, IOrderedQueryable<Product>> BuildOrderBy(string? sortBy);
        Func<IQueryable<Product>, IQueryable<Product>> BuildInclude();
    }
}
