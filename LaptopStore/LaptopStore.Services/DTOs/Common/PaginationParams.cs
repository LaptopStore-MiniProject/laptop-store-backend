using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaptopStore.Services.DTOs.Common
{
    // [PaginationParams] : Class cha chứa các thuộc tính phân trang cơ bản. Các module khác có thể kế thừa lại để tránh lặp code.
    public class PaginationParams
    {
        // [PaginationParams] : Trang hiện tại, mặc định là 1. Nếu client truyền nhỏ hơn 1 thì ép về 1 để tránh skip âm.
        private int _pageIndex = 1;
        public int PageIndex 
        {
            get => _pageIndex;
            set => _pageIndex = (value < 1) ? 1 : value;
        }
        // [PaginationParams] : Số phần tử mỗi trang. Mặc định là 8, tối đa là 50 để tránh query
        private int _pageSize = 8;
        // [PaginationParams] : Số phần tử mỗi trang. Mặc định là 8, tối đa là 50 để tránh query quá nặng.
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1)
                {
                    _pageSize = 8;
                }
                else if (value > 50)
                {
                    _pageSize = 50;
                }
                else 
                {
                    _pageSize = value;
                }
            }
        }
    }
    // [PagedResultDto] : Class bọc kết quả trả về dùng chung cho các API có phân trang.
    public class PagedResultDto<T> 
    {
        // [PagedResultDto] : Danh sách dữ liệu của trang hiện tại.
        public List<T> Items { get; set; } = new List<T>();
        // [PagedResultDto] : Tổng số bản ghi thỏa điều kiện filter/search.
        public int TotalRecords { get; set; }
        // [PagedResultDto] : Trang hiện tại.
        public int PageIndex { get; set; }
        // [PagedResultDto] : Kích thước mỗi trang.
        public int PageSize { get; set; }
        // [PagedResultDto] : Tổng số trang để frontend render pagination control.
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        // [PagedResultDto] : Dùng để frontend biết có thể bấm nút Previous hay không.
        public bool HasPreviousPage => PageIndex > 1;
        // [PagedResultDto] : Dùng để frontend biết có thể bấm nút Next hay không.
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
