using Microsoft.Research.SEAL;
using System.Collections.Generic;
using System.IO;

namespace EHL.Crypto;

public class ServerCrypto
{
    private readonly SEALContext _context;
    private readonly Evaluator _evaluator;
    private readonly CKKSEncoder _encoder;

    public ServerCrypto()
    {
        _context = CkksParams.CreateContext();
        _evaluator = new Evaluator(_context);
        _encoder = new CKKSEncoder(_context);
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
}