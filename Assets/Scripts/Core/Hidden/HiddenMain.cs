using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Singletons;
using DG.Tweening;
using Core.WebSocket;
using static Core.Hidden.HiddenGameGlobals;

namespace Core.Hidden
{
    public class HiddenMain : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField] private PowerUps powerUps;
        [SerializeField] private GameGUI gameGUI;
        
        [SerializeField] private Button[] gridButtons = new Button[9];
        // [SerializeField] private Button[] colorChoosingButtons = new Button[3];

        [SerializeField] private Image[] gridButtonImages = new Image[9];
        //[SerializeField] private Image[] otherBoard = new Image[9];
        
        [SerializeField] private Button readyButtonAI;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button replayButton;

        #endregion
        
        #region Member Variables

        // Core variables
        private WebSocketNetworkHandler _wsHandler;
        private GameManager _gameManager;
        private Automated _ai;
        private Network _networkHandler;
        public Network NetworkHandler  => _networkHandler;

        private GameState _gameState;
        private bool _playerIsReady;
        private float _turnTimer;
        private int _countdownTimer;
        private Coroutine _timerCoroutine;
        private int _numberOfRounds;
        private bool _playAgainstAI;
        private bool _isOnline;
        
        // Grid data
        private GridData _playerGrid;
        private GridData _opponentGrid;
        //private string _currentPaintColor;
        private int _currentRound;
        private int _totalTurns;
        private int _maxTurns;
        private bool _shieldSelectionMode;
        public bool ShieldSelectionMode { get => _shieldSelectionMode; set => _shieldSelectionMode = value; }
        private bool _isMyTurn = true;
        public bool IsMyTurn => _isMyTurn;
        
        // Extra turns
        private readonly List<int> _extraTurnMoves = new List<int>();
        private readonly List<string> _extraTurnColors = new List<string>();
        private bool _isInExtraTurn;
        
        // Cursor
        private Texture2D _cursorTexture;
        private Vector2 _cursorHotspot;

        #endregion
        
        #region Core Game Functions
        
        private void Awake()
        {
            _gameManager = GameManager.Instance;
            _gameManager.VFX.InitializePool();
            _wsHandler = WebSocketNetworkHandler.Instance;
            _wsHandler.HiddenGame = this;

            _ai = new Automated(this);
            _networkHandler = new Network(this, gameGUI, _wsHandler);
        }

        private void Start()
        {
            _playAgainstAI = _gameManager.playingAgainstAI;
            _isOnline = _gameManager.isOnline;
            _numberOfRounds = _gameManager.numberOfRounds;
            Debug.Log(_numberOfRounds);

            InitializeGameState();
            InitButtons();
            
            _gameState = gameGUI.StateChange(_playAgainstAI ? GameState.SetupAI : GameState.Setup, false);
            
            //if (_playAgainstAI) StartGameAI();
            //if (_playAgainstAI) gameGUI.ToggleOfflineCanvas(true);
        }
        
        private void InitializeGameState()
        {
            _currentRound = 1;
            _totalTurns = 0;
            _maxTurns = _numberOfRounds * 2;
            _turnTimer = _gameManager.timer;
            // _currentPaintColor = "";
            gameGUI.CurrentPaintColor = "";
            InitializeGrids();
            powerUps.InitializePowerups();
        }

        private void StartGame(bool isMyTurn)
        {
            _isMyTurn = isMyTurn;
            DOTween.KillAll();
    
            if (_isMyTurn) _gameManager.TextAnimations.PopText(gameGUI.GetText(TMPTextType.PlayerTurn), "You start!");
            else _gameManager.TextAnimations.Typewriter(gameGUI.GetText(TMPTextType.PlayerTurn), "Waiting...");
    
            _gameState = gameGUI.StateChange(GameState.Battle);
            if (_isMyTurn) _timerCoroutine = StartCoroutine(TimerCoroutine());
        }


        private void StartGameAI()
        {
            readyButtonAI.gameObject.SetActive(false);
            gameGUI.ToggleOfflineCanvas(false);
            _gameState = gameGUI.StateChange(GameState.Battle);
            _isMyTurn = true;
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }
        
        private void ResetGame()
        {
            InitializeGameState();
            gameGUI.ResetGUI();
            readyButton.gameObject.SetActive(true);
            _gameState = gameGUI.StateChange(GameState.Setup);
            _gameManager.TextAnimations.Typewriter(gameGUI.GetText(TMPTextType.Countdown), "Press Ready to go again");
            if (_playAgainstAI) StartGame(true);
        }

        public void QuitGame()
        {
            _networkHandler.Cleanup();
            DOTween.KillAll();
            SceneLoader.Instance.LoadScene("Intro");
        }
        private void EndGame()
        {
            if(_timerCoroutine != null) StopCoroutine(_timerCoroutine);   
            StartCoroutine(DelayedEndGame());
        }
        private IEnumerator DelayedEndGame()
        {
            // Wait a short time to ensure all conflicts are resolved
            yield return new WaitForSeconds(1.1f);

            int playerScore = _playerGrid.Marks.Count(mark => mark);
            int opponentScore = _opponentGrid.Marks.Count(mark => mark);

            _gameState = gameGUI.StateChange(GameState.EndGame);
    
            gameGUI.PopulateEndGameGrids(_playerGrid, _opponentGrid);
            gameGUI.DisplayGameResults(playerScore, opponentScore);
        }
        
        #endregion
        
        #region Initializers

        private void InitializeGrids()
        {
            _playerGrid.Marks = new bool[9];
            _playerGrid.Color = new string[9];
            _playerGrid.Immune = new bool[9];

            _opponentGrid.Marks = new bool[9];
            _opponentGrid.Color = new string[9];
            _opponentGrid.Immune = new bool[9];
        }

        private void InitButtons()
        {
            readyButtonAI.onClick.AddListener(StartGameAI);
            readyButton.onClick.AddListener(Ready);
            replayButton.onClick.AddListener(ResetGame);

            for (int i = 0; i < gridButtons.Length; i++)
            {
                int buttonIndex = i;
                gridButtons[i].onClick.AddListener(() => OnGridButtonClick(gridButtons[buttonIndex]));
            }
        }
        
        #endregion

        #region Game Visuals

        // private void OnColorButtonClick(int colorIndex)
        // {
        //     string selectedColor = "";
        //     switch (colorIndex)
        //     {
        //         case 0:
        //             selectedColor = ColorGreenSelect;
        //             gameGUI.UpdateCursor(0);
        //             break;
        //         case 1:
        //             selectedColor = ColorBlueSelect;
        //             gameGUI.UpdateCursor(1);
        //             break;
        //         case 2:
        //             selectedColor = ColorRedSelect;
        //             gameGUI.UpdateCursor(2);
        //             break;
        //     }
        //     gameGUI.CurrentPaintColor = selectedColor;
        // }

        #endregion
        
        #region Core Mechanics
        private void OnGridButtonClick(Button clickedButton)
        {
            int buttonIndex = Array.IndexOf(gridButtons, clickedButton);

            // Handle shield selection mode
            if (_shieldSelectionMode)
            {
                ApplyShield(buttonIndex);
                return;
            }

            // Regular move logic
            if (_playerGrid.Marks[buttonIndex])
            {
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TMPTextType.Announcement), "Spot taken!", 0.15f, 0.1f, 1.15f);
                return;
            }

            if (!string.IsNullOrEmpty(gameGUI.CurrentPaintColor) && _isMyTurn)
            {
                ApplyMove(buttonIndex, gameGUI.CurrentPaintColor);
            }
        }

        public void ApplyMove(int position, string color)
        {
            if (_playerGrid.Marks[position] || string.IsNullOrEmpty(color) || !_isMyTurn)
                return;
        
            _playerGrid.Marks[position] = true;
            _playerGrid.Color[position] = color;
    
            gameGUI.ChangeButtonColor(position, color);
            gameGUI.ResetAllButtonStates();
            gameGUI.CurrentPaintColor = "";
            gameGUI.ResetCursor();
            powerUps.ResetBluePowerUp();
    
            StartCoroutine(ProcessTurn(position, true));
        }

        public void ApplyShield(int position)
        {
            if (!_playerGrid.Marks[position] || string.IsNullOrEmpty(_playerGrid.Color[position]))
            {
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TMPTextType.Announcement), "Can only shield your pieces!", 0.15f, 0.1f, 1.15f);
                return;
            }
        
            _playerGrid.Immune[position] = true;
            gameGUI.ShowShieldVisual(position);
            
            if (_isOnline) _networkHandler.SendImmuneStatus((byte)position);
            _gameManager.TextAnimations.PopText(gameGUI.GetText(TMPTextType.Announcement), "Piece shielded!", 0.15f, 0.1f, 1.15f);
    
            _shieldSelectionMode = false;
            powerUps.OnShieldApplied();
        }
        
        private IEnumerator ProcessTurn(int buttonIndex, bool isLocalPlayerMove)
        {
            // Step 1: Handle initial game state updates
            string originalColor = _playerGrid.Color[buttonIndex];
            yield return ProcessMoveConflicts(buttonIndex, isLocalPlayerMove);
            TrackMoveForExtraTurn(buttonIndex, originalColor, isLocalPlayerMove);
    
            // Step 2: Check for patterns and special effects
            yield return StartCoroutine(CheckPatternsCoroutine());
    
            // Step 3: Handle turn transition
            bool extraTurn = HandleExtraTurnPowerUp(isLocalPlayerMove);
    
            if (extraTurn)
            {
                HandleExtraTurnSpecifically();
            }
            else
            {
                SendNetworkMoves(buttonIndex, originalColor, isLocalPlayerMove);
                IncrementTurnCounter();
        
                if (IsGameOver())
                {
                    EndGame();
                    yield break;
                }
        
                CleanupBeforeTurnChange();
                ChangeTurn();
                TriggerAIIfNeeded(isLocalPlayerMove);
            }
        }

        // Helper methods to break down the complexity
        private IEnumerator ProcessMoveConflicts(int buttonIndex, bool isLocalPlayerMove)
        {
            if ((isLocalPlayerMove && _opponentGrid.Marks[buttonIndex]) || (!isLocalPlayerMove && _playerGrid.Marks[buttonIndex]))
            {
                yield return ResolveConflict(buttonIndex);
            }
        }

        private void TrackMoveForExtraTurn(int buttonIndex, string color, bool isLocalPlayerMove)
        {
            if (!isLocalPlayerMove) return;
            
            if (_isInExtraTurn)
            {
                _extraTurnMoves.Add(buttonIndex);
                _extraTurnColors.Add(color);
            }
            else
            {
                _extraTurnMoves.Clear();
                _extraTurnColors.Clear();
                _extraTurnMoves.Add(buttonIndex);
                _extraTurnColors.Add(color);
            }
        }
        
        private void HandleExtraTurnSpecifically()
        {
            // Reset timer
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
            _timerCoroutine = StartCoroutine(TimerCoroutine());
    
            // Visual feedback
            _gameManager.TextAnimations.PopText(gameGUI.GetText(TMPTextType.Announcement), "Extra turn!", 0.15f, 0.1f, 1.15f);
    
            // If needed, trigger AI for an AI extra turn
            if (_playAgainstAI && !_isMyTurn)
            {
                StartCoroutine(ArtificialOpponent());
            }
        }

        private bool HandleExtraTurnPowerUp(bool isLocalPlayerMove)
        {
            bool extraTurn = powerUps.redPowerActivated && isLocalPlayerMove;
            if (extraTurn)
            {
                powerUps.ResetRedPowerup();
                powerUps.redPowerActivated = false;
                _isInExtraTurn = true;
            }
            return extraTurn;
        }

        private void SendNetworkMoves(int buttonIndex, string originalColor, bool isLocalPlayerMove)
        {
            if (!isLocalPlayerMove || !_isOnline) return;
            if (_isInExtraTurn)
            {
                _networkHandler.SendMoves(_extraTurnMoves, _extraTurnColors);
                _isInExtraTurn = false;
            }
            else
            {
                _networkHandler.SendMove(buttonIndex, originalColor);
            }
        }

        private void IncrementTurnCounter()
        {
            _totalTurns++;
        }

        private bool IsGameOver()
        {
            return _totalTurns >= _maxTurns;
        }

        private void CleanupBeforeTurnChange()
        {
            if (_isMyTurn)
            {
                powerUps.ResetBluePowerUp();
            }
        }

        private void ChangeTurn()
        {
            _isMyTurn = !_isMyTurn;
            gameGUI.UpdateTurnIndicators(_isMyTurn);
            HandleTimerBasedOnGameState();
            
            if (_isMyTurn)
            {
                _currentRound = (_totalTurns + (_isMyTurn ? 2 : 1)) / 2;
                gameGUI.GetText(TMPTextType.CurrentRound).text = $"Current Round {_currentRound}";
            }
        }

        private void TriggerAIIfNeeded(bool isLocalPlayerMove)
        {
            if (isLocalPlayerMove && _playAgainstAI)
            {
                StartCoroutine(ArtificialOpponent());
            }
        }
        
        private void ResolveReceivedMove(int position, string opponentColor)
        {
            _opponentGrid.Marks[position] = true;
            _opponentGrid.Color[position] = opponentColor;
            //otherBoard[position].color = GetColorFromHex(opponentColor);
            gameGUI.OtherBoard[position].color = GetColorFromHex(opponentColor);

            if (_playerGrid.Marks[position])
            {
                StartCoroutine(ResolveConflict(position));
            }
        }
        
        private IEnumerator ResolveConflict(int position)
        {
            // Step 1: Get colors and validate
            string player1Color = _playerGrid.Color[position];
            string player2Color = _opponentGrid.Color[position];

            if (string.IsNullOrEmpty(player1Color) || string.IsNullOrEmpty(player2Color))
            {
                yield break;
            }
            
            // Step 2: Determine conflict outcome
            (bool shouldClearPlayer1, bool shouldClearPlayer2) = ResolvingComparison(player1Color, player2Color);

            // Step 3: Handle immunities
            shouldClearPlayer1 = HandlePieceImmunity(position, shouldClearPlayer1, true);
            shouldClearPlayer2 = HandlePieceImmunity(position, shouldClearPlayer2, false);

            // Exit if no pieces to clear
            if (!shouldClearPlayer1 && !shouldClearPlayer2)
            {
                yield break;
            }

            // Step 4: Animate the clearing process
            yield return AnimatePieceClearing(position, shouldClearPlayer1, shouldClearPlayer2);

            // Step 5: Actually clear the squares
            if (shouldClearPlayer1) ClearSquare(position, true);
            if (shouldClearPlayer2) ClearSquare(position, false);
        }

        private bool HandlePieceImmunity(int position, bool shouldClear, bool isPlayer1)
        {
            if (!shouldClear) return false;
            
            GridData grid = isPlayer1 ? _playerGrid : _opponentGrid;
            
            if (grid.Immune[position])
            {
                grid.Immune[position] = false;
                gameGUI.RemoveShieldVisual(position, !isPlayer1);
                string message = isPlayer1 ? "Your piece was protected!" : "Opponent's piece was protected!";
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TMPTextType.Announcement), message, 0.15f, 0.1f, 1.15f);
                ClearSquare(position, !isPlayer1); // Not sure why this is the opposite, keeping your logic
                return false;
            }
            
            return true;
        }

        private IEnumerator AnimatePieceClearing(int position, bool clearPlayer1, bool clearPlayer2)
        {
            // Determine which transform to use for the VFX
            Transform effectTransform = clearPlayer1 ? gridButtonImages[position].transform : gameGUI.OtherBoard[position].transform;

            // Play the effect only if it's not clearPlayer2
            if (clearPlayer1)
            {
                GameManager.Instance.VFX.PlayEffectAt(effectTransform, 1, 1.5f);
            }

    
            // Small delay
            yield return new WaitForSeconds(0.1f);
    
            // Continue with the original color lerping animation
            float duration = 0.2f;
            float elapsedTime = 0f;
    
            // Get initial colors
            Color startColor1 = gridButtonImages[position].color;
            Color startColor2 = gameGUI.OtherBoard[position].color;
            Color targetColor = Color.white;
    
            // Animate fading to white
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;

                if (clearPlayer1) gridButtonImages[position].color = Color.Lerp(startColor1, targetColor, progress);
                if (clearPlayer2) gameGUI.OtherBoard[position].color = Color.Lerp(startColor2, targetColor, progress);

                yield return null;
            }
        }

        private (bool shouldClearPlayer1, bool shouldClearPlayer2) ResolvingComparison(string player1Color, string player2Color)
        {
            if (player1Color == player2Color)
            {
                return (true, true);
            }

            switch (player1Color)
            {
                case ColorGreenSelect:
                    return (player2Color == ColorBlueSelect, player2Color == ColorRedSelect);
                case ColorBlueSelect:
                    return (player2Color == ColorRedSelect, player2Color == ColorGreenSelect);
                case ColorRedSelect:
                    return (player2Color == ColorGreenSelect, player2Color == ColorBlueSelect);
                default:
                    return (false, false);
            }
        }
        
        private void ClearSquare(int position, bool isPlayerSquare)
        {
            Color whiteColor = Color.white;

            if (isPlayerSquare)
            {
                gridButtonImages[position].color = whiteColor;
                _playerGrid.Marks[position] = false;
                _playerGrid.Color[position] = "";
                //Debug.Log($"ClearSquare: Playing effect at position {position} on player grid");
            }
            else
            {
                gameGUI.OtherBoard[position].color = whiteColor;
                _opponentGrid.Marks[position] = false;
                _opponentGrid.Color[position] = "";
            }
        }

        private IEnumerator CheckPatternsCoroutine()
        {
            // We always check the active player's grid
            GridData activeGrid = _isMyTurn ? _playerGrid : _opponentGrid;
            CheckForPatterns(activeGrid);
            yield return null;
        }
        private void CheckForPatterns(GridData grid)
        {
            // Check rows
            for (int i = 0; i < 9; i += 3)
            {
                if (CheckLine(grid, i, i + 1, i + 2)) return;
            }

            // Check columns
            for (int i = 0; i < 3; i++)
            {
                if (CheckLine(grid, i, i + 3, i + 6)) return;
            }
    
            // Check diagonals
            CheckLine(grid, 0, 4, 8);
            CheckLine(grid, 2, 4, 6);
        }
        private bool CheckLine(GridData grid, int a, int b, int c)
        {
            if (!grid.Marks[a] || !grid.Marks[b] || !grid.Marks[c]) return false;
            if (grid.Color[a] != grid.Color[b] || grid.Color[b] != grid.Color[c]) return false;

            //Debug.Log($"Found match with color: {grid.Color[a]}");
            if (!_isMyTurn) return false;

            return powerUps.ProcessPowerup(grid.Color[a]);
        }
        private void SwapTurns()
        {
            _isMyTurn = !_isMyTurn;
            gameGUI.UpdateTurnIndicators(_isMyTurn);
            powerUps.UpdatePowerupInteractivity(_isMyTurn);
            HandleTimerBasedOnGameState();

            if (!_isMyTurn) powerUps.ResetBluePowerUp();
            
            if (_isMyTurn)
            {
                _currentRound = (_totalTurns + (_isMyTurn ? 1 : 0)) / 2;
                gameGUI.GetText(TMPTextType.CurrentRound).text = $"Current Round {_currentRound}";
            }
        }
        
        private IEnumerator ArtificialOpponent()
        {
            yield return new WaitForSeconds(0.5f);
            int movePosition = _ai.SimulateOpponentMove(_opponentGrid, gameGUI.OtherBoard);
    
            if (movePosition >= 0)
            {
                StartCoroutine(ProcessTurn(movePosition, false));
            }
        }
        
        private void HandleTimerBasedOnGameState()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            if (_gameState == GameState.Battle && _isMyTurn)
            {
                _timerCoroutine = StartCoroutine(TimerCoroutine());
            }
        }
        private IEnumerator TimerCoroutine()
        {
            _turnTimer = _gameManager.timer;

            while (_turnTimer > 0)
            {
                _turnTimer -= Time.deltaTime;
                _gameManager.TextAnimations.UpdateTimerWithEffects(gameGUI.TimerFill, _turnTimer, _gameManager.timer);
                yield return null;
            }
    
            _gameManager.TextAnimations.ResetTimerVisuals(gameGUI.TimerFill);
            _turnTimer = _gameManager.timer;
    
            // Handle shield selection first if active
            if (_shieldSelectionMode)
            {
                _ai.ForceShieldSelection(_playerGrid);
                _ai.ForcePlayerMove(_playerGrid);
            }
            else
            {
                _ai.ForcePlayerMove(_playerGrid);
            }
        }
        
        #endregion
        
        #region Network
        private void Ready()
        {
            _playerIsReady = true;
            readyButton.gameObject.SetActive(false);
        
            if (!_isOnline || _playAgainstAI)
            {
                StartGame(true);
            }
            else
            {
                _networkHandler.SendReady(_playerIsReady);
            }
            _gameManager.TextAnimations.Typewriter(gameGUI.GetText(TMPTextType.Countdown), "Ready and waiting...");
        }
        
        public void HandleReadyStateReceived(bool isMyTurn)
        {
            DOTween.KillAll();
            string[] countdownStrings = { "3", "2", "1", "GO!" };
            StartCoroutine(GameStartCountdownWrapper(countdownStrings, isMyTurn));
        }
        
        private IEnumerator GameStartCountdownWrapper(string[] countdownStrings, bool isMyTurn)
        {
            yield return StartCoroutine(gameGUI.GameStartCountdown(countdownStrings));
            StartGame(isMyTurn);
        }
        
        public void HandleMoveReceived(int index, string color)
        {
            ResolveReceivedMove(index, color);
    
            // Increment turn counter for online mode only
            IncrementTurnCounter();
    
            // Check if this was the final turn
            if (_totalTurns >= _maxTurns)
            {
                EndGame();
                return;  // Skip SwapTurns if game is over
            }
    
            SwapTurns();
        }
        
        public void HandleMultipleMovesReceived(List<int> indices, List<string> colors)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                ResolveReceivedMove(indices[i], colors[i]);
            }
    
            // Increment turn counter for online mode only
            IncrementTurnCounter();
    
            // Check if this was the final turn
            if (_totalTurns >= _maxTurns)
            {
                EndGame();
                return;
            }
    
            SwapTurns();
        }
        
        public void HandleImmuneStatusReceived(byte[] indices)
        {
            powerUps.ShieldOpponentPieces(_opponentGrid, indices);
        }
        #endregion
    }
}
