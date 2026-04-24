using LaptopStore.Repositories.Context;
using LaptopStore.Repositories.Entities;
using LaptopStore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;

namespace LaptopStore.Repositories.Implements
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;

        // Biến để lưu trữ transaction hiện tại
        private IDbContextTransaction? _currentTransaction;

        // Khai báo một biến private để lưu trữ ngầm
        private IGenericRepository<Product>? _products;
        private IGenericRepository<Category>? _categories;
        private IGenericRepository<Order>? _orders;
        private IGenericRepository<OrderDetail>? _orderDetail;
        private IGenericRepository<CartItem>? _cartItems;
        private IGenericRepository<Cart>? _carts;
        private IGenericRepository<Brand>? _brands;
        private IGenericRepository<ProductImage>? _productImages;
        private IGenericRepository<Role>? _roles;
        private IGenericRepository<User>? _users;
        private IGenericRepository<RefreshToken>? _refreshTokens;
        public UnitOfWork(LaptopStoreDbContext context,ILogger<UnitOfWork> logger) 
        {
            _context = context;
            _logger = logger;
        }
        // [UnitOfWork] : Khởi tạo các Repository (Lazy loading/hoặc khởi tạo trực tiếp). Ở đây khởi tạo trực tiếp cho gọn.
        //Lazy Initialization(Khởi tạo trễ).
        public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);

        public IGenericRepository<Category> Categories => _categories ??= new GenericRepository<Category>(_context);

        public IGenericRepository<Order> Orders => _orders ??= new GenericRepository<Order>(_context);

        public IGenericRepository<CartItem> CartItems => _cartItems ??= new GenericRepository<CartItem>(_context);

        public IGenericRepository<Cart> Carts => _carts ??= new GenericRepository<Cart>(_context);

        public IGenericRepository<Brand> Brands => _brands ??= new GenericRepository<Brand>(_context);

        public IGenericRepository<OrderDetail> OrderDetails => _orderDetail ??= new GenericRepository<OrderDetail>(_context);

        public IGenericRepository<ProductImage> ProductImages => _productImages ??= new GenericRepository<ProductImage>(_context);

        public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);

        public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
        public IGenericRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new GenericRepository<RefreshToken>(_context);

        public async Task BeginTransactionAsync()
        {
            // [UnitOfWork] : Kiểm tra để tránh việc mở nhiều transaction lồng nhau không cần thiết trên cùng một context.
            if (_currentTransaction != null)
            {
                _logger.LogWarning("[UnitOfWork] : Một transaction đã được khởi tạo trước đó. Bỏ qua yêu cầu tạo mới.");
                return;
            }
            _currentTransaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("[UnitOfWork] : Đã khởi tạo Transaction mới.");
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                if (_currentTransaction == null)
                {
                    throw new InvalidOperationException("Không có transaction nào được mở để commit.");
                }
                // [UnitOfWork] : Gọi SaveChangesAsync một lần cuối cùng để đảm bảo mọi Tracking lọt lưới đều được đẩy xuống DB trước khi Commit.
                await _context.SaveChangesAsync();
                await _currentTransaction.CommitAsync();
                _logger.LogInformation("[UnitOfWork] : Đã Commit transaction thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UnitOfWork] : Lỗi khi commit transaction. Tự động tiến hành rollback bảo vệ dữ liệu.");
                await RollbackTransactionAsync();
                throw; // Ném lỗi ra ngoài cho Service hoặc Controller bắt (để trả về HTTP 500)
            }
            finally 
            {
                if(_currentTransaction != null) 
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            // [UnitOfWork] : Dọn dẹp context khi UnitOfWork bị hủy bởi Dependency Injection (Scoped).
            _context.Dispose();
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_currentTransaction != null) 
                {
                    await _currentTransaction.RollbackAsync();
                    _logger.LogInformation("[UnitOfWork] : Đã Rollback transaction an toàn.");
                }
            }
            finally
            {
                // [UnitOfWork] : Tương tự như Commit, Rollback xong cũng phải dọn dẹp bộ nhớ.
                if(_currentTransaction != null) 
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
