using EHL.Crypto;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

var apiBase = "https://localhost:7245"; // match your actual port
var client = new ClientCrypto();
using var http = new HttpClient { BaseAddress = new Uri(apiBase) };

// Step 1: register RelinKeys with the server (one-time, non-secret)
using (var ms = new MemoryStream())
{
    client.RelinKeys.Save(ms);
    string relinBase64 = Convert.ToBase64String(ms.ToArray());
    var reg = await http.PostAsJsonAsync("/api/HealthStats/register-keys", new { RelinKeysBase64 = relinBase64 });
    Console.WriteLine($"Key registration: {reg.StatusCode}");
}

// Step 2: connect to the live stats hub
var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBase}/statshub")
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();

connection.On<StatsUpdate>("StatsUpdated", update =>
{
    byte[] sumBytes = Convert.FromBase64String(update.SumBase64);
    byte[] sumSqBytes = Convert.FromBase64String(update.SumOfSquaresBase64);

    double sum = client.Decrypt(sumBytes);
    double sumSq = client.Decrypt(sumSqBytes);
    int n = update.Count;

    double mean = sum / n;
    double variance = (sumSq / n) - (mean * mean);
    double stddev = Math.Sqrt(Math.Max(variance, 0)); // guard tiny negative drift

    int sumLevel = client.GetRemainingModulusLevels(sumBytes);
    int sumSqLevel = client.GetRemainingModulusLevels(sumSqBytes);

    Console.WriteLine($"\n[Live update] n={n}");
    Console.WriteLine($"  Mean:   {mean:F4}");
    Console.WriteLine($"  StdDev: {stddev:F4}");
    Console.WriteLine($"  Modulus chain level remaining — sum: {sumLevel}, sumOfSquares: {sumSqLevel}");
});

await connection.StartAsync();
Console.WriteLine("Connected to live stats hub. Submitting values...\n");

// Step 3: submit values, one at a time, watch live pushes arrive
var values = new List<double> { 42.5, 17.3, 60.0, 25.0, 30.1 };
foreach (var v in values)
{
    byte[] encrypted = client.Encrypt(v);
    string base64 = Convert.ToBase64String(encrypted);
    var response = await http.PostAsJsonAsync("/api/HealthStats/submit", new { CiphertextBase64 = base64 });
    string body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Submit {v}: {response.StatusCode} — {body}");
    await Task.Delay(1500);
}


Console.WriteLine("\nDone. Press any key to exit.");
Console.ReadKey();

record StatsUpdate(string SumBase64, string SumOfSquaresBase64, int Count);