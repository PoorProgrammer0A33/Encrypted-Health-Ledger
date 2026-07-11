using Microsoft.Research.SEAL;
using System.Collections.Generic;

namespace EHL.Crypto;

public class CkksHelper
{
    private readonly SEALContext _context;
    private readonly CKKSEncoder _encoder;
    private readonly Encryptor _encryptor;
    private readonly Evaluator _evaluator;
    private readonly Decryptor _decryptor;
    private readonly double _scale = System.Math.Pow(2.0, 40);
    private readonly RelinKeys _relinKeys;

    public PublicKey PublicKey { get; }
    public SecretKey SecretKey { get; }

    public CkksHelper()
    {
        var parms = new EncryptionParameters(SchemeType.CKKS);
        ulong polyModulusDegree = 8192;
        parms.PolyModulusDegree = polyModulusDegree;
        parms.CoeffModulus = CoeffModulus.Create(polyModulusDegree, new int[] { 60, 40, 40, 60 });

        _context = new SEALContext(parms);
        var keygen = new KeyGenerator(_context);

        SecretKey = keygen.SecretKey;
        keygen.CreatePublicKey(out PublicKey publicKey);
        PublicKey = publicKey;

        keygen.CreateRelinKeys(out RelinKeys relinKeys);
        _relinKeys = relinKeys;

        _encryptor = new Encryptor(_context, PublicKey);
        _evaluator = new Evaluator(_context);
        _decryptor = new Decryptor(_context, SecretKey);
        _encoder = new CKKSEncoder(_context);
    }

    public Ciphertext Encrypt(double value)
    {
        var plain = new Plaintext();
        _encoder.Encode(value, _scale, plain);
        var encrypted = new Ciphertext();
        _encryptor.Encrypt(plain, encrypted);
        return encrypted;
    }

    public Ciphertext Add(Ciphertext a, Ciphertext b)
    {
        var result = new Ciphertext();
        _evaluator.Add(a, b, result);
        return result;
    }

    public Ciphertext Multiply(Ciphertext a, Ciphertext b)
    {
        var result = new Ciphertext();
        _evaluator.Multiply(a, b, result);
        _evaluator.RelinearizeInplace(result, _relinKeys);
        _evaluator.RescaleToNextInplace(result);
        return result;
    }

    public double Decrypt(Ciphertext encrypted)
    {
        var plain = new Plaintext();
        _decryptor.Decrypt(encrypted, plain);
        var result = new List<double>();
        _encoder.Decode(plain, result);
        return result[0];
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
        {
            sum = Add(sum, values[i]);
        }
        return MultiplyByConstant(sum, 1.0 / values.Count);
    }
}