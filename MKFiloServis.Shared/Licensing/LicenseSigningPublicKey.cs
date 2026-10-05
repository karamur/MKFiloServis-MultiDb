namespace MKFiloServis.Shared.Licensing;

public static class LicenseSigningPublicKey
{
    // Yalnız açık anahtar; imzalama sırrı dağıtıma girmez.
    public const string Pem = """
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAxScEI0CuYyAPEl+sMAC3
        OXJnLMa/2ljHLdAGrneyBnPEFu+fsLAWZqsTqaC6c5JrTWHqXS+F6s4Jvi3bebmd
        bvEjVamgxkVxcBsvLxkU/phtEOUwYxIOGx7wcr7kathx44CIdTtKAym2WO51Ur7O
        KiHrnPALGvc1pi4FpyKErlS574nB/xgsvsC3XE8CBCzYdpCSeJIZvCVGCtrVbA56
        3vZh+MxIXSaei4TgwKbdqmIi0cEWX8VVmVUjqSzxM9v7vLg7GqM4VCUJR+59vsbr
        PYbb9LJRWNa8GOnLJfgolcUILdnZ97nsM89v1JDHG6Rs9qzJwbrPpFoh2mor4oOY
        A6sgPHVSGQvm1rYThtgBDAbRBjqi02OV4SwWCIanuXpiJm9E2pzlm+HIhj/FwMC6
        Ojm+kE5OKCdBLKksZ6Lzpv7PhZab6wnq6zzd8dTRRM59WN/w+2YTQV0106Q0kOuo
        xgmDfTgdYxtrh+/Rkxg4U/0wN7F4CVPhLn28Xr95NZB9AgMBAAE=
        -----END PUBLIC KEY-----
        """;
}
