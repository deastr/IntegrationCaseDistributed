using Backend;
using IdGen.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Primitives;
using Service;
using Shared;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

//Twitter Snowflake-like id generator.
//Normally you need to use something like machine name to create a unique generator id per service.
int serviceId = Random.Shared.Next(1, 512);
string service = $"service{serviceId}";
builder.Services.AddIdGen(serviceId);

builder.Services.AddSingleton(typeof(DistributedLockProvider));

builder.Services.AddSingleton<IItemIntegrationBackend, ItemIntegrationBackend>();

builder.Services.AddStackExchangeRedisCache(config =>
{
    config.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");
});

var app = builder.Build();

//Clear cache
var c = app.Services.GetRequiredService<IDistributedCache>();
await c.RemoveAsync("case_idempotencykeys");
await c.RemoveAsync("case_items");

//Idempotency middleware
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("Idempotency-Key", out StringValues value))
    {
        const string cacheKey = "case_idempotencykeys";
        var cache = context.RequestServices.GetRequiredService<IDistributedCache>();

        var lockProvider = context.RequestServices.GetRequiredService<DistributedLockProvider>();

        List<string> list;

        if (!await lockProvider.AcquireIdempotencyLockAsync(service))
            throw new TimeoutException("Idempotency key control lock acquisition timed out.");

        var listCached = await cache.GetAsync(cacheKey);

        await lockProvider.ReleaseIdempotencyLockAsync(service);

        if (listCached is null)
            list = [];
        else
            list = JsonSerializer.Deserialize<List<string>>(listCached)!;

        string idempotencyValue = value!;
        if (list.Contains(idempotencyValue))
        {
            //This is a resent request
            //https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }
        else
        {
            await next(context);

            listCached = await cache.GetAsync(cacheKey);

            if(listCached is null)
                list = [];
            else
                list = JsonSerializer.Deserialize<List<string>>(listCached)!;
            
            list.Add(idempotencyValue);

            await cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(list));
        }
    }
    else
    {
        await next(context);
    }
});

app.MapPut("/saveItem", async ([FromServices] IItemIntegrationBackend itemIntegrationBackend, ItemRequest itemRequest) =>
{
    return await itemIntegrationBackend.SaveItem(itemRequest);
});

app.MapGet("/getAllItems", async ([FromServices] IItemIntegrationBackend itemIntegrationBackend) =>
{
    return await itemIntegrationBackend.GetAllItems();
});

app.Run();