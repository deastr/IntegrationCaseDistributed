using StackExchange.Redis;

namespace Service;

public sealed class DistributedLockProvider
{
    readonly IDatabaseAsync database;
    const string keyIdempotency = "case_lock_idempotency";
    readonly TimeSpan lockExpiry = TimeSpan.FromSeconds(3);
    readonly TimeSpan lockAcquisionTimeout = TimeSpan.FromSeconds(30);

    public DistributedLockProvider(IConfiguration config)
    {
        var option = new ConfigurationOptions { Ssl = false };
        option.EndPoints.Add(config.GetValue<string>("Redis:ConnectionString")!);

        var connect = ConnectionMultiplexer.Connect(option);
        database = connect.GetDatabase();
    }

    public Task<bool> AcquireIdempotencyLockAsync(string service)
    {
        return AcquireLockAsync(keyIdempotency, service);
    }

    public Task<bool> ReleaseIdempotencyLockAsync(string service)
    {
        return database.LockReleaseAsync(keyIdempotency, service);
    }

    private async Task<bool> AcquireLockAsync(string key, string service)
    {
        bool lockTaken = false;
        DateTime checkStartTime = DateTime.UtcNow;
        //Try to acquire lock until timeout
        while (!lockTaken && checkStartTime.Add(lockAcquisionTimeout) > DateTime.UtcNow)
        {
            lockTaken = await database.LockTakeAsync(key, service, lockExpiry);
            if (lockTaken)
                break;
            else
                //wait for the next retry
                await Task.Delay(100);
        }
        return lockTaken;
    }
}
