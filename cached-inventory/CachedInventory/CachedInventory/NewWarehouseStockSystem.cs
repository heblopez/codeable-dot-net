namespace CachedInventory;

using System.Collections.Concurrent;

public class NewWarehouseStockSystem
{
  private readonly ConcurrentDictionary<int, int> stockCache = new();
  private readonly ConcurrentDictionary<int, SemaphoreSlim> locks = new();
  private readonly IWarehouseStockSystemClient legacySystemClient;

  public NewWarehouseStockSystem(IWarehouseStockSystemClient client)
  {
    legacySystemClient = client;
  }

  public async Task<int> GetStock(int productId)
  {
    if (!stockCache.TryGetValue(productId, out var stock))
    {
      stock = await legacySystemClient.GetStock(productId);
      stockCache[productId] = stock;
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
      stockCache[productId] = newAmount;
      _ = Task.Run(() => legacySystemClient.UpdateStock(productId, newAmount));
    }
    finally
    {
      semaphore.Release();
    }
  }
}
