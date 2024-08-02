namespace CachedInventory;

public class StockEvent
{
  public int ProductId { get; set; }
  public int Amount { get; set; }
  public DateTime Timestamp { get; set; }
}
