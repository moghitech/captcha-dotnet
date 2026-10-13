using Captcha.Services.Store.Models;

namespace Captcha.Services.Store;

/*
 * REDIS SCHEMA  (works unchanged on Valkey)
 * ---------------------------------------------------------------------------------------------
 * Key      cap:v1:ch:{id}                  {id} = 128-bit random, base64url (22 chars)
 * Type     HASH
 * TTL      Captcha:TtlSeconds (default 120). Set in the same MULTI/EXEC as the write, so a crash
 *          can never leave a key without an expiry.
 * Fields Mac 32 raw bytes  HMAC-SHA256(macKey, id \n purpose \n normalisedAnswer)
 *          purpose  string        e.g. "login"; a challenge is only valid for the purpose it was issued for
 *          left     integer       attempts remaining; decremented BEFORE the comparison
 *
 * The plain answer is never stored. Read access to Redis alone is not enough to recover answers:
 * the MAC key lives only in the service's configuration.
 *
 * Lifecycle
 *   PutAsync              HSET + EXPIRE, atomic (MULTI/EXEC).
 *   ConsumeAttemptAsync   one Lua script, atomic: check purpose -> reserve an attempt -> return the MAC.
 *                         The comparison happens in .NET with a constant-time check.
 *   TryDeleteAsync        DEL after a correct answer. Of N concurrent correct verifications exactly
 *                         one DEL returns true, so the challenge can be accepted only once.
 *
 * Attempt accounting (MaxAttempts = 3): calls 1..3 get left = 2,1,0 and are compared;
 * call 4 sees left = -1, deletes the key and is treated as expired_or_used.
 * Reserving the attempt before comparing means parallel guesses cannot exceed the limit.
 *
 * Capacity: roughly 150-250 bytes per live challenge. Live keys ~ issue rate x TTL
 * (50 issues/s x 120 s = 6,000 keys, a few MB).
 *
 * Suggested ACL for the service user: ~cap:v1:* with only the commands used here
 * (EVAL, HSET, HMGET, HINCRBY, EXPIRE, DEL, PING).
 */
public interface IChallengeStore
{
    /// <summary>
    /// Stores a new challenge with the specified identifier, message authentication code, purpose, attempt count, and time-to-live.
    /// </summary>
    /// <param name="id">The unique identifier of the challenge.</param>
    /// <param name="mac">The message authentication code associated with the challenge.</param>
    /// <param name="purpose">The purpose for which the challenge was issued.</param>
    /// <param name="attempts">The number of attempts allowed for the challenge.</param>
    /// <param name="ttl">The duration for which the challenge remains valid.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PutAsync(string id, byte[] mac, string purpose, int attempts, TimeSpan ttl);

    /// <summary>
    /// Consumes a single attempt for the challenge with the specified identifier and purpose.
    /// </summary>
    /// <param name="id">The unique identifier of the challenge.</param>
    /// <param name="purpose">The purpose for which the challenge was issued.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the consumed challenge, or <c>null</c> if the challenge was not found or is no longer valid.</returns>
    Task<ConsumedChallenge?> ConsumeAttemptAsync(string id, string purpose);

    /// <summary>
    /// Attempts to delete the challenge with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the challenge to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the challenge was deleted; otherwise, <c>false</c>.</returns>
    Task<bool> TryDeleteAsync(string id);
}