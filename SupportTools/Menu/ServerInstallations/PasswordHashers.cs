using System;
using System.Security.Cryptography;
using System.Text;

namespace SupportTools.Menu.ServerInstallations;

//ინსტალაციის პაროლების ჰეშები. ღია პაროლი არსად ინახება, autoinstall.yaml-ში მხოლოდ ეს ჰეშები ხვდება
public static class PasswordHashers
{
    private const string CryptAlphabet = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int CryptRounds = 5000;
    private const int CryptSaltLength = 16;

    //SHA-512 crypt-ის შედეგის ბაიტების რიგი სამ-სამად (Ulrich Drepper, "Unix crypt using SHA-256 and SHA-512")
    private static readonly int[] CryptByteOrder =
    [
        0, 21, 42, 22, 43, 1, 44, 2, 23, 3, 24, 45, 25, 46, 4, 47, 5, 26, 6, 27, 48, 28, 49, 7, 50, 8, 29, 9, 30, 51,
        31, 52, 10, 53, 11, 32, 12, 33, 54, 34, 55, 13, 56, 14, 35, 15, 36, 57, 37, 58, 16, 59, 17, 38, 18, 39, 60, 40,
        61, 19, 62, 20, 41
    ];

    //Linux-ის მომხმარებლის პაროლი: $6$<salt>$<hash>, იგივე, რასაც openssl passwd -6 იძლევა
    public static string LinuxSha512Crypt(string password)
    {
        return LinuxSha512Crypt(password, RandomNumberGenerator.GetString(CryptAlphabet, CryptSaltLength));
    }

    internal static string LinuxSha512Crypt(string password, string salt)
    {
        byte[] p = Encoding.UTF8.GetBytes(password);
        byte[] s = Encoding.UTF8.GetBytes(salt);

        byte[] b = Sha512(p, s, p);

        using var aHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        aHash.AppendData(p);
        aHash.AppendData(s);
        aHash.AppendData(Repeat(b, p.Length));
        for (int length = p.Length; length > 0; length >>= 1)
        {
            aHash.AppendData((length & 1) != 0 ? b : p);
        }

        byte[] a = aHash.GetHashAndReset();

        using var dpHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        for (int i = 0; i < p.Length; i++)
        {
            dpHash.AppendData(p);
        }

        byte[] pSequence = Repeat(dpHash.GetHashAndReset(), p.Length);

        using var dsHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        for (int i = 0; i < 16 + a[0]; i++)
        {
            dsHash.AppendData(s);
        }

        byte[] sSequence = Repeat(dsHash.GetHashAndReset(), s.Length);

        byte[] c = a;
        using var cHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        for (int i = 0; i < CryptRounds; i++)
        {
            cHash.AppendData(i % 2 != 0 ? pSequence : c);
            if (i % 3 != 0)
            {
                cHash.AppendData(sSequence);
            }

            if (i % 7 != 0)
            {
                cHash.AppendData(pSequence);
            }

            cHash.AppendData(i % 2 != 0 ? c : pSequence);
            c = cHash.GetHashAndReset();
        }

        var result = new StringBuilder("$6$").Append(salt).Append('$');
        for (int i = 0; i < CryptByteOrder.Length; i += 3)
        {
            AppendCryptBase64(result, (c[CryptByteOrder[i]] << 16) | (c[CryptByteOrder[i + 1]] << 8) |
                                      c[CryptByteOrder[i + 2]], 4);
        }

        AppendCryptBase64(result, c[63], 2);
        return result.ToString();
    }

    //MariaDB-ის mysql_native_password: *HEX(SHA1(SHA1(პაროლი)))
    public static string MariaDbNativePassword(string password)
    {
        //ამ ფორმატს MariaDB ითხოვს, სხვა ალგორითმს ვერ ავირჩევთ
#pragma warning disable CA5350, S4790
        byte[] hash = SHA1.HashData(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
#pragma warning restore CA5350, S4790
        return "*" + Convert.ToHexString(hash);
    }

    //SQL Server-ის ლოგინის ჰეში ALTER LOGIN ... WITH PASSWORD = 0x... HASHED-ისთვის:
    //0x0200 + 4-ბაიტიანი salt + SHA-512(პაროლი UTF-16LE-ში + salt)
    public static string SqlServerPassword(string password)
    {
        return SqlServerPassword(password, RandomNumberGenerator.GetBytes(4));
    }

    internal static string SqlServerPassword(string password, byte[] salt)
    {
        byte[] hash = Sha512(Encoding.Unicode.GetBytes(password), salt);
        return "0x0200" + Convert.ToHexString(salt) + Convert.ToHexString(hash);
    }

    private static byte[] Sha512(params byte[][] parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        foreach (byte[] part in parts)
        {
            hash.AppendData(part);
        }

        return hash.GetHashAndReset();
    }

    private static byte[] Repeat(byte[] block, int length)
    {
        byte[] result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = block[i % block.Length];
        }

        return result;
    }

    private static void AppendCryptBase64(StringBuilder sb, int value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            sb.Append(CryptAlphabet[value & 0x3f]);
            value >>= 6;
        }
    }
}
