// ReSharper disable ClassNeverInstantiated.Global

namespace CachedInventory.Tests;

public class SingleRetrieval
{
  [Fact(DisplayName = "retirar un producto")]
  public static async Task Test() => await TestApiPerformance.Test(1, [3], false, 2_000);
}

public class FourRetrievalsInParallel
{
  [Fact(DisplayName = "retirar cuatro productos en paralelo")]
  public static async Task Test() => await TestApiPerformance.Test(2, [1, 2, 3, 4], true, 1_000);
}

public class FourRetrievalsSequentially
{
  [Fact(DisplayName = "retirar cuatro productos secuencialmente")]
  public static async Task Test() => await TestApiPerformance.Test(3, [1, 2, 3, 4], false, 1_000);
}

public class SevenRetrievalsInParallel
{
  [Fact(DisplayName = "retirar siete productos en paralelo")]
  public static async Task Test() => await TestApiPerformance.Test(4, [1, 2, 3, 4, 5, 6, 7], true, 500);
}

public class SevenRetrievalsSequentially
{
  [Fact(DisplayName = "retirar siete productos secuencialmente")]
  public static async Task Test() => await TestApiPerformance.Test(5, [1, 2, 3, 4, 5, 6, 7], false, 500);
}

public class SingleRetrievalWithoutEnoughStock
{
  [Fact(DisplayName = "retirar un producto sin suficiente stock")]
  public static async Task Test() => await TestApiPerformance.Test(6, [7], false, 2_000, 5);
}

public class SevenRetrievalsWithoutEnoughSequentially
{
  [Fact(DisplayName = "7 + 1 retiros sin suficiente stock secuencialmente")]
  public static async Task Test() => await TestApiPerformance.Test(7, [1, 2, 3, 4, 5, 6, 7], false, 1_000, 3);
}

public class SevenRetrievalsWithoutEnoughStockinParallel
{
  [Fact(DisplayName = "7 + 1 retiros sin suficiente stock en paralelo")]
  public static async Task Test() => await TestApiPerformance.Test(7, [1, 2, 3, 4, 5, 6, 7], true, 1_000, 3);
}

internal static class TestApiPerformance
{
  internal static async Task Test(int productId, int[] retrievals, bool isParallel, long expectedPerformance, int excessAmount = 0)
  {
    await using var setup = await TestSetup.Initialize();
    await setup.Restock(productId, retrievals.Sum());
    await setup.VerifyStockFromFile(productId, retrievals.Sum());
    var tasks = new List<Task>();
    
    if (excessAmount > 0)
    {
      retrievals = retrievals.Append(excessAmount).ToArray();
    }
    
    for (var i = 0; i < retrievals.Length; i++)
    {
      var withError = (i == (retrievals.Length - 1) && excessAmount > 0);
      var task = setup.Retrieve(productId, retrievals[i], withError);
      
      if (!isParallel)
      {
        await task;
      }

      tasks.Add(task);
    }

    await Task.WhenAll(tasks);
    var finalStock = await setup.GetStock(productId);
    Assert.True(finalStock == 0, $"El stock final no es 0, sino {finalStock}.");
    Assert.True(
      setup.AverageRequestDuration < expectedPerformance,
      $"Duración promedio: {setup.AverageRequestDuration}ms, se esperaba un máximo de {expectedPerformance}ms.");
    await setup.VerifyStockFromFile(productId, 0);
  }
}
