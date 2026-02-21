using UnityEngine;
using DG.Tweening;
using TMPro;
using System;
using static Core.Hidden.HiddenGameGlobals;
using UnityEngine.UI;

namespace Core.Utility
{
    public class DoTweenSimpleAnimations
    {
        private bool _isWarningAnimationPlaying;
        private Sequence _blinkSequence;
        
        private static readonly Color[] RainbowColors = new Color[] {
            new Color(1, 0.3f, 0.3f),    // Soft Red
            new Color(1, 0.6f, 0.2f),    // Soft Orange
            new Color(1, 1, 0.3f),       // Soft Yellow
            new Color(0.3f, 1, 0.3f),    // Soft Green
            new Color(0.3f, 0.7f, 1),    // Soft Blue
            new Color(0.7f, 0.3f, 1)     // Soft Purple
        };

        private void ForEachTextElement(object textElement, Action<TMP_Text> action)
        {
            if (textElement == null) return;
            
            if (textElement is TMP_Text singleText)
            {
                action(singleText);
            }
            else if (textElement is TMP_Text[] textArray)
            {
                foreach (var txt in textArray)
                {
                    if (txt != null) action(txt);
                }
            }
        }

        private void KillTweens(Transform target)
        {
            DOTween.Kill(target);
        }

        public void PopText(object textElement, string message, float scaleDuration = 0.2f, float scaleDownDuration = 0.1f, float scaleAmount = 1.2f, bool clearAfterDelay = false, float clearDelay = 1.5f)
        {
            ForEachTextElement(textElement, (txt) => {
                txt.text = message;
                txt.transform.localScale = Vector3.one;

                Sequence sequence = DOTween.Sequence();
                sequence.Append(txt.transform.DOScale(scaleAmount, scaleDuration))
                    .Append(txt.transform.DOScale(1f, scaleDownDuration))
                    .SetEase(Ease.OutBack);
        
                if (clearAfterDelay)
                {
                    DOVirtual.DelayedCall(clearDelay, () => txt.text = "");
                }
            });
        }
        
        public void ShowFadingText(string message, object textElement, float fadeInDuration = 0.5f, float displayDuration = 1f, float fadeOutDuration = 0.5f)
        {
            ForEachTextElement(textElement, (txt) => {
                AnimateSingleText(message, txt, fadeInDuration, displayDuration, fadeOutDuration);
            });
        }
        
        private void AnimateSingleText(string message, TMP_Text textElement, float fadeInDuration, float displayDuration, float fadeOutDuration)
        {
            textElement.alpha = 0f;
            textElement.text = message;

            Sequence fadeSequence = DOTween.Sequence();
            fadeSequence.Append(textElement.DOFade(1f, fadeInDuration))
                .AppendInterval(displayDuration)
                .Append(textElement.DOFade(0f, fadeOutDuration));
        }
        
        public void CreateBreathingAnimation(Transform target, float scalePulse = 1.05f, float duration = 1.75f)
        {
            KillTweens(target);
            target.localScale = Vector3.one;
            
            Sequence breathingSequence = DOTween.Sequence();
            breathingSequence.Append(target.DOScale(scalePulse, duration * 0.6f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(1f, duration * 0.4f).SetEase(Ease.InOutQuad))
                .SetLoops(-1);
        }

        public void Countdown(TMP_Text textElement, string[] countdownTexts, float fadeInDuration = 0.3f, float displayDuration = 0.7f, float fadeOutDuration = 0.3f, float popScale = 1.3f, Action onComplete = null)
        {
            if (textElement == null || countdownTexts == null || countdownTexts.Length == 0)
                return;
                
            KillTweens(textElement.transform);
            textElement.alpha = 0f;
            
            Sequence countdownSequence = DOTween.Sequence();
            countdownSequence.SetTarget(textElement);
            
            for (int i = 0; i < countdownTexts.Length; i++)
            {
                int index = i;
                countdownSequence.AppendCallback(() => textElement.text = countdownTexts[index]);
                
                Sequence numberSequence = DOTween.Sequence();
                numberSequence.Append(textElement.DOFade(1f, fadeInDuration))
                    .Join(textElement.transform.DOScale(popScale, fadeInDuration).SetEase(Ease.OutBack))
                    .AppendInterval(displayDuration)
                    .Append(textElement.DOFade(0f, fadeOutDuration))
                    .Join(textElement.transform.DOScale(1f, fadeOutDuration).SetEase(Ease.InBack));
                
                countdownSequence.Append(numberSequence);
            }
            
            if (onComplete != null)
                countdownSequence.OnComplete(() => onComplete.Invoke());
        }
        
        public void Typewriter(TMP_Text textElement, string fullText, float typingSpeed = 0.05f, float initialDelay = 0.1f, Action onComplete = null)
        {
            if (textElement == null) return;
                
            DOTween.Kill(textElement);
            textElement.text = "";
            
            Sequence typeSequence = DOTween.Sequence();
            
            if (initialDelay > 0)
                typeSequence.AppendInterval(initialDelay);
                
            typeSequence.AppendCallback(() => textElement.maxVisibleCharacters = 0);
            typeSequence.Append(DOTween.To(() => textElement.maxVisibleCharacters, 
                x => textElement.maxVisibleCharacters = x, fullText.Length, fullText.Length * typingSpeed)
                .OnUpdate(() => {
                    if (textElement.text != fullText)
                        textElement.text = fullText;
                }));
                
            if (onComplete != null)
                typeSequence.OnComplete(() => onComplete.Invoke());
        }
        
        public void ShakeTransform(Transform target, float duration = 0.5f, float strength = 1f, int vibrato = 10, 
            float randomness = 90f, bool snapping = false, bool fadeOut = true, 
            ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Full)
        {
            if (target == null) return;
            
            KillTweens(target);
            target.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut, randomnessMode);
        }

        public void ShakeTransformDirectional(Transform target, Vector3 strength, float duration = 0.5f, int vibrato = 10, 
            float randomness = 90f, bool snapping = false, bool fadeOut = true, 
            ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Full)
        {
            if (target == null) return;
            
            KillTweens(target);
            target.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut, randomnessMode);
        }
        
        public void FloatingAnimation(Transform target, float yOffset = 0.5f, float duration = 1f, bool loop = true)
        {
            if (target == null) return;
                
            KillTweens(target);
            Vector3 startPos = target.position;
            
            Sequence floatSequence = DOTween.Sequence();
            floatSequence.Append(target.DOMoveY(startPos.y + yOffset, duration/2).SetEase(Ease.InOutSine))
                .Append(target.DOMoveY(startPos.y, duration/2).SetEase(Ease.InOutSine));
                
            if (loop)
                floatSequence.SetLoops(-1);
        }
        
        public void PopThenBreathe(Transform target, float popScale = 1.3f, float popDuration = 0.2f, 
            float returnDuration = 0.1f, float breathScale = 1.1f, float breathDuration = 0.8f)
        {
            if (target == null) return;
        
            KillTweens(target);
            target.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOScale(popScale, popDuration).SetEase(Ease.OutBack))
                .Append(target.DOScale(1f, returnDuration))
                .AppendCallback(() => {
                    CreateBreathingAnimation(target, breathScale, breathDuration);
                });
        }
        
        public void RainbowText(string message, object textElement, float fadeInDuration = 0.5f, float displayDuration = 2f, float fadeOutDuration = 0.5f)
        {
            ForEachTextElement(textElement, (txt) => {
                AnimateRainbowText(message, txt, fadeInDuration, displayDuration, fadeOutDuration);
            });
        }

        private void AnimateRainbowText(string message, TMP_Text textElement, float fadeInDuration, float displayDuration, float fadeOutDuration)
        {
            Color originalColor = textElement.color;
            textElement.alpha = 0f;
            textElement.text = message;
            
            Sequence rainbowSequence = DOTween.Sequence();
            rainbowSequence.Append(textElement.DOFade(1f, fadeInDuration));
            
            float transitionTime = displayDuration / RainbowColors.Length;
            foreach (Color color in RainbowColors)
            {
                rainbowSequence.Append(textElement.DOColor(color, transitionTime));
            }
            
            rainbowSequence.Append(textElement.DOFade(0f, fadeOutDuration))
                           .AppendCallback(() => {
                                textElement.color = originalColor;
                                textElement.text = "";
                           });
        }

        public void UpdateTimerWithEffects(Image timerFill, float currentTime, float maxTime, float warningThreshold = 2f)
        {
            timerFill.fillAmount = Mathf.Clamp01(currentTime / maxTime);

            if (currentTime <= 0)
            {
                ResetTimerVisuals(timerFill);
                return;
            }

            if (currentTime <= warningThreshold && !_isWarningAnimationPlaying)
            {
                StartWarningAnimation(timerFill);
            }
        }

        private void StartWarningAnimation(Image timerFill)
        {
            timerFill.color = ColorRedBlinker;
            DOTween.Kill(timerFill);
            
            if (_blinkSequence != null)
            {
                _blinkSequence.Kill();
                _blinkSequence = null;
            }

            _blinkSequence = DOTween.Sequence();
            _blinkSequence.Append(timerFill.DOFade(0.5f, 0.25f))
                .Append(timerFill.DOFade(1f, 0.25f))
                .SetLoops(-1);

            _isWarningAnimationPlaying = true;
        }
        
        public void ResetTimerVisuals(Image timerFill)
        {
            DOTween.Kill(timerFill, true);
            DOTween.Kill(timerFill.transform, true);
            
            if (_blinkSequence != null)
            {
                _blinkSequence.Kill(true);
                _blinkSequence = null;
            }
            
            _isWarningAnimationPlaying = false;
            timerFill.color = ColorYellowBlinker;
            timerFill.fillAmount = 1f;
            timerFill.DOFade(1f, 0f);
            
            DOVirtual.DelayedCall(0.01f, () => {
                timerFill.color = ColorYellowBlinker;
            });
        }

        public void ShieldBouncyAppearance(Transform target)
        {
            if (target == null) return;

            KillTweens(target);
            target.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOScale(1.3f, 0.2f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(0.8f, 0.1f).SetEase(Ease.InQuad))
                .Append(target.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
        }
        
        public void ShieldBouncyDisappearance(Transform target)
        {
            if (target == null) return;

            KillTweens(target);
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad))
                .Append(target.DOScale(0f, 0.2f).SetEase(Ease.InQuad))
                .OnComplete(() => {
                    if (target != null && target.gameObject != null)
                        target.gameObject.SetActive(false);
                });
        }
    }
}
