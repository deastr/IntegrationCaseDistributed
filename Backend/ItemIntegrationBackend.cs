using IdGen;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.Text.Json;

namespace Backend;

public interface IItemIntegrationBackend
{
    Task<Item> SaveItem(ItemRequest itemRequest);
    Task<List<Item>> GetAllItems();
}

public sealed class ItemIntegrationBackend(IdGenerator idGenerator, IDistributedCache cache) : IItemIntegrationBackend
{
    private readonly IdGenerator idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    private readonly IDistributedCache cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private const string cacheKey = "case_items";

    public async Task<Item> SaveItem(ItemRequest itemRequest)
    {
        await Task.Delay(2000);
        
        Item item = new(idGenerator.CreateId(), itemRequest.Content);
        
        var listCached = await cache.GetAsync(cacheKey);
        List<Item> list;

        if (listCached is null)
            list = [];
        else
            list = JsonSerializer.Deserialize<List<Item>>(listCached)!;

        list.Add(item);

        await cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(list));

        return item;
    }

    public async Task<List<Item>> GetAllItems()
    {
        var items = await cache.GetAsync(cacheKey);
        return JsonSerializer.Deserialize<List<Item>>(items)!;
    }
}