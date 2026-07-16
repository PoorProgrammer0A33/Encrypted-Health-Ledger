using EHL.Crypto;
using Microsoft.Research.SEAL;

var client = new ClientCrypto();

using var ms = new MemoryStream();
client.RelinKeys.Save(ms);
var relinBytes = ms.ToArray();

var context = CkksParams.CreateContext();
var relinKeys = new RelinKeys();
using var loadMs = new MemoryStream(relinBytes);
relinKeys.Load(context, loadMs);

var server = new ServerCrypto(relinKeys);

var values = new List<double> { 42.5, 17.3, 60.0, 25.0 };
var encryptedBytes = values.ConvertAll(v => client.Encrypt(v));

var deserialized = encryptedBytes.ConvertAll(b => server.Deserialize(b));
var encAvgResult = server.Average(deserialized);
var resultBytes = server.Serialize(encAvgResult);

double decrypted = client.Decrypt(resultBytes);

Console.WriteLine($"Plaintext average: {values.Average()}");
Console.WriteLine($"Decrypted (via full client/server round trip): {decrypted}");