using UnityEngine;
using UnityEngine.UI;
using Player.Scripts;

namespace UI.Scripts
{
    /// <summary>
    /// Handles menu toggles for crouch and sprint input modes.
    /// Saves values in PlayerPrefs so they can be reused in another scene.
    /// </summary>
    public class InputMenuPrefs : MonoBehaviour
    {
        [Header("Input Toggles")]
        [SerializeField] private Toggle crouchToggle;
        [SerializeField] private Toggle sprintToggle;

        /// <summary>
        /// Loads saved values and initializes toggle states.
        /// </summary>
        private void Start()
        {
            if (crouchToggle != null)
            {
                crouchToggle.isOn = InputPrefs.IsCrouchToggleEnabled;
                crouchToggle.onValueChanged.AddListener(OnCrouchToggleChanged);
            }

            if (sprintToggle != null)
            {
                sprintToggle.isOn = InputPrefs.IsSprintToggleEnabled;
                sprintToggle.onValueChanged.AddListener(OnSprintToggleChanged);
            }
        }

        /// <summary>
        /// Removes listeners when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (crouchToggle != null)
                crouchToggle.onValueChanged.RemoveListener(OnCrouchToggleChanged);

            if (sprintToggle != null)
                sprintToggle.onValueChanged.RemoveListener(OnSprintToggleChanged);
        }

        /// <summary>
        /// Saves crouch toggle mode.
        /// </summary>
        /// <param name="_value">New crouch toggle value.</param>
        private void OnCrouchToggleChanged(bool _value)
        {
            InputPrefs.IsCrouchToggleEnabled = _value;
        }

        /// <summary>
        /// Saves sprint toggle mode.
        /// </summary>
        /// <param name="_value">New sprint toggle value.</param>
        private void OnSprintToggleChanged(bool _value)
        {
            InputPrefs.IsSprintToggleEnabled = _value;
        }
    }
}