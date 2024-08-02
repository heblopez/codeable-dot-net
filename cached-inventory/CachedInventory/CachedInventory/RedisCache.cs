namespace CachedInventory;

using StackExchange.Redis;

public class RedisCache
{
  private readonly IDatabase database;

  public RedisCache(ConnectionMultiplexer connectionMultiplexer)
  {
    database = connectionMultiplexer.GetDatabase();
  }

  public async Task<int?> GetStockAsync(int productId)
  {
    var value = await database.StringGetAsync($"stock:{productId}");
    return (value.IsNullOrEmpty) ? 0 : int.Parse(value);
  }
  
  public async Task SetStockAsync(int productId, int stock)
  {
    await database.StringSetAsync($"stock:{productId}", stock);
  }
}
