using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class ConsoleLogManager : IndestructibleSingletonBehaviour<ConsoleLogManager>
    {
        private bool _isConsoleEnabled = true;
        private KeyCode _toggleKey = KeyCode.BackQuote;
        
        private readonly Queue<DebugMessage> _messageQueue = new Queue<DebugMessage>();
        private int _maxMessages = 5;
        private float _messageDuration = 30f;

        // GUI Position control
        private Rect _windowRect;
        private Vector2 _scrollPosition;
        private bool _isDraggable = true;
        private GUIStyle _windowStyle;
        private bool _isStyleInitialized = false;
        private bool _isCollapsed = false;

        // Constants for window positioning and sizing
        private const float COLLAPSED_HEIGHT = 30f;
        private const float COLLAPSED_WIDTH = 150f;
        private readonly Vector2 COLLAPSED_POSITION = new Vector2(10, Screen.height - 40);

        private class DebugMessage
        {
            public string Text;
            public float TimeStamp;

            public DebugMessage(string text)
            {
                Text = text;
                TimeStamp = Time.time;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            InitializeWindowPosition();
        }

        private void InitializeWindowPosition()
        {
            float defaultWidth = Screen.width / 3;
            float defaultHeight = Screen.height / 3;
            float centerX = (Screen.width - defaultWidth) / 2;
            float centerY = (Screen.height - defaultHeight) / 2;
            
            _windowRect = new Rect(centerX, centerY, defaultWidth, defaultHeight);
            ToggleCollapse();
        }

        void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleConsole();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Log("Test message at " + Time.time);
            }
        }

        private void OnGUI()
        {
            if (!_isConsoleEnabled) return;

            if (!_isStyleInitialized)
            {
                InitializeGUIStyle();
            }

            ClampWindowToBounds();
            _windowRect = GUI.Window(0, _windowRect, DrawConsoleWindow, "Console", _windowStyle);
        }

        private void ToggleCollapse()
        {
            _isCollapsed = !_isCollapsed;
            
            if (_isCollapsed)
            {
                // Store current position before collapsing
                _windowRect = new Rect(
                    COLLAPSED_POSITION.x,
                    COLLAPSED_POSITION.y,
                    COLLAPSED_WIDTH,
                    COLLAPSED_HEIGHT
                );
            }
            else
            {
                // Return to center
                float centerX = (Screen.width - Screen.width / 3) / 2;
                float centerY = (Screen.height - Screen.height / 3) / 2;
                _windowRect = new Rect(
                    centerX,
                    centerY,
                    Screen.width / 3,
                    Screen.height / 3
                );
            }
        }

        private void DrawConsoleWindow(int windowID)
        {
            if (GUI.Button(new Rect(_windowRect.width - 25, 5, 20, 20), _isCollapsed ? "+" : "-"))
            {
                ToggleCollapse();
            }

            if (!_isCollapsed)
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                var currentMessages = _messageQueue.ToList();
                foreach (var message in currentMessages)
                {
                    if (Time.time - message.TimeStamp > _messageDuration)
                    {
                        _messageQueue.Dequeue();
                        continue;
                    }

                    GUILayout.Box(message.Text);
                }

                GUILayout.EndScrollView();
            }

            if (_isDraggable && !_isCollapsed)
            {
                GUI.DragWindow();
            }
        }

        private void ClampWindowToBounds()
        {
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
        }

        // Console functions
        public void ToggleConsole()
        {
            _isConsoleEnabled = !_isConsoleEnabled;
        }
        
        private void LogMessage(string message)
        {
            _messageQueue.Enqueue(new DebugMessage(message));
            if (_messageQueue.Count > _maxMessages)
                _messageQueue.Dequeue();
        }
        
        public void Log(string message)
        {
            if (Instance != null)
            {
                Instance.LogMessage(message);
            }
        }
        
        private void InitializeGUIStyle()
        {
            if (_isStyleInitialized) return;
        
            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.fontSize = 12;
            _windowStyle.normal.textColor = Color.white;
            _windowStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));
        
            _isStyleInitialized = true;
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // Configuration methods
        public void SetMaxMessages(int max)
        {
            _maxMessages = max;
        }

        public void SetMessageDuration(float duration)
        {
            _messageDuration = duration;
        }

        public void SetDraggable(bool draggable)
        {
            _isDraggable = draggable;
        }
    }
}
