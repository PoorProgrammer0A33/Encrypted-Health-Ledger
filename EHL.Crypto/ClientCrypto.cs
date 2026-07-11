using Microsoft.Research.SEAL;
using System.Collections.Generic;
using System.IO;

namespace EHL.Crypto;

public class ClientCrypto
{
    private readonly SEALContext _context;
    private readonly CKKSEncoder _encoder;
    private readonly Encryptor _encryptor;
    private readonly Decryptor _decryptor;
    private readonly double _scale = System.Math.Pow(2.0, 40);

    public PublicKey PublicKey { get; }
    public SecretKey SecretKey { get; }

    public ClientCrypto()
    {
        _context = CkksParams.CreateContext();
        var keygen = new KeyGenerator(_context);

        SecretKey = keygen.SecretKey;
        keygen.CreatePublicKey(out PublicKey publicKey);
        PublicKey = publicKey;

        _encryptor = new Encryptor(_context, PublicKey);
        _decryptor = new Decryptor(_context, SecretKey);
        _encoder = new CKKSEncoder(_context);
    }

    public byte[] Encrypt(double value)
    {
        var plain = new Plaintext();
        _encoder.Encode(value, _scale, plain);
        var encrypted = new Ciphertext();
        _encryptor.Encrypt(plain, encrypted);

        using var ms = new MemoryStream();
        encrypted.Save(ms);
        return ms.ToArray();
    }

    public double Decrypt(byte[] ciphertextBytes)
    {
        var encrypted = new Ciphertext(_context);
        using var ms = new MemoryStream(ciphertextBytes);
        encrypted.Load(_context, ms);

        var plain = new Plaintext();
        _decryptor.Decrypt(encrypted, plain);
        var result = new List<double>();
        _encoder.Decode(plain, result);
        return result[0];
    }
}