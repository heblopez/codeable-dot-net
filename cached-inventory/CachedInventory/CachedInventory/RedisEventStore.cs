namespace CachedInventory;

using System.Text.Json;
using StackExchange.Redis;

public class RedisEventStore
{
  private readonly IDatabase database;

  public RedisEventStore(ConnectionMultiplexer redis)
  {
    database = redis.GetDatabase();
  }

  public void AddStockEvent(StockEvent stockEvent)
  {
    var serializedEvent = JsonSerializer.Serialize(stockEvent);
    database.ListRightPush("stock_events", serializedEvent);
  }

  public async Task<StockEvent?> GetNextStockEventAsync()
  {
    var serializedEvent = await database.ListLeftPopAsync("stock_events");
    if (!serializedEvent.HasValue)
    {
      return null;
    }
    try
    {
      return JsonSerializer.Deserialize<StockEvent>(serializedEvent!);
    }
    catch (JsonException)
    {
      Console.WriteLine("Error deserializando el evento de stock.");
      return null;
    }
  }
}
