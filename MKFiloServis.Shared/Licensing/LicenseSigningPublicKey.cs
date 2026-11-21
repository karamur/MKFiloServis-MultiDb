namespace MKFiloServis.Shared.Licensing;

public static class LicenseSigningPublicKey
{
    // Yalnız açık anahtar; imzalama sırrı dağıtıma girmez.
    public const string Pem = """
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA2rupnxuzVpZS5XyWI9rG
        /DuHchAOdXhPtw6Glw76Vz/UMkr9pCg/voi4RMu0jbDJhzbkDUzzerTnA+KuzMP8
        hmie4R52ufgB9i+3j2pfOB+P4OXPNyWxshwf1S7TbWS9saFUH5RvLSl9t1yr6zAg
        j2XY4SFbGZPnA0ADv+ykBGVy4/40+5mhUwP+N45cvgqED2J73knTlNQ6tI563FZK
        lb46O5tQAL+WYdSvX8h5AuSGFIvF9/bKJnoFiR7lbwaTIPlVMQoCa4EwQsgNc5ZZ
        +G+F6k4BzW/d0NEmOnuDOCAIc9RlyBzi4aouCD21HaRm/qqm4kUbsLo+f1i10J7k
        DGENpZAsNp0XsFONYblAMgYRsiHyynFzISFHv/0AAvdMvEt2ZdqJZ28SU+q8lstk
        GdhA571ahom0UOjQHJiwI6+W/QbbsOPzrKB+bSMaGOZiIYjOWEX20N/f9W1Mu0Ik
        EFXF/46bMNVIqrDqpWpjP5Lm39RdxUQEe/F82vxOpLflAgMBAAE=
        -----END PUBLIC KEY-----
        """;
}
