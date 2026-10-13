using Captcha.Store.Models;

namespace Captcha.Store;

/*
 * REDIS SCHEMA  (works unchanged on Valkey)
 * ---------------------------------------------------------------------------------------------
 * Key      cap:v1:ch:{id}                  {id} = 128-bit random, base64url (22 chars)
 * Type     HASH
 * TTL      Captcha:TtlSeconds (default 120). Set in the same MULTI/EXEC as the write, so a crash
 *          can never leave a key without an expiry.
 * Fields   mac      32 raw bytes  HMAC-SHA256(macKey, id \n purpose \n normalisedAnswer)
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
    Task PutAsync(string id, byte[] mac, string purpose, int attempts, TimeSpan ttl);
    Task<ConsumedChallenge?> ConsumeAttemptAsync(string id, string purpose);
    Task<bool> TryDeleteAsync(string id);
}