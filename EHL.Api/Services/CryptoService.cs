using EHL.Crypto;
using EHL.Ledger;
using Microsoft.Research.SEAL;
using System.Security.Cryptography;
using System.Text;

namespace EHL.Api.Services;

public class CryptoService
{
    private readonly CkksHelper _helper;
    private readonly LedgerService _ledger;
    private readonly List<Ciphertext> _submissions = new();

    public CryptoService(LedgerService ledger)
    {
        _helper = new CkksHelper();
        _ledger = ledger;
    }

    public void Submit(double value)
    {
        var encrypted = _helper.Encrypt(value);
        _submissions.Add(encrypted);

        string hash = ComputeHash(encrypted);
        _ledger.LogEvent("SUBMIT", hash);
    }

    public double ComputeAverage()
    {
        if (_submissions.Count == 0)
            throw new InvalidOperationException("No submissions yet.");

        var encAvg = _helper.Average(_submissions);
        double result = _helper.Decrypt(encAvg);

        string hash = ComputeHash(encAvg);
        _ledger.LogEvent("COMPUTE_AVERAGE", hash);

        return result;
    }

    public int SubmissionCount => _submissions.Count;

    private static string ComputeHash(Ciphertext ciphertext)
    {
        using var ms = new MemoryStream();
        ciphertext.Save(ms);
        byte[] hashBytes = SHA256.HashData(ms.ToArray());
        return Convert.ToHexString(hashBytes);
    }
}