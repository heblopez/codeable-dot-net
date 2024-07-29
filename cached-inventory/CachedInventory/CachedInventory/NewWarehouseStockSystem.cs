namespace CachedInventory;

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

public class NewWarehouseStockSystem
{
  private readonly MemoryCache stockCache = new(new MemoryCacheOptions());
  private readonly ConcurrentDictionary<int, SemaphoreSlim> locks = new();
  private readonly IWarehouseStockSystemClient legacySystemClient;

  public NewWarehouseStockSystem(IWarehouseStockSystemClient client)
  {
    legacySystemClient = client;
  }

  public async Task<int> GetStock(int productId)
  {
    if (!stockCache.TryGetValue(productId, out int stock))
    {
      stock = await legacySystemClient.GetStock(productId);
      stockCache.Set(productId, stock);
      locks.TryAdd(productId, new SemaphoreSlim(1, 1));
    }

    return stock;
  }

  public async Task UpdateStock(int productId, int newAmount)
  {
    var semaphore = locks.GetOrAdd(productId, new SemaphoreSlim(1, 1));
    await semaphore.WaitAsync();

    try
    {
      stockCache.Set(productId, newAmount);
      _ = Task.Run(() => legacySystemClient.UpdateStock(productId, newAmount));
    }
    finally
    {
      semaphore.Release();
    }
  }
}
