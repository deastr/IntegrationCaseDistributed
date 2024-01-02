using Client;
using System.Diagnostics;

var sw1 = Stopwatch.StartNew();

int iterationCount = 1_00;
List<long> responseTimes = new(iterationCount);

await Parallel.ForAsync(1, iterationCount, async (i, ct) =>
{
    var sw2 = Stopwatch.StartNew();
    Console.WriteLine($"Sending {i} / {iterationCount}");
    var t1 = ItemIntegrationServiceClient.SaveItem(new(Content: "a"));
    var t2 = ItemIntegrationServiceClient.SaveItem(new(Content: "b"));
    var t3 = ItemIntegrationServiceClient.SaveItem(new(Content: "c"));
    await Task.WhenAll(t1, t2, t3);
    sw2.Stop();
    responseTimes.Add(sw2.ElapsedMilliseconds);
});

sw1.Stop();
Console.WriteLine($"Done. Elapsed time: {(int)sw1.ElapsedMilliseconds}ms, average response time: {(int)responseTimes.Average()}ms.");

var items = await ItemIntegrationServiceClient.GetAllItems();
Console.WriteLine($"Item count: {items.Count}");

Console.ReadKey();