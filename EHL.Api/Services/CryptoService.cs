using EHL.Crypto;
using EHL.Ledger;
using Microsoft.Research.SEAL;
using System.Security.Cryptography;

namespace EHL.Api.Services;

public class CryptoService
{
    private readonly ServerCrypto _server;
    private readonly LedgerService _ledger;
    private readonly List<Ciphertext> _submissions = new();

    public CryptoService(LedgerService ledger)
    {
        _server = new ServerCrypto();
        _ledger = ledger;
    }

    public void Submit(byte[] ciphertextBytes)
    {
        var ct = _server.Deserialize(ciphertextBytes);
        _submissions.Add(ct);

        string hash = ComputeHash(ciphertextBytes);
        _ledger.LogEvent("SUBMIT", hash);
    }

    public byte[] ComputeEncryptedAverage()
    {
        if (_submissions.Count == 0)
            throw new InvalidOperationException("No submissions yet.");

        var encAvg = _server.Average(_submissions);
        byte[] resultBytes = _server.Serialize(encAvg);

        string hash = ComputeHash(resultBytes);
        _ledger.LogEvent("COMPUTE_AVERAGE", hash);

        return resultBytes;
    }

    public int SubmissionCount => _submissions.Count;

    private static string ComputeHash(byte[] bytes)
    {
        byte[] hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}