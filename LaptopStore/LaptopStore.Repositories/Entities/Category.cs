using System;
using System.Collections.Generic;

namespace LaptopStore.Repositories.Entities
{
    // [CategoryEntity] : Danh mục sản phẩm (Gaming, Office). Dùng int vì số lượng ít, cần truy vấn cực nhanh để hiển thị Menu.
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        // [CategoryEntity] : Cờ đánh dấu xóa mềm, mặc định là chưa xóa
        public bool IsDeleted { get; set; } = false;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}