using EHL.Crypto;

var helper = new CkksHelper();

double a = 42.5;
double b = 17.3;

Console.WriteLine($"Plaintext:  {a} + {b} = {a + b}");

var encA = helper.Encrypt(a);
var encB = helper.Encrypt(b);
var encSum = helper.Add(encA, encB);
double decrypted = helper.Decrypt(encSum);

Console.WriteLine($"Encrypted sum, decrypted: {decrypted}");

var encProduct = helper.Multiply(encA, encB);
double decryptedProduct = helper.Decrypt(encProduct);
Console.WriteLine($"Plaintext:  {a} * {b} = {a * b}");
Console.WriteLine($"Encrypted product, decrypted: {decryptedProduct}");

var values = new List<double>() { 42.5, 17.3, 60.0, 25.0 };
var encryptedValues = values.ConvertAll(v => helper.Encrypt(v));

var encAvg = helper.Average(encryptedValues);
var decryptAvg = helper.Decrypt(encAvg);

Console.WriteLine($"Plaintext: {values.Average()}");
Console.WriteLine($"Encrypted Average, decrypted: {decryptAvg}");