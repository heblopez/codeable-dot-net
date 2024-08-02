namespace CachedInventory;

using System.Collections.Concurrent;

public class NewWarehouseStockSystem(
  IWarehouseStockSystemClient legacyClient,
  RedisCache redisCache,
  RedisEventStore eventStore)
{
  public async Task<int> GetStock(int productId)
  {
    var stock = await redisCache.GetStockAsync(productId);
    if (stock == null)
    {
      var stockFromLegacy = await legacyClient.GetStock(productId);
      await redisCache.SetStockAsync(productId, stockFromLegacy);
    }

    return stock.Value;
  }

  public async Task<int> UpdateStock(int productId, int newAmount)
  {
    await redisCache.SetStockAsync(productId, newAmount);
    var stockEvent = new StockEvent { ProductId = productId, Amount = newAmount, Timestamp = DateTime.Now };
    eventStore.AddStockEvent(stockEvent);
    return newAmount;
  }
}
