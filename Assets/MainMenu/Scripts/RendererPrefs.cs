using UnityEngine;

namespace Player.Scripts
{
    /// <summary>
    /// Sauvegarde de l'état du FullScreenPassRendererFeature
    /// </summary>
    public static class S_RendererPrefs
    {
        private const string fullscreenPassKey = "fullscreenPassEnabled";

        /// <summary>
        /// True = activé, False = désactivé
        /// </summary>
        public static bool IsFullScreenPassEnabled
        {
            get => PlayerPrefs.GetInt(fullscreenPassKey, 1) == 1; // par défaut activé
            set
            {
                PlayerPrefs.SetInt(fullscreenPassKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}