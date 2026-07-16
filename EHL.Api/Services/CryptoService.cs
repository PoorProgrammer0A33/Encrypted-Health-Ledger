using EHL.Crypto;
using EHL.Ledger;
using Microsoft.AspNetCore.SignalR;
using EHL.Api.Hubs;
using Microsoft.Research.SEAL;
using System.Security.Cryptography;

namespace EHL.Api.Services;

public class CryptoService
{
    private readonly LedgerService _ledger;
    private readonly IHubContext<StatsHub> _hub;
    private readonly List<Ciphertext> _submissions = new();
    private ServerCrypto? _server;

    public CryptoService(LedgerService ledger, IHubContext<StatsHub> hub)
    {
        _ledger = ledger;
        _hub = hub;
    }

    public void RegisterKeys(byte[] relinKeysBytes)
    {
        var context = CkksParams.CreateContext();
        var relinKeys = new RelinKeys();
        using var ms = new MemoryStream(relinKeysBytes);
        relinKeys.Load(context, ms);

        _server = new ServerCrypto(relinKeys);
    }

    public async Task Submit(byte[] ciphertextBytes)
    {
        if (_server == null)
            throw new InvalidOperationException("Client must register keys before submitting.");

        var ct = _server.Deserialize(ciphertextBytes);
        _submissions.Add(ct);

        string hash = ComputeHash(ciphertextBytes);
        _ledger.LogEvent("SUBMIT", hash);

        // Recompute and push after each submission
        var sum = _server.Average(_submissions); // reuse existing sum-then-scale path for sum; see note below
        var sumOfSquares = _server.SumOfSquares(_submissions);

        byte[] sumBytes = _server.Serialize(_server.RawSum(_submissions)); // see note
        byte[] sumSqBytes = _server.Serialize(sumOfSquares);

        await _hub.Clients.All.SendAsync("StatsUpdated", new
        {
            SumBase64 = Convert.ToBase64String(sumBytes),
            SumOfSquaresBase64 = Convert.ToBase64String(sumSqBytes),
            Count = _submissions.Count
        });
    }

    public int SubmissionCount => _submissions.Count;

    private static string ComputeHash(byte[] bytes)
    {
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }

    public byte[] ComputeEncryptedAverage()
    {
        if (_server == null || _submissions.Count == 0)
            throw new InvalidOperationException("No submissions yet, or keys not registered.");

        var encAvg = _server.Average(_submissions);
        return _server.Serialize(encAvg);
    }
}