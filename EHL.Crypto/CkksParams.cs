using Microsoft.Research.SEAL;

namespace EHL.Crypto;

public static class CkksParams
{
    public static SEALContext CreateContext()
    {
        var parms = new EncryptionParameters(SchemeType.CKKS);
        ulong polyModulusDegree = 8192;
        parms.PolyModulusDegree = polyModulusDegree;
        parms.CoeffModulus = CoeffModulus.Create(polyModulusDegree, new int[] { 60, 40, 40, 60 });
        return new SEALContext(parms);
    }
}