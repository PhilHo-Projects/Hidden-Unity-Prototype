using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.WebSocket;

namespace Core.Hidden
{
    public class MatchmakingHiddenGame : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text connectionStatus;
        [SerializeField] private Button findOpponentButton;
        [SerializeField] private Button cancelFindOpponentButton;
        [SerializeField] private CanvasGroup mainPage;
        [SerializeField] private CanvasGroup readyPage;

        [Header("Game Reference")]
        [SerializeField] private HiddenMain gridGame;

        // Private variables
        private bool _isSearching;
        private float _searchStartTime;
        private Coroutine _searchTimerCoroutine;
        private WebSocketNetworkHandler _wsHandler;

        private void Awake()
        {
            _wsHandler = WebSocketNetworkHandler.Instance;
            
            // Initialize UI state
            findOpponentButton.gameObject.SetActive(true);
            cancelFindOpponentButton.gameObject.SetActive(false);
        }

        private void Start()
        {
            connectionStatus.text = "Find someone to play";
        }

        private void OnEnable()
        {
            // Subscribe to network events
            _wsHandler.Matchmaking.OnMatchFound += MatchFound;
            _wsHandler.Matchmaking.OnSearchCancelled += MatchmakingCancelled;

            // Set up button listeners
            findOpponentButton.onClick.AddListener(FindOpponent);
            cancelFindOpponentButton.onClick.AddListener(CancelMatchmaking);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            _wsHandler.Matchmaking.OnMatchFound -= MatchFound;
            _wsHandler.Matchmaking.OnSearchCancelled -= MatchmakingCancelled;

            // Remove button listeners
            findOpponentButton.onClick.RemoveListener(FindOpponent);
            cancelFindOpponentButton.onClick.RemoveListener(CancelMatchmaking);
            
            // Stop any active coroutines
            if (_searchTimerCoroutine != null)
            {
                StopCoroutine(_searchTimerCoroutine);
                _searchTimerCoroutine = null;
            }
        }

        private void FindOpponent()
        {
            _wsHandler.Matchmaking.StartMatchmaking();

            connectionStatus.text = "Searching...";
            _isSearching = true;
            _searchStartTime = Time.time;

            if (_searchTimerCoroutine != null)
                StopCoroutine(_searchTimerCoroutine);
            
            _searchTimerCoroutine = StartCoroutine(UpdateSearchTimer());

            SetButtonState(false);
        }

        private void CancelMatchmaking()
        {
            _wsHandler.Matchmaking.CancelMatchmaking();
            MatchmakingCancelled();
        }

        private void MatchFound(string roomId)
        {
            // Stop search timer
            _isSearching = false;
            if (_searchTimerCoroutine != null)
            {
                StopCoroutine(_searchTimerCoroutine);
                _searchTimerCoroutine = null;
            }

            // Hide both buttons when match is found
            findOpponentButton.gameObject.SetActive(false);
            cancelFindOpponentButton.gameObject.SetActive(false);
            
            // Transition to ready page
            FadeToReadyScreen();
            
            // Notify game of match found
            //OnMatchFound?.Invoke(roomId);
            
            Debug.Log("Found match and we're in the room called " + roomId);
        }

        private void MatchmakingCancelled()
        {
            // Update UI
            connectionStatus.text = "Cancelled";
            _isSearching = false;

            // Stop timer coroutine
            if (_searchTimerCoroutine != null)
            {
                StopCoroutine(_searchTimerCoroutine);
                _searchTimerCoroutine = null;
            }

            // Show find button, hide cancel button
            SetButtonState(true);

            // Reset the status text after delay
            StartCoroutine(ResetStatusTextAfterDelay(2f));
        }

        private IEnumerator UpdateSearchTimer()
        {
            while (_isSearching)
            {
                float elapsedTime = Time.time - _searchStartTime;
                int minutes = Mathf.FloorToInt(elapsedTime / 60);
                int seconds = Mathf.FloorToInt(elapsedTime % 60);
                connectionStatus.text = $"Searching... {minutes:00}:{seconds:00}";
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator ResetStatusTextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!_isSearching)
                connectionStatus.text = "Find someone to play";
        }
        
        // Helper methods
        private void SetButtonState(bool showFindButton)
        {
            findOpponentButton.gameObject.SetActive(showFindButton);
            cancelFindOpponentButton.gameObject.SetActive(!showFindButton);
        }
        
        private void FadeToReadyScreen()
        {
            // Transition to the ready screen
            mainPage.alpha = 0;
            mainPage.blocksRaycasts = false;
            mainPage.interactable = false;
            
            readyPage.alpha = 1;
            readyPage.blocksRaycasts = true;
            readyPage.interactable = true;
        }
    }
}

