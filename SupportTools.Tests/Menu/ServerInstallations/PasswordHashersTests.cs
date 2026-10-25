using System;
using System.Security.Cryptography;
using System.Text;
using SupportTools.Menu.ServerInstallations;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

public sealed class PasswordHashersTests
{
    //the test vector of Ulrich Drepper's specification ("Unix crypt using SHA-256 and SHA-512")
    [Fact]
    public void LinuxSha512Crypt_WithSpecificationSalt_ReturnsSpecificationHash()
    {
        // Act
        string result = PasswordHashers.LinuxSha512Crypt("Hello world!", "saltstring");

        // Assert
        Assert.Equal(
            "$6$saltstring$svn8UoSVapNtMuq1ukKS4tPQd8iKwSMHWjl/O817G3uBnIFNjnQJuesI68u4OTLiBFdcbYEdFCoEOfaS35inz1",
            result);
    }

    //a password longer than one SHA-512 digest (64 bytes) and a 16-character salt, as openssl passwd -6 gives it
    [Fact]
    public void LinuxSha512Crypt_WithLongPassword_ReturnsOpensslHash()
    {
        // Act
        string result = PasswordHashers.LinuxSha512Crypt(
            "a very much longer text to encrypt.  This one even stretches over more than one line, really.",
            "abcdefgh12345678");

        // Assert
        Assert.Equal(
            "$6$abcdefgh12345678$742V.p6WPBX70LJ2bsJfW/ka3MDdpeG6QS2qq7yb5PJZzlSDOBh0tyzj8.V4SxiNY5ZQ0NQJYJTmRxz3r23oE/",
            result);
    }

    [Fact]
    public void LinuxSha512Crypt_WithRandomSalt_CanBeVerifiedWithItsSalt()
    {
        // Act
        string result = PasswordHashers.LinuxSha512Crypt("secret");

        // Assert
        string salt = result.Split('$')[2];
        Assert.Equal(16, salt.Length);
        Assert.Equal(result, PasswordHashers.LinuxSha512Crypt("secret", salt));
    }

    //the well-known value of MariaDB's PASSWORD('password')
    [Fact]
    public void MariaDbNativePassword_ForPassword_ReturnsKnownHash()
    {
        // Act
        string result = PasswordHashers.MariaDbNativePassword("password");

        // Assert
        Assert.Equal("*2470C0C06DEE42FD1618BB99005ADCA2EC9D1E19", result);
    }

    [Fact]
    public void SqlServerPassword_WithSalt_IsVersionSaltAndSha512OfUtf16PasswordAndSalt()
    {
        // Arrange
        byte[] salt = [0x04, 0x14, 0xC3, 0x4D];
        byte[] expectedHash = SHA512.HashData([.. Encoding.Unicode.GetBytes("secret"), .. salt]);

        // Act
        string result = PasswordHashers.SqlServerPassword("secret", salt);

        // Assert
        Assert.Equal("0x02000414C34D" + Convert.ToHexString(expectedHash), result);
    }

    [Fact]
    public void SqlServerPassword_WithRandomSalt_HasHashedLoginFormat()
    {
        // Act
        string result = PasswordHashers.SqlServerPassword("secret");

        // Assert
        Assert.Matches("^0x0200[0-9A-F]{136}$", result);
    }
}
