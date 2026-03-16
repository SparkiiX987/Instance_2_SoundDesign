using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Auth.Services
{
    /// <summary>
    /// Stockage sécurisé des tokens (encryption simple XOR + Base64)
    /// Pour prod: considérer une lib plus robuste ou keychain natif
    /// </summary>
    public static class SecureStorage
    {
        private static readonly string ENCRYPTION_KEY = GetDeviceKey();

        public static void SaveToken(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            string encrypted = Encrypt(value);
            PlayerPrefs.SetString(key, encrypted);
            PlayerPrefs.Save();
        }

        public static string GetToken(string key)
        {
            if (!PlayerPrefs.HasKey(key))
                return null;

            string encrypted = PlayerPrefs.GetString(key);
            return Decrypt(encrypted);
        }

        public static void DeleteToken(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private static string Encrypt(string plainText)
        {
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
                
                // XOR simple (MVP, pas production-ready)
                byte[] encrypted = new byte[plainBytes.Length];
                for (int i = 0; i < plainBytes.Length; i++)
                {
                    encrypted[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                return Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecureStorage] Encryption failed: {ex.Message}");
                return plainText; // Fallback (pas idéal)
            }
        }

        private static string Decrypt(string encryptedText)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
                
                // XOR inverse
                byte[] decrypted = new byte[encryptedBytes.Length];
                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    decrypted[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecureStorage] Decryption failed: {ex.Message}");
                return null;
            }
        }

        private static string GetDeviceKey()
        {
            // Utiliser un hash du device ID comme clé (pas parfait mais mieux que rien)
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId));
                return Convert.ToBase64String(hashBytes).Substring(0, 32);
            }
        }
    }
}

