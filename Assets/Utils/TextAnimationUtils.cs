using DG.Tweening;
using TMPro;
using UnityEngine;
using Utils.TimerSystem;

namespace Utils
{
    public class TextAnimationUtils
    {
        public static void ChangeText(TMP_Text textComponent, string newText, float duration = AnimationHelper.NORMAL, System.Action onComplete = null)
        {
            if (textComponent == null)
            {
                Debug.LogWarning("<b>[TextAnimationUtils]</b> Cannot change text: TextComponent is null or destroyed.");
                return;
            }
            
            string currentText = textComponent.text;

            if (currentText == newText)
            {
                onComplete?.Invoke();
                return;
            }

            int commonPrefixLength = 0;
            int minLength = Mathf.Min(currentText.Length, newText.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (currentText[i] != newText[i])
                {
                    break;
                }

                commonPrefixLength++;
            }

            int charsToDelete = currentText.Length - commonPrefixLength;

            if (charsToDelete > 0)
            {
                float deleteDuration = duration * 0.5f;

                DOVirtual.Float(currentText.Length, commonPrefixLength, deleteDuration, (value) =>
                {
                    if (textComponent == null) return;
                    
                    int charCount = Mathf.FloorToInt(value);
                    textComponent.text = currentText.Substring(0, charCount);
                })
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (textComponent == null)
                    {
                        onComplete?.Invoke();
                        return;
                    }
                    
                    float addDuration = duration * 0.5f;

                    DOVirtual.Float(commonPrefixLength, newText.Length, addDuration, (value) =>
                    {
                        if (textComponent == null) return;
                        
                        int charCount = Mathf.FloorToInt(value);
                        textComponent.text = newText.Substring(0, charCount);
                    })
                    .SetEase(Ease.Linear)
                    .SetAutoKill(true)
                    .OnComplete(() =>
                    {
                        if (textComponent != null)
                            textComponent.text = newText;
                        onComplete?.Invoke();
                    });
                });

                return;
            }

            float addDuration = duration;

            DOVirtual.Float(commonPrefixLength, newText.Length, addDuration, (value) =>
            {
                if (textComponent == null) return;
                
                int charCount = Mathf.FloorToInt(value);
                textComponent.text = newText.Substring(0, charCount);
            })
            .SetEase(Ease.Linear)
            .SetAutoKill(true)
            .OnComplete(() =>
            {
                if (textComponent != null)
                    textComponent.text = newText;
                onComplete?.Invoke();
            });
        }

        public static TimerSystem.TimerSystem StartTimerText(TextMeshProUGUI textComponent, TimerSystem.TimerSystem timer, string label = "Timer", string format = "0.00", System.Action onComplete = null)
        {
            if (textComponent == null)
            {
                Debug.LogWarning("<b>[TextAnimationUtils]</b> Cannot start timer text: TextComponent is null or destroyed.");
                return timer;
            }
            
            ChangeText(textComponent, $"{label}: {timer.GetFormattedTime(format)}", 1f, () =>
                timer.onTimerTick += () =>
                {
                    if (textComponent)
                        textComponent.text = $"{label}: {timer.GetFormattedTime(format)}";
                });
            return timer;
        }
    }
}