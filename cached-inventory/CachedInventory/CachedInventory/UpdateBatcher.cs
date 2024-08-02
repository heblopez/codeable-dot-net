namespace CachedInventory;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class UpdateBatcher
{
  private readonly ConcurrentDictionary<int, StockEvent> _updates = new();
  private readonly TimeSpan _debounceTime;
  private readonly Func<int, StockEvent, Task> _updateAction;
  private readonly Timer _timer;
  private static readonly SemaphoreSlim Semaphore = new(1, 1);

  public UpdateBatcher(TimeSpan debounceTime, Func<int, StockEvent, Task> updateAction)
  {
    _debounceTime = debounceTime;
    _updateAction = updateAction;
    _timer = new Timer(FlushUpdates, null, _debounceTime, _debounceTime);
  }

  public async Task AddUpdateAsync(StockEvent stockEvent)
  {
    await Semaphore.WaitAsync();
    try
    {
      _updates[stockEvent.ProductId] = stockEvent;
    }
    finally
    {
      Semaphore.Release();
    }
  }

  private async void FlushUpdates(object? state)
  {
    await Semaphore.WaitAsync();
    try
    {
      var updates = _updates.ToArray();
      _updates.Clear();

      foreach (var update in updates)
      {
        await _updateAction(update.Key, update.Value);
      }
    }
    finally
    {
      Semaphore.Release();
    }
  }
}
