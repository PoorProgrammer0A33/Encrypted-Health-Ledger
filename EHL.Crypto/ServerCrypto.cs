using Microsoft.Research.SEAL;
using System.Collections.Generic;
using System.IO;

namespace EHL.Crypto;

public class ServerCrypto
{
    private readonly SEALContext _context;
    private readonly Evaluator _evaluator;
    private readonly CKKSEncoder _encoder;
    private readonly RelinKeys _relinKeys;

    public ServerCrypto(RelinKeys relinKeys)
    {
        _context = CkksParams.CreateContext();
        _evaluator = new Evaluator(_context);
        _encoder = new CKKSEncoder(_context);
        _relinKeys = relinKeys;
    }

    public Ciphertext Deserialize(byte[] bytes)
    {
        var ct = new Ciphertext(_context);
        using var ms = new MemoryStream(bytes);
        ct.Load(_context, ms);
        return ct;
    }

    public byte[] Serialize(Ciphertext ct)
    {
        using var ms = new MemoryStream();
        ct.Save(ms);
        return ms.ToArray();
    }

    public Ciphertext Add(Ciphertext a, Ciphertext b)
    {
        var result = new Ciphertext();
        _evaluator.Add(a, b, result);
        return result;
    }

    public Ciphertext MultiplyByConstant(Ciphertext encrypted, double constant)
    {
        var plainConstant = new Plaintext();
        _encoder.Encode(constant, encrypted.Scale, plainConstant);
        _evaluator.ModSwitchToInplace(plainConstant, encrypted.ParmsId);

        var result = new Ciphertext();
        _evaluator.MultiplyPlain(encrypted, plainConstant, result);
        _evaluator.RescaleToNextInplace(result);
        return result;
    }

    public Ciphertext Average(List<Ciphertext> values)
    {
        var sum = values[0];
        for (int i = 1; i < values.Count; i++)
            sum = Add(sum, values[i]);
        return MultiplyByConstant(sum, 1.0 / values.Count);
    }

    public Ciphertext SumOfSquares(List<Ciphertext> values)
    {
        Ciphertext sum = null;
        foreach (var v in values)
        {
            var squared = new Ciphertext();
            _evaluator.Multiply(v, v, squared);
            _evaluator.RelinearizeInplace(squared, _relinKeys); // needs RelinKeys in ServerCrypto now
            _evaluator.RescaleToNextInplace(squared);

            if (sum == null) sum = squared;
            else sum = Add(sum, squared);
        }
        return sum;
    }

    public Ciphertext RawSum(List<Ciphertext> values)
    {
        var sum = values[0];
        for (int i = 1; i < values.Count; i++)
            sum = Add(sum, values[i]);
        return sum;
    }
}