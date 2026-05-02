namespace DumbFox;

using System;
using System.Security.Cryptography;
using System.Text;

public static class DumbfoxEncryption
{
	public static string DecryptIvPrependedBase64(string b64, byte[] aesKey)
	{
		byte[] blob = Convert.FromBase64String(b64);
		if (blob.Length < 16) throw new Exception("Blob too short to contain IV");
		
		byte[] iv = new byte[16];
		Buffer.BlockCopy(blob, 0, iv, 0, 16);
		
		int ctLen = blob.Length - 16;
		byte[] ct = new byte[ctLen];
		Buffer.BlockCopy(blob, 16, ct, 0, ctLen);

		using Aes aes = Aes.Create();
		aes.Key = aesKey;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using ICryptoTransform dec = aes.CreateDecryptor();
		byte[] pt = dec.TransformFinalBlock(ct, 0, ct.Length);

		return Encoding.UTF8.GetString(pt);
	}
	
	public static byte[] RandomAes256Key()
	{
		byte[] key = new byte[32]; // 256-bit
		RandomNumberGenerator.Fill(key);
		return key;
	}

	public static RSA BuildRsaFromModExp(byte[] modulus, byte[] exponent)
	{
		RSA rsa = RSA.Create();
		rsa.ImportParameters(new RSAParameters
		{
			Modulus = modulus,
			Exponent = exponent
		});
		return rsa;
	}

	public static byte[] RsaEncryptPkcs1(RSA rsa, byte[] data)
	{
		// Java Cipher for RSA often defaults to "RSA/ECB/PKCS1Padding"
		// That corresponds to RSAEncryptionPadding.Pkcs1
		return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
	}

    public static string EncryptB64(string value, byte[] aesSecret)
    {
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        using var aes = Aes.Create();
        aes.Key = aesSecret;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        byte[] plaintext = Encoding.UTF8.GetBytes(value);
        byte[] encrypted = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        byte[] payload = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, payload, iv.Length, encrypted.Length);
        return Convert.ToBase64String(payload);
    }

    public static string DecryptB64(string payloadB64, byte[] aesSecret)
    {
        byte[] payload = Convert.FromBase64String(payloadB64);
        byte[] iv = payload.Take(16).ToArray();
        byte[] ciphertext = payload.Skip(16).ToArray();
        using var aes = Aes.Create();
        aes.Key = aesSecret;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        byte[] decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}