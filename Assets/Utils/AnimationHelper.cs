using DG.Tweening;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Rotation direction for spinners and animations
    /// </summary>
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    /// <summary>
    /// Design system for all DOTween animations in the client
    /// Durations, easings, and reusable helpers
    /// </summary>
    public static class AnimationHelper
    {
        #region Timings

        public const float INSTANT = 0.1f;
        public const float FAST = 0.2f;
        public const float NORMAL = 0.3f;
        public const float MEDIUM = 0.4f;
        public const float SLOW = 0.5f;
        public const float VERY_SLOW = 0.8f;

        #endregion

        #region Easings

        public static readonly Ease IN_BACK = Ease.OutBack;
        public static readonly Ease IN_SMOOTH = Ease.OutQuad;
        public static readonly Ease IN_ELASTIC = Ease.OutElastic;
        public static readonly Ease OUT_SMOOTH = Ease.InQuad;
        public static readonly Ease OUT_FAST = Ease.InCubic;
        public static readonly Ease LOOP = Ease.InOutSine;
        public static readonly Ease OUT_LOOP = Ease.OutSine;
        public static readonly Ease HOVER = Ease.OutQuad;

        #endregion

        #region Screen Transitions

        /// <summary>
        /// Show a screen (fade + slide)
        /// </summary>
        public static Sequence FadeInScreen(CanvasGroup canvasGroup, RectTransform rectTransform, float slideOffset = 50f)
        {
            Sequence sequence = DOTween.Sequence();
            
            // Initial state
            canvasGroup.alpha = 0;
            rectTransform.anchoredPosition = new Vector2(slideOffset, 0);
            canvasGroup.gameObject.SetActive(true);

            // Animation
            sequence.Append(canvasGroup.DOFade(1f, NORMAL).SetEase(IN_SMOOTH));
            sequence.Join(rectTransform.DOAnchorPosX(0, NORMAL).SetEase(IN_SMOOTH));
            sequence.OnComplete(() => canvasGroup.interactable = true);

            return sequence;
        }

        /// <summary>
        /// Hide a screen (fade + slide)
        /// </summary>
        public static Sequence FadeOutScreen(CanvasGroup canvasGroup, RectTransform rectTransform, float slideOffset = -50f)
        {
            Sequence sequence = DOTween.Sequence();
            
            canvasGroup.interactable = false;

            // Animation
            sequence.Append(canvasGroup.DOFade(0f, FAST).SetEase(OUT_SMOOTH));
            sequence.Join(rectTransform.DOAnchorPosX(slideOffset, FAST).SetEase(OUT_FAST));
            sequence.OnComplete(() => canvasGroup.gameObject.SetActive(false));

            return sequence;
        }

        #endregion

        #region Panel Animations

        /// <summary>
        /// Panel that "pops" in (appearance with scale bounce)
        /// </summary>
        public static Sequence PopIn(Transform transform, float duration = MEDIUM)
        {
            Sequence sequence = DOTween.Sequence();
            
            // Initial state
            transform.localScale = Vector3.zero;
            transform.gameObject.SetActive(true);

            // Animation
            sequence.Append(transform.DOScale(1f, duration).SetEase(IN_BACK));

            return sequence;
        }

        /// <summary>
        /// Panel that disappears (scale to zero)
        /// </summary>
        public static Sequence PopOut(Transform transform, float duration = FAST)
        {
            Sequence sequence = DOTween.Sequence();
            
            // Animation
            sequence.Append(transform.DOScale(0f, duration).SetEase(OUT_FAST));
            sequence.OnComplete(() => transform.gameObject.SetActive(false));

            return sequence;
        }

        #endregion

        #region Button Interactions

        /// <summary>
        /// Hover on a button (scale + optional glow)
        /// </summary>
        public static void ButtonHoverEnter(Transform button, CanvasGroup glow = null, float scaleMultiplier = 1.05f)
        {
            button.DOScale(scaleMultiplier, FAST).SetEase(HOVER);
            
            if (glow != null)
                glow.DOFade(0.3f, FAST);
        }

        /// <summary>
        /// Exit hover on a button
        /// </summary>
        public static void ButtonHoverExit(Transform button, CanvasGroup glow = null)
        {
            button.DOScale(1f, FAST).SetEase(HOVER);
            
            if (glow != null)
                glow.DOFade(0f, FAST);
        }

        /// <summary>
        /// Click animation (punch scale)
        /// </summary>
        public static void ButtonClick(Transform button)
        {
            button.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 0.5f);
        }

        #endregion

        #region Loading Animations

        /// <summary>
        /// Loading spinner (infinite rotation)
        /// </summary>
        /// <param name="spinner">Transform of the spinner to animate</param>
        /// <param name="direction">Rotation direction (CounterClockwise by default)</param>
        public static Tweener StartLoadingSpinner(Transform spinner, RotationDirection direction = RotationDirection.CounterClockwise)
        {
            float rotationAngle = direction == RotationDirection.Clockwise ? -360f : 360f;
            return spinner.DORotate(new Vector3(0, 0, rotationAngle), 1f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        /// <summary>
        /// Pulse for a loading element (scale loop)
        /// </summary>
        public static Tweener StartLoadingPulse(Transform element, float scaleMin = 0.9f, float scaleMax = 1.1f)
        {
            return element.DOScale(scaleMax, 0.6f)
                .SetEase(LOOP)
                .SetLoops(-1, LoopType.Yoyo)
                .From(scaleMin);
        }

        /// <summary>
        /// Skeleton shimmer effect (moving gradient)
        /// </summary>
        public static Tweener StartShimmerEffect(RectTransform shimmerMask, float width)
        {
            shimmerMask.anchoredPosition = new Vector2(-width, 0);
            
            return shimmerMask.DOAnchorPosX(width, 1.5f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        #endregion

        #region Notifications / Toasts

        /// <summary>
        /// Toast that slides from the bottom
        /// </summary>
        public static Sequence ShowToast(RectTransform toast, float visibleY = 20f, float hiddenY = -100f, float displayDuration = 3f)
        {
            Sequence sequence = DOTween.Sequence();
            
            // Initial state
            toast.anchoredPosition = new Vector2(0, hiddenY);
            toast.gameObject.SetActive(true);

            // Slide in
            sequence.Append(toast.DOAnchorPosY(visibleY, MEDIUM).SetEase(IN_BACK));
            
            // Wait
            sequence.AppendInterval(displayDuration);
            
            // Slide out
            sequence.Append(toast.DOAnchorPosY(hiddenY, FAST).SetEase(OUT_FAST));
            sequence.OnComplete(() => Object.Destroy(toast.gameObject));

            return sequence;
        }

        /// <summary>
        /// Badge notification (pop + shake)
        /// </summary>
        public static Sequence ShowNotificationBadge(Transform badge)
        {
            Sequence sequence = DOTween.Sequence();
            
            badge.localScale = Vector3.zero;
            badge.gameObject.SetActive(true);

            // Pop
            sequence.Append(badge.DOScale(1f, MEDIUM).SetEase(IN_BACK));
            
            // Shake
            sequence.Append(badge.DOShakeRotation(0.3f, strength: 15f, vibrato: 10));

            return sequence;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Shake to indicate an error
        /// </summary>
        public static void ShakeError(Transform transform, float strength = 10f)
        {
            transform.DOShakePosition(0.3f, strength: strength, vibrato: 10, fadeOut: true);
        }

        /// <summary>
        /// Temporary highlight (flash color)
        /// </summary>
        public static void FlashHighlight(UnityEngine.UI.Image image, Color highlightColor, float duration = 0.5f)
        {
            Color originalColor = image.color;
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(image.DOColor(highlightColor, duration * 0.3f));
            sequence.Append(image.DOColor(originalColor, duration * 0.7f));
        }

        /// <summary>
        /// Fade color transition for an Image
        /// </summary>
        /// <param name="image">The Image component to animate</param>
        /// <param name="newColor">The target color to fade to</param>
        /// <param name="duration">Duration of the fade (default: NORMAL)</param>
        /// <param name="ease">Easing function (default: IN_SMOOTH)</param>
        /// <returns>The Tweener for chaining or control</returns>
        public static Tweener FadeColor(UnityEngine.UI.Image image, Color newColor, float duration = NORMAL, Ease? ease = null)
        {
            return image.DOColor(newColor, duration).SetEase(ease ?? IN_SMOOTH);
        }

        /// <summary>
        /// Fade color transition for a TextMeshPro component
        /// </summary>
        /// <param name="text">The TextMeshProUGUI component to animate</param>
        /// <param name="newColor">The target color to fade to</param>
        /// <param name="duration">Duration of the fade (default: NORMAL)</param>
        /// <param name="ease">Easing function (default: IN_SMOOTH)</param>
        /// <returns>The Tweener for chaining or control</returns>
        public static Tweener FadeColor(TMPro.TextMeshProUGUI text, Color newColor, float duration = NORMAL, Ease? ease = null)
        {
            return text.DOColor(newColor, duration).SetEase(ease ?? IN_SMOOTH);
        }

        /// <summary>
        /// Fade color transition for a SpriteRenderer
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer component to animate</param>
        /// <param name="newColor">The target color to fade to</param>
        /// <param name="duration">Duration of the fade (default: NORMAL)</param>
        /// <param name="ease">Easing function (default: IN_SMOOTH)</param>
        /// <returns>The Tweener for chaining or control</returns>
        public static Tweener FadeColor(SpriteRenderer spriteRenderer, Color newColor, float duration = NORMAL, Ease? ease = null)
        {
            return spriteRenderer.DOColor(newColor, duration).SetEase(ease ?? IN_SMOOTH);
        }

        /// <summary>
        /// Kill all animations of a GameObject
        /// </summary>
        public static void KillAllAnimations(GameObject target, bool complete = false)
        {
            target.transform.DOKill(complete);
            
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.DOKill(complete);
            
            var rectTransform = target.GetComponent<RectTransform>();
            if (rectTransform != null)
                rectTransform.DOKill(complete);
        }

        #endregion

        #region Text Animations

        /// <summary>
        /// Animated text change with typing effect (delete old, type new)
        /// </summary>
        /// <param name="textComponent">The TextMeshPro component to animate</param>
        /// <param name="newText">The new text to display</param>
        /// <param name="duration">Total duration of the animation</param>
        /// <param name="onComplete">Callback when animation completes</param>
        public static void AnimateTextChange(TMPro.TextMeshProUGUI textComponent, string newText, float duration = NORMAL, System.Action onComplete = null)
        {
            if (textComponent == null)
            {
                Debug.LogWarning("[AnimationHelper] Cannot animate text: TextComponent is null.");
                onComplete?.Invoke();
                return;
            }

            string currentText = textComponent.text;

            if (currentText == newText)
            {
                onComplete?.Invoke();
                return;
            }

            // Find common prefix
            int commonPrefixLength = 0;
            int minLength = Mathf.Min(currentText.Length, newText.Length);

            for (int i = 0; i < minLength; i++)
            {
                if (currentText[i] != newText[i])
                    break;
                commonPrefixLength++;
            }

            int charsToDelete = currentText.Length - commonPrefixLength;

            // If characters need to be deleted
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

                    // Then add new characters
                    float addDuration = duration * 0.5f;

                    DOVirtual.Float(commonPrefixLength, newText.Length, addDuration, (value) =>
                    {
                        if (textComponent == null) return;
                        
                        int charCount = Mathf.FloorToInt(value);
                        textComponent.text = newText.Substring(0, charCount);
                    })
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (textComponent != null)
                            textComponent.text = newText;
                        onComplete?.Invoke();
                    });
                });

                return;
            }

            // Only add new characters
            float typewriterDuration = duration;

            DOVirtual.Float(commonPrefixLength, newText.Length, typewriterDuration, (value) =>
            {
                if (textComponent == null) return;
                
                int charCount = Mathf.FloorToInt(value);
                textComponent.text = newText.Substring(0, charCount);
            })
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (textComponent != null)
                    textComponent.text = newText;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Simple typewriter effect from empty to full text
        /// </summary>
        /// <param name="textComponent">The TextMeshPro component to animate</param>
        /// <param name="text">The text to display</param>
        /// <param name="duration">Duration of the typewriter effect</param>
        /// <param name="onComplete">Callback when animation completes</param>
        public static void TypewriterEffect(TMPro.TextMeshProUGUI textComponent, string text, float duration = NORMAL, System.Action onComplete = null)
        {
            if (!textComponent)
            {
                Debug.LogWarning("[AnimationHelper] Cannot start typewriter: TextComponent is null.");
                onComplete?.Invoke();
                return;
            }

            textComponent.text = "";

            DOVirtual.Float(0, text.Length, duration, (value) =>
            {
                if (!textComponent)
                    return;
                
                int charCount = Mathf.FloorToInt(value);
                textComponent.text = text.Substring(0, charCount);
            })
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (textComponent)
                    textComponent.text = text;
                onComplete?.Invoke();
            });
        }

        #endregion
    }
}

