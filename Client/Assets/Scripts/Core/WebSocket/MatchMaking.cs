using System;
using System.Collections;
using MessagePack;
using UnityEngine;

namespace Core.WebSocket
{
    public class Matchmaking
    {
        // Events for communication with game-specific UI/logic
        public event Action<string> OnMatchFound;
        //public event Action OnSearchTimeout;
        public event Action OnSearchCancelled;
        
        // Reference to the network handler
        private readonly WebSocketNetworkHandler _networkHandler;
        private Coroutine _timeoutCoroutine;
        
        // State
        private bool _isSearching;
        public bool IsSearching => _isSearching;
        
        // Configuration
        private readonly float _matchmakingTimeout;
        
        public Matchmaking(WebSocketNetworkHandler networkHandler, float timeout = 60f)
        {
            _networkHandler = networkHandler;
            _matchmakingTimeout = timeout;
        }

        // Call this to handle the match found packet from the server
        public void HandleMatchFoundPacket(byte[] data)
        {
            if (!_isSearching) return;
            
            _isSearching = false;
            
            // Stop timeout coroutine
            if (_timeoutCoroutine != null)
            {
                _networkHandler.StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }
            
            // Parse room info
            RoomAction roomAction = MessagePackSerializer.Deserialize<RoomAction>(data);
            string roomId = roomAction.RoomId;
            
            Debug.Log($"Match found! Room ID: {roomId}");
            OnMatchFound?.Invoke(roomId);
        }
        
        public void StartMatchmaking()
        {
            if (_isSearching) return;
            
            _isSearching = true;
            
            var matchRequest = new BooleanPacket
            {
                Type = PacketType.MatchmakingRequest,
                Response = true
            };
            
            _networkHandler.SendPacket(matchRequest);
            
            // Start timeout monitoring
            _timeoutCoroutine = _networkHandler.StartCoroutine(MatchmakingTimeoutCoroutine());
        }
        
        public void CancelMatchmaking()
        {
            if (!_isSearching) return;
            
            _isSearching = false;
            
            var cancelRequest = new BooleanPacket
            {
                Type = PacketType.MatchmakingRequest,
                Response = false
            };
            
            _networkHandler.SendPacket(cancelRequest);
            
            // Stop timeout coroutine if running
            if (_timeoutCoroutine != null)
            {
                _networkHandler.StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }
            
            Debug.Log("Cancelled matchmaking search");
        }
        
        private IEnumerator MatchmakingTimeoutCoroutine()
        {
            yield return new WaitForSeconds(_matchmakingTimeout);
            
            if (_isSearching)
            {
                // Auto-cancel if it's taking too long
                _isSearching = false;
                var cancelRequest = new BooleanPacket
                {
                    Type = PacketType.MatchmakingRequest,
                    Response = false
                };
                _networkHandler.SendPacket(cancelRequest);
                
                Debug.Log("Matchmaking search timed out");
                OnSearchCancelled?.Invoke();
            }
            _timeoutCoroutine = null;
        }
    }
}
