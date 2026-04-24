using LaptopStore.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaptopStore.Services.Implements
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IDistributedCache distributedCache,ILogger<CacheService> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            // [CacheService] : Đọc dữ liệu JSON từ Redis theo key, sau đó deserialize về kiểu T.
            var cachedData = await _distributedCache.GetStringAsync(key);
            if (cachedData == null) 
            {
                _logger.LogInformation("[CacheService] : Cache miss với key = {CacheKey}.", key);
                return default;
            }
            _logger.LogInformation("[CacheService] : Cache hit với key = {CacheKey}.", key);
            return JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task RemoveAsync(string key)
        {
            // [CacheService] : Xóa cache khi dữ liệu gốc trong DB thay đổi để tránh trả dữ liệu cũ.
            await _distributedCache.RemoveAsync(key);
            _logger.LogInformation("[CacheService] : Đã xóa cache với key = {CacheKey}.", key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            // [CacheService] : Serialize object thành JSON để lưu Redis.
            var jsonData = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),
            };
            await _distributedCache.SetStringAsync(key,jsonData, options);
            _logger.LogInformation(
                "[CacheService] : Đã ghi cache với key = {CacheKey}, expiration = {ExpirationMinutes} phút.",
                key,
                (expiration ?? TimeSpan.FromMinutes(5)).TotalMinutes);
        }
    }
}
