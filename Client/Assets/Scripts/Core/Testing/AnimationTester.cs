using UnityEngine;
using TMPro;
using DG.Tweening;
using Core.Utility;

public class AnimationTester : MonoBehaviour
{
    public TMP_Text countdownText;
    public TMP_Text typewriterText;
    public Transform objectToShake;
    public Transform objectToFloat;
    public Transform objectToBreathe;
    
    [TextArea(3, 5)]
    public string[] countdownStrings = new string[] { "3", "2", "1", "GO!" };
    
    private DoTweenSimpleAnimations animations;
    
    void Start()
    {
        animations = new DoTweenSimpleAnimations();
        DOTween.SetTweensCapacity(500, 50); // Increase capacity for complex scenes

        TestBreathing();
        TestCountdown();
        TestTypewriter();
        TestShake();
    }
    
    public void TestCountdown()
    {
        animations.Countdown(countdownText, countdownStrings, 
            onComplete: () => Debug.Log("Countdown complete!"));
    }
    
    public void TestTypewriter()
    {
        animations.Typewriter(typewriterText, "This is a typewriter effect that types out text character by character.");
    }
    
    public void TestShake()
    {
        // For uniform shake
        animations.ShakeTransform(objectToShake);

        // For directional shake (stronger on Y axis for example)
        animations.ShakeTransformDirectional(objectToShake, new Vector3(0.5f, 2f, 0.5f));    
    }
    
    public void TestFloating()
    {
        animations.FloatingAnimation(objectToFloat);
    }
    
    public void TestBreathing()
    {
        animations.CreateBreathingAnimation(objectToBreathe);
    }
}
