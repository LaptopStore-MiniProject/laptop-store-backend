using LaptopStore.Repositories.Context;
using LaptopStore.Repositories.Entities;
using LaptopStore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LaptopStore.Repositories.Implements
{
    // [GenericRepository] : Lớp triển khai các thao tác tương tác với cơ sở dữ liệu dùng chung cho mọi Entity.
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly LaptopStoreDbContext _context;
        internal DbSet<T> _dbset;
        // [GenericRepository] : Tiêm (Inject) ApplicationDbContext vào thông qua Constructor (Dependency Injection).
        public GenericRepository(LaptopStoreDbContext context) 
        {
            _context = context;
            _dbset = _context.Set<T>();
        }
        // [GenericRepository] : Đánh dấu thêm mới 1 thực thể.
        public async Task AddAsync(T entity)
        {
            await _dbset.AddAsync(entity);
        }
        // [GenericRepository] : Đánh dấu thêm mới một tập hợp thực thể.
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbset.AddRangeAsync(entities);
        }
        // [GenericRepository] : Lấy danh sách dữ liệu, hỗ trợ lọc, sắp xếp và join các bảng liên quan.
        public async Task<List<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string? includeProperties = null,bool tracked = true)
        {
            // [GenericRepository] : Khởi tạo query từ DbSet.
            IQueryable<T> query = _dbset;


            // <-- Thêm khối if này để tối ưu RAM
            if (!tracked)
            {
                query = query.AsNoTracking();
            }

            // [GenericRepository] : Nếu có điều kiện lọc (filter), áp dụng ngay vào query bằng mệnh đề Where.
            if (filter != null)
            {
                query = query.Where(filter);
            }
            // [GenericRepository] : Tách chuỗi includeProperties (phân cách bằng dấu phẩy) và ghim vào query để Join bảng.
            if(!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp.Trim());
                }
            }
            // [GenericRepository] : Nếu có yêu cầu sắp xếp, áp dụng sắp xếp. Nếu không, trả về danh sách dạng List.
            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            return await query.ToListAsync();
        }
        // [GenericRepository] : Tìm kiếm 1 bản ghi cụ thể, thường dùng để check tồn tại hoặc lấy chi tiết.
        public async Task<T?> GetAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = true)
        {
            IQueryable<T> query = _dbset;
            // [GenericRepository] : Nếu chỉ đọc dữ liệu (không update), tắt Tracking để tối ưu hiệu năng bộ nhớ.
            if (!tracked)
            {
                query = query.AsNoTracking();
            }

            query = query.Where(filter);

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp.Trim());
                }
            }
            // [GenericRepository] : Thực thi câu lệnh SQL và lấy bản ghi đầu tiên khớp điều kiện (hoặc null).
            return await query.FirstOrDefaultAsync();
        }

        public async Task<(List<T> Items, int TotalRecords)> GetPagedAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int pageIndex = 1, int pageSize = 8, bool tracked = true)
        {
            IQueryable<T> query = _dbset;

            if (!tracked)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            if (include != null)
                query = include(query);

            // [GenericRepository] : Đếm tổng số bản ghi thỏa mãn điều kiện filter (chưa cắt trang) để tính TotalPages.
            int totalRecords = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);

            // [GenericRepository] : Áp dụng Paging: Bỏ qua các records của trang trước (Skip) và lấy số lượng của trang hiện tại (Take).
            var items = await query.Skip((pageIndex - 1) * pageSize) //(1 - 1) * 8 = 0  // 2. (2 - 1) * 8 = 8
                                   .Take(pageSize) // Lấy từ 1 đến 8 // lấy từ 9 đến 16
                                   .ToListAsync();

            return (items, totalRecords);
        }

        // [GenericRepository] : Đánh dấu xóa một thực thể.
        public void Remove(T entity)
        {
             _dbset.Remove(entity);
        }
        // [GenericRepository] : Đánh dấu xóa một tập hợp thực thể.
        public void RemoveRange(IEnumerable<T> entities)
        {
             _dbset.RemoveRange(entities);
        }
        // [GenericRepository] : Đánh dấu cập nhật một thực thể. (EF Core không có UpdateAsync)
        public void Update(T entity)
        {
            _dbset.Update(entity);
        }


    }
}
