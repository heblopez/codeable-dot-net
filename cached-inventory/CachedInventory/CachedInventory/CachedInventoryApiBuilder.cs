namespace CachedInventory;

using Microsoft.AspNetCore.Mvc;

public static class CachedInventoryApiBuilder
{
  public static WebApplication Build(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSingleton<IWarehouseStockSystemClient, WarehouseStockSystemClient>();
    builder.Services.AddSingleton<NewWarehouseStockSystem>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.MapGet(
        "/stock/{productId:int}",
        async ([FromServices] NewWarehouseStockSystem client, int productId) => await client.GetStock(productId))
      .WithName("GetStock")
      .WithOpenApi();

    app.MapPost(
        "/stock/retrieve",
        async ([FromServices] NewWarehouseStockSystem client, [FromBody] RetrieveStockRequest req) =>
        {
          var currentStock = await client.GetStock(req.ProductId);
          if (currentStock < req.Amount)
          {
            return Results.BadRequest("Not enough stock.");
          }
          var newStock = currentStock - req.Amount;
          var result = await client.UpdateStock(req.ProductId, newStock);
          return Results.Ok(new { req.ProductId, Amount = result});
        })
      .WithName("RetrieveStock")
      .WithOpenApi();


    app.MapPost(
        "/stock/restock",
        async ([FromServices] NewWarehouseStockSystem client, [FromBody] RestockRequest req) =>
        {
          var currentStock = await client.GetStock(req.ProductId);
          var newStock = currentStock + req.Amount;
          var result = await client.UpdateStock(req.ProductId, newStock);
          return Results.Ok(new { req.ProductId, Amount = result});
        })
      .WithName("Restock")
      .WithOpenApi();

    return app;
  }
}

public record RetrieveStockRequest(int ProductId, int Amount);

public record RestockRequest(int ProductId, int Amount);
