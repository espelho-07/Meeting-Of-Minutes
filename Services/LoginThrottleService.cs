using System.Collections.Concurrent;

namespace Meeting_Of_Minutes.Services
{
    public static class LoginThrottleService
    {
        private static readonly ConcurrentDictionary<string, AttemptWindow> Attempts = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxFailures = 5;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        public static bool IsBlocked(string key, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!Attempts.TryGetValue(key, out AttemptWindow? attempt))
            {
                return false;
            }

            if (attempt.FirstFailureAtUtc.Add(Window) <= DateTime.UtcNow)
            {
                Attempts.TryRemove(key, out _);
                return false;
            }

            if (attempt.FailureCount < MaxFailures)
            {
                return false;
            }

            retryAfter = attempt.FirstFailureAtUtc.Add(Window) - DateTime.UtcNow;
            return retryAfter > TimeSpan.Zero;
        }

        public static void RegisterFailure(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            Attempts.AddOrUpdate(
                key,
                _ => new AttemptWindow { FailureCount = 1, FirstFailureAtUtc = DateTime.UtcNow },
                (_, existing) =>
                {
                    if (existing.FirstFailureAtUtc.Add(Window) <= DateTime.UtcNow)
                    {
                        existing.FirstFailureAtUtc = DateTime.UtcNow;
                        existing.FailureCount = 1;
                    }
                    else
                    {
                        existing.FailureCount++;
                    }

                    return existing;
                });
        }

        public static void Clear(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                Attempts.TryRemove(key, out _);
            }
        }

        private sealed class AttemptWindow
        {
            public int FailureCount { get; set; }
            public DateTime FirstFailureAtUtc { get; set; }
        }
    }
}
