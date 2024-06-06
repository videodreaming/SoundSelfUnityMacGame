using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public static class EncryptionHelper
{
    public static readonly string EncryptionKey;
    public static readonly byte[] Key;

    static EncryptionHelper()
    {
        // Load the key from configuration file
        Config config = ConfigLoader.LoadConfig();
        if (config != null)
        {
            EncryptionKey = config.EncryptionKey;
            Key = Convert.FromBase64String(EncryptionKey);
        }
        else
        {
            throw new InvalidOperationException("Encryption key not found.");
        }
    }

    public static string Encrypt(string plainText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.GenerateIV();
            byte[] iv = aesAlg.IV;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, iv);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                byte[] encrypted = msEncrypt.ToArray();
                return $"{BitConverter.ToString(iv).Replace("-", "")}:{BitConverter.ToString(encrypted).Replace("-", "")}";
            }
        }
    }

    public static string Decrypt(string encryptedText)
    {
        string[] parts = encryptedText.Split(':');
        byte[] iv = Enumerable.Range(0, parts[0].Length / 2)
                              .Select(x => Convert.ToByte(parts[0].Substring(x * 2, 2), 16))
                              .ToArray();
        byte[] cipherText = Enumerable.Range(0, parts[1].Length / 2)
                                      .Select(x => Convert.ToByte(parts[1].Substring(x * 2, 2), 16))
                                      .ToArray();

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = iv;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
