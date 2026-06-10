using StackExchange.Redis;

namespace Rating.BusinessLayer.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            try
            {
                _db = redis.GetDatabase();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error instanciando redis cache: " +ex.Message);
            }
        }

        public async Task<double?> GetAverageAsync(Guid productoId)
        {
            if (_db is null) return 0;
            var value = await _db.StringGetAsync($"avg:{productoId}");
            return value.HasValue ? double.Parse(value) : null;
        }

        public async Task SetAverageAsync(Guid productoId, double average)
        {
            if (_db != null)
            {
                await _db.StringSetAsync($"avg:{productoId}", average, TimeSpan.FromMinutes(10));
            }
        }
    }
}