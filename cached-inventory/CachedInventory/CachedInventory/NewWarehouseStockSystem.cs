namespace CachedInventory;

using System.Collections.Concurrent;

public class NewWarehouseStockSystem: IDisposable
{
  private readonly ConcurrentDictionary<int, int> stockCache = new();
  private readonly ConcurrentDictionary<int, int> pendingUpdatesToLegacy = new();
  private readonly ConcurrentDictionary<int, SemaphoreSlim> locks = new();
  private readonly IWarehouseStockSystemClient legacySystemClient;
  private readonly Timer inactivityTimer;
  private readonly TimeSpan inactivityTimeout = TimeSpan.FromMilliseconds(500);
  private readonly CancellationTokenSource cancellationTokenSource = new();

  public NewWarehouseStockSystem(IWarehouseStockSystemClient client)
  {
    legacySystemClient = client;
    inactivityTimer = new Timer(
      OnInactivity,
      null,
      Timeout.InfiniteTimeSpan,
      Timeout.InfiniteTimeSpan
    );
  }

  public async Task<int> GetStock(int productId)
  {
    if (stockCache.TryGetValue(productId, out var stock))
    {
      inactivityTimer.Change(inactivityTimeout, Timeout.InfiniteTimeSpan);
      return stock;
    }

    stock = await legacySystemClient.GetStock(productId);
    stockCache[productId] = stock;

    inactivityTimer.Change(inactivityTimeout, Timeout.InfiniteTimeSpan);
    return stock;
  }

  public async Task<int> UpdateStock(int productId, int newAmount)
  {
    var semaphore = locks.GetOrAdd(productId, new SemaphoreSlim(1, 1));
    await semaphore.WaitAsync();

    try
    {
      stockCache[productId] = newAmount;
      pendingUpdatesToLegacy[productId] = newAmount;
      inactivityTimer.Change(inactivityTimeout, Timeout.InfiniteTimeSpan);
      return newAmount;
    }
    finally
    {
      semaphore.Release();
    }
  }

  private void OnInactivity(object? state) =>
    Task.Run(
      async () =>
      {
        foreach (var entry in pendingUpdatesToLegacy)
        {
          var semaphore = locks.GetOrAdd(entry.Key, new SemaphoreSlim(1, 1));
          await semaphore.WaitAsync();
          try
          {
            await legacySystemClient.UpdateStock(entry.Key, entry.Value);
            pendingUpdatesToLegacy.TryRemove(entry.Key, out _);
          }
          catch (Exception e)
          {
            Console.WriteLine($"Error updating stock of product {entry.Key} to legacy system: {e.Message}");
          }
          finally
          {
            semaphore.Release();
          }
        }
      }
    );
  
  public void Dispose()
  {
    inactivityTimer.Dispose();
    cancellationTokenSource.Cancel();
    cancellationTokenSource.Dispose();
  }
}
