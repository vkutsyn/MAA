using System.Diagnostics;

namespace MAA.LoadTests;

/// <summary>
/// Lightweight load test runner for question definitions endpoint.
/// Run from a console app or script that references this file.
/// </summary>
public static class QuestionDefinitionsLoadTest
{
    public static async Task RunAsync(
        string baseUrl,
        string stateCode,
        string programCode,
        int totalRequests = 500,
        int maxConcurrency = 25,
        CancellationToken cancellationToken = default)
    {
        var target = new Uri(new Uri(baseUrl.TrimEnd('/')), $"/api/questions/{stateCode}/{programCode}");
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        using var gate = new SemaphoreSlim(maxConcurrency);

        var latencies = new List<long>(totalRequests);
        var errors = 0;

        var tasks = Enumerable.Range(0, totalRequests).Select(async _ =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var stopwatch = Stopwatch.StartNew();
                using var response = await httpClient.GetAsync(target, cancellationToken);
                stopwatch.Stop();

                lock (latencies)
                {
                    latencies.Add(stopwatch.ElapsedMilliseconds);
                }

                if (!response.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref errors);
                }
            }
            finally
            {
                gate.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        latencies.Sort();
        var p95 = Percentile(latencies, 0.95);
        var p99 = Percentile(latencies, 0.99);
        var average = latencies.Count == 0 ? 0 : latencies.Average();

        Console.WriteLine($"Question Definitions Load Test Results");
        Console.WriteLine($"Target: {target}");
        Console.WriteLine($"Total Requests: {totalRequests}");
        Console.WriteLine($"Errors: {errors}");
        Console.WriteLine($"Average: {average:0.0} ms");
        Console.WriteLine($"p95: {p95} ms");
        Console.WriteLine($"p99: {p99} ms");
    }

    private static long Percentile(IReadOnlyList<long> sorted, double percentile)
    {
        if (sorted.Count == 0)
            return 0;

        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Clamp(index, 0, sorted.Count - 1);
        return sorted[index];
    }
}
