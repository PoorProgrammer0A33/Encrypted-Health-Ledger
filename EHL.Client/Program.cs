using EHL.Crypto;
using System.Net.Http.Json;

var client = new ClientCrypto();
using var http = new HttpClient { BaseAddress = new Uri("https://localhost:7245") }; // match your actual port

var values = new List<double> { 42.5, 17.3, 60.0, 25.0 };

foreach (var v in values)
{
    byte[] encryptedBytes = client.Encrypt(v);
    string base64 = Convert.ToBase64String(encryptedBytes);

    var response = await http.PostAsJsonAsync("/api/HealthStats/submit", new { CiphertextBase64 = base64 });
    Console.WriteLine($"Submitted {v}: {response.StatusCode}");
}

var avgResponse = await http.GetFromJsonAsync<AverageResponse>("/api/HealthStats/average");
double decryptedAverage = client.Decrypt(Convert.FromBase64String(avgResponse!.EncryptedAverageBase64));

Console.WriteLine($"Decrypted average (client-side only): {decryptedAverage}");

record AverageResponse(string EncryptedAverageBase64, int BasedOnCount);