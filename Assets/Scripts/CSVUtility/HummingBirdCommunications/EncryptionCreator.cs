using System;
using System.Security.Cryptography;
using UnityEngine;

public class GenerateKey : MonoBehaviour
{
    void Start()
    {
        GenerateAndLogKey();
    }

    private void GenerateAndLogKey()
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.KeySize = 256;
            aesAlg.GenerateKey();
            string base64Key = Convert.ToBase64String(aesAlg.Key);
            Debug.Log("Generated Key: " + base64Key);
        }
    }
}
