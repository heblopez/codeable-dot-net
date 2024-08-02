namespace CachedInventory;

using System.Collections.Concurrent;

public class EventStore
{
  private static readonly ConcurrentQueue<StockEvent> Events = new();
  
  public static void AddStockEvent(StockEvent stockEvent) => Events.Enqueue(stockEvent);

  public static bool TryDequeue(out StockEvent stockEvent) => Events.TryDequeue(out stockEvent);

  public static bool HasEvents => !Events.IsEmpty;
}
