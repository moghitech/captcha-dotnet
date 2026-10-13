using Captcha.Services.Store.Models;
using StackExchange.Redis;

namespace Captcha.Services.Store;

public sealed class RedisChallengeStore(IConnectionMultiplexer mux) : IChallengeStore
{
    private static string Key(string id) => $"cap:v1:ch:{id}";

    // KEYS[1] = challenge key, ARGV[1] = purpose requested by the caller.
    // Returns nil (not found / purpose mismatch / attempts exhausted) or { Mac, attemptsLeft }.
    private const string ConsumeLua =
        """
        local v = redis.call('HMGET', KEYS[1], 'mac', 'purpose', 'left')
        if not v[1] then return nil end
        if v[2] ~= ARGV[1] then
          redis.call('DEL', KEYS[1])
          return nil
        end
        local left = redis.call('HINCRBY', KEYS[1], 'left', -1)
        if left < 0 then
          redis.call('DEL', KEYS[1])
          return nil
        end
        return { v[1], left }
        """;

    public async Task PutAsync(string id, byte[] mac, string purpose, int attempts, TimeSpan ttl)
    {
        var key = Key(id);
        var tx = mux.GetDatabase().CreateTransaction();
        _ = tx.HashSetAsync(key, hashFields:
        [
            new HashEntry(name: "mac", mac),
            new HashEntry(name: "purpose", purpose),
            new HashEntry(name: "left", attempts)
        ]);
        _ = tx.KeyExpireAsync(key, ttl);

        if (!await tx.ExecuteAsync())
            throw new InvalidOperationException("Redis transaction for new challenge was not committed.");
    }

    public async Task<ConsumedChallenge?> ConsumeAttemptAsync(string id, string purpose)
    {
        var res = await mux.GetDatabase().ScriptEvaluateAsync(ConsumeLua, keys: [Key(id)], [purpose]);

        if (res.IsNull) return null;

        var parts = (RedisResult[])res!;
        return new ConsumedChallenge((byte[])parts[0]!, (int)parts[1]);
    }

    public Task<bool> TryDeleteAsync(string id) => mux.GetDatabase().KeyDeleteAsync(Key(id));
}