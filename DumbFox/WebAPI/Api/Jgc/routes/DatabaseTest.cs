using Common.Utils;
using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Net.Sockets;
using System.Security.Cryptography;
namespace WebAPI.Api.Jgc.routes;


public static class DatabaseTest
{
    public static async Task Handle(HttpContext ctx)
    {
        string publicKeyModulus = "rIR3tgZjBXcPFnkhPSoe6bUGwiEY61W9pux+auqhVJZ8s7FzhBgiiKqjLDwxLddB0wJHWBR99ER2UzN+Nj/+5716Mn/pRxp7k47tFkTmN/xMqgXkiYQDmRFUVHplN6mcavvyKGIXGHZDTsh1MZeTar9Ah03Rmn2RXrzHtesyM5U=";
        string publicKeyExponent = "EQ==";


        byte[] modulus = Convert.FromBase64String(publicKeyModulus);
        byte[] exponent = Convert.FromBase64String(publicKeyExponent);

        byte[] aesKey = DumbfoxEncryption.RandomAes256Key();

        byte[] encrypted;
        using (RSA rsa = DumbfoxEncryption.BuildRsaFromModExp(modulus, exponent))
        {
            encrypted = DumbfoxEncryption.RsaEncryptPkcs1(rsa, aesKey);
        }

        string ekB64 = Convert.ToBase64String(encrypted);

        Console.WriteLine($"PublicKeyModulus: {publicKeyModulus}");
        Console.WriteLine($"PublicKeyExponent: {publicKeyExponent}");
        Console.WriteLine($"AesKey: {Convert.ToBase64String(aesKey)}");
        Console.WriteLine($"EncryptedSymmetricKey: {ekB64}");

    }
}

