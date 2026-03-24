using UnityEngine;

namespace Player.Scripts
{
    public static class InputPrefs
    {
        private const string playerpfercuche = "playerpfercuche";
        private const string playerpferspeide = "playerpferspeide";

        public static bool IsCrouchToggleEnabled
        {
            get => PlayerPrefs.GetInt(playerpfercuche, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(playerpfercuche, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool IsSprintToggleEnabled
        {
            get => PlayerPrefs.GetInt(playerpferspeide, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(playerpferspeide, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}