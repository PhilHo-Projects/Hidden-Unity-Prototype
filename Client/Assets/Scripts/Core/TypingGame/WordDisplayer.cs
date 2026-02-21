using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Core.Singletons;
using DG.Tweening;
using UnityEngine.UI;

namespace Core
{
    public class WordDisplayer : MonoBehaviour
    {
        private const string ColorYellow = "#C8C676";
        private const string ColorGrey = "#37393C";
        private const string ColorRed = "#D7444C";
        private const string ColorGreen = "#4CAE54";
        private static readonly Color ColorDarkYellow = new Color(0xE2/255f, 0xB7/255f, 0x14/255f);


        [SerializeField] private TextMeshProUGUI sampleText;
        [SerializeField] private TextMeshProUGUI countdownTimerText;
        [SerializeField] private TextMeshProUGUI wpmText;
        [SerializeField] private RectTransform cursorRect;
        [SerializeField] private CanvasGroup cursorCanvasGroup;
        [SerializeField] private WordListScriptableObject wordList;
        [SerializeField] private float initialCountdowntimer = 30f;

        [SerializeField] private Button startSimulationButton;
        [SerializeField] private Image startSimulationImage;
        [SerializeField] private Button resetMatchButton;
        [SerializeField] private Button returnToMenuButton;


        private List<string> _words;
        private int _currentWordIndex;
        private string _currentWord;
        private readonly List<char> _typedChars = new List<char>();
        
        private Sequence _blinkSequence;
        private bool _isFirstWord = true;
        private Vector3 _lastCursorPosition;
        private float _countdownTimer = 30f;
        
        private int _completedWordsCharCount = 0;
        private int _completedWordsCount = 0;
        
        private float _simulatedTypingSpeed = 16f;
        private float _lastSimulatedTypeTime = 0f;
        
        private bool _simulating;
        private bool _matchStarted;

        private void Start()
        {
            if (wordList == null || sampleText == null || cursorRect == null)
            {
                Debug.LogError("WordList, TMP_Text component, or cursor is not assigned!");
                return;
            }
            _words = wordList.words.ToList();
            _currentWord = _words[_currentWordIndex];
            _countdownTimer = initialCountdowntimer;
                                    
            InputManager.Instance.InputActionHandlers["AnyKey"].Performed += ctx => 
            {
                _matchStarted = true;
                InputManager.Instance.ClearInputActionSubscribers("AnyKey", InputManager.InputEventType.Performed);
            };
            DisplayWords();
            InitializeCursorBlink();
            Invoke(nameof(InitialCursorPositioning), 0.5f);
            SetupButtons();


        }

        private void SetupButtons()
        {
            startSimulationButton.onClick.AddListener(() =>
            {
                _matchStarted = true;
                _simulating = !_simulating;
                if (_simulating)
                {
                    startSimulationImage.color = Color.green;
                }
                else
                {
                    startSimulationImage.color = ColorDarkYellow;
                }
            });
            
            resetMatchButton.onClick.AddListener(ResetMatch);
            returnToMenuButton.onClick.AddListener(() =>
            {
                DOTween.KillAll();
                InputManager.Instance.ClearInputActionSubscribers("AnyKey", InputManager.InputEventType.Performed);
                SceneLoader.Instance.LoadScene("Intro");
            });
        }

        private void InitialCursorPositioning()
        {
            UpdateCursorPosition(true);
        }
        
        private void Update()
        {
            if (!_matchStarted) return;
    
            HandleInput();
            CountdownTimerText();
            CalculateTypeRacerStyle();
    
            if (_simulating)
                SimulateTyping();
        }


        private void HandleInput()
        {
            if (!Input.anyKeyDown) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (IsCurrentWordComplete())
                {
                    MoveToNextWord();
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                HandleBackspace();
                return;
            }

            HandleAlphaNumericInput();
        }

        private void HandleAlphaNumericInput()
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                {
                    string keyString = kcode.ToString();
                    if (keyString.Length == 1 && keyString != " ")
                    {
                        ProcessTypedChar(keyString.ToLower()[0]);
                        break;
                    }
                }
            }
        }

        private void DisplayWords()
        {
            string displayText = "";
    
            for (int i = 0; i < _words.Count; i++)
            {
                if (i > 0) displayText += " ";
        
                if (i < _currentWordIndex)
                    displayText += $"<color={ColorGreen}>{_words[i]}</color>";
                else if (i == _currentWordIndex)
                    displayText += GetColoredCurrentWord();
                else
                    displayText += $"<color={ColorGrey}>{_words[i]}</color>";
            }
    
            sampleText.text = displayText;
        }

        private bool IsCurrentWordComplete()
        {
            if (_typedChars.Count != _currentWord.Length) return false;
            
            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (_typedChars[i] != _currentWord[i]) return false;
            }
            return true;
        }
        
        private string GetColoredCurrentWord()
        {
            string coloredWord = "";
            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (i < _typedChars.Count)
                {
                    if (_typedChars[i] == _currentWord[i])
                        coloredWord += $"<color={ColorYellow}>{_currentWord[i]}</color>";
                    else
                        coloredWord += $"<color={ColorRed}>{_currentWord[i]}</color>";
                }
                else
                {
                    coloredWord += $"<color={ColorGrey}>{_currentWord[i]}</color>";
                }
            }
            return coloredWord;
        }

        private void InitializeCursorBlink()
        {
            _blinkSequence?.Kill();
    
            _blinkSequence = DOTween.Sequence();
            _blinkSequence.Append(cursorCanvasGroup.DOFade(0f, 0.53f))
                .Append(cursorCanvasGroup.DOFade(1f, 0.53f))
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        private void StopBlinking()
        {
            _blinkSequence?.Kill();
            cursorCanvasGroup.alpha = 1f;
        }
        
        private void UpdateCursorPosition(bool immediate = false)
        {
            TMP_TextInfo textInfo = sampleText.textInfo;
            if (textInfo.characterCount == 0) return;

            int charIndex = _currentWordIndex > 0 
                ? _words.Take(_currentWordIndex).Sum(w => w.Length + 1) + _typedChars.Count
                : _typedChars.Count;

            if (charIndex >= textInfo.characterCount)
                charIndex = textInfo.characterCount - 1;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

            int lineIndex = charInfo.lineNumber;

            Vector3 targetPos = sampleText.transform.TransformPoint(new Vector3(charInfo.topLeft.x, textInfo.lineInfo[lineIndex].baseline, 0));

            if (immediate || _lastCursorPosition == Vector3.zero)
            {
                cursorRect.position = targetPos;
                _lastCursorPosition = targetPos;
            }
            else
            {
                DOTween.Kill(cursorRect);
                cursorRect.DOMove(targetPos, 0.1f).SetEase(Ease.OutQuad).OnComplete(() => _lastCursorPosition = cursorRect.position);
            }
        }
        
        private void ProcessTypedChar(char typedChar)
        {
            if (_isFirstWord && _typedChars.Count == 0)
            {
                StopBlinking();
                _isFirstWord = false;
            }

            if (_typedChars.Count < _currentWord.Length)
            {
                _typedChars.Add(typedChar);
                DisplayWords();
                UpdateCursorPosition();
            }
        }
        
        private void MoveToNextWord()
        {
            if (IsCurrentWordComplete())
            {
                _completedWordsCharCount += _currentWord.Length;
                _completedWordsCount++;
            }
            
            if (_currentWordIndex < _words.Count - 1)
            {
                _currentWordIndex++;
                _currentWord = _words[_currentWordIndex];
                _typedChars.Clear();
                DisplayWords();
                UpdateCursorPosition();
            }
        }

        private void HandleBackspace()
        {
            if (_typedChars.Count > 0)
            {
                _typedChars.RemoveAt(_typedChars.Count - 1);
                DisplayWords();
                UpdateCursorPosition();
            
                if (_typedChars.Count == 0 && _isFirstWord)
                {
                    InitializeCursorBlink();
                }
            }
        }
        
        private void CountdownTimerText()
        {
            if (_countdownTimer <= 0)
                return;
            _countdownTimer -= Time.deltaTime;
            countdownTimerText.text = _countdownTimer.ToString("0.0");
        }
        
        private void CalculateTypeRacerStyle()
        {
            float timeElapsed = (30f - _countdownTimer) / 60f; // Convert to minutes
            if (timeElapsed <= 0 || _completedWordsCount == 0) return;

            float averageWordLength = _completedWordsCharCount / (float)_completedWordsCount;
            float wpm = (_completedWordsCharCount / averageWordLength) / timeElapsed;

            wpmText.text = $"{Mathf.Round(wpm)} WPM";
        }
        private void SimulateTyping()
        {
            if (_countdownTimer <= 0) return;
    
            if (Time.time - _lastSimulatedTypeTime >= 1f/_simulatedTypingSpeed)
            {
                _lastSimulatedTypeTime = Time.time;

                // If current word is complete, simulate space press
                if (_typedChars.Count == _currentWord.Length)
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                        return;
                
                    if (IsCurrentWordComplete())
                    {
                        MoveToNextWord();
                    }
                }
                // Otherwise type the next character
                else if (_typedChars.Count < _currentWord.Length)
                {
                    ProcessTypedChar(_currentWord[_typedChars.Count]);
                }
            }
        }

        private void ResetMatch()
        {
            _currentWordIndex = 0;
            _currentWord = _words[_currentWordIndex];
            _typedChars.Clear();
            _completedWordsCharCount = 0;
            _completedWordsCount = 0;
            _isFirstWord = true;
            _countdownTimer = 30f;
            _simulating = false;
            _matchStarted = false;
    
            DisplayWords();
            InitializeCursorBlink();
            Invoke(nameof(InitialCursorPositioning), 0.1f);

            // Reset UI elements
            wpmText.text = "0 WPM";
            countdownTimerText.text = "30.0";
            
            InputManager.Instance.InputActionHandlers["AnyKey"].Performed += ctx => 
            {
                _matchStarted = true;
                InputManager.Instance.ClearInputActionSubscribers("AnyKey", InputManager.InputEventType.Performed);
            };
        }

        private void OnDestroy()
        {
            resetMatchButton.onClick.RemoveAllListeners();
            startSimulationButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.RemoveAllListeners();
            DOTween.KillAll();
        }
    }
}
