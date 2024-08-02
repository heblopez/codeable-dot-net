namespace CachedInventory;

using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

public class EventProcessingService(RedisEventStore eventStore, IWarehouseStockSystemClient legacyClient, UpdateBatcher updateBatcher)
  : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var stockEvent = await eventStore.GetNextStockEventAsync();

      if (stockEvent != null)
      {
        try
        {
          // _ = Task.Run(() => legacyClient.UpdateStock(stockEvent.ProductId, stockEvent.Amount));
          updateBatcher.AddUpdateAsync(stockEvent);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error updating stock in legacy system: {ex.Message}");
        }
      }
      else
      {
        await Task.Delay(1000, stoppingToken);
      }
    }
  }
}

