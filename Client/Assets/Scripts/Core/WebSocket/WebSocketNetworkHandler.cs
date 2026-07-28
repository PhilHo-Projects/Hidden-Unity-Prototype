using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Hidden;
using NativeWebSocket;
using Debug = UnityEngine.Debug;
using MessagePack;
using UnityEngine;

namespace Core.WebSocket
{
    public class WebSocketNetworkHandler : IndestructibleSingletonBehaviour<WebSocketNetworkHandler>
    {
        // ## Constants
        private const string ServerUrl = "wss://hidden.philippeho.dev/ws";

        // ## Core Components
        private NativeWebSocket.WebSocket _webSocket;
        private Matchmaking _matchmaking;
        public Matchmaking Matchmaking { get => _matchmaking; set => _matchmaking = value; }

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        private bool _isConnecting;

        // ## Handlers
        private ChatHandler _chatHandler;
        public ChatHandler ChatHandler { get => _chatHandler; set => _chatHandler = value; }

        private MovementHandler _movementHandler;
        public MovementHandler MovementHandler { get => _movementHandler; set => _movementHandler = value; }
        
        private HiddenMain _hiddenGame;
        public HiddenMain HiddenGame { get => _hiddenGame; set => _hiddenGame = value; }

        // ## Message Processing
        private readonly Queue<Action> _actions = new Queue<Action>();
        private Dictionary<PacketType, Action<byte[]>> _messageHandlers;
        private readonly MessagePackConfig _messagePackConfig = new MessagePackConfig();

        // ## User Management
        private byte _clientId;
        public byte ClientId => _clientId;
        private readonly Dictionary<byte, string> _users = new Dictionary<byte, string>();
        public IReadOnlyDictionary<byte, string> Users => _users;

        // ## Events
        public event Action<bool> OnServerResponse;
        public event Action<bool> OnGameStartConfirmation;
        
        protected override void Awake()
        {
            base.Awake();
            _matchmaking = new Matchmaking(this);
            InitializeMessageHandlers();
            _messagePackConfig.InitMessagePackResolvers();
        }
        private void InitializeMessageHandlers()
        {
            _messageHandlers = new Dictionary<PacketType, Action<byte[]>>
            {
                { PacketType.Chat, ProcessChatMessage },
                { PacketType.Position, ProcessPosition },
                { PacketType.IdAssign, HandleIdAssign },
                { PacketType.TimeSync, HandleTimeSync },
                { PacketType.ServerResponse, HandleServerResponse },
                { PacketType.UserInfo, HandleUserInfo },
                { PacketType.MatchFound, ProcessMatchFound },
                { PacketType.GameStartInfo , StartGameConfirmationFromServer },
                { PacketType.HiddenGamePacket, ProcessHiddenGame },
                { PacketType.ExtraTurnPacket, ProcessExtraTurnPacket },
                { PacketType.HiddenGameImmune, ProcessImmune }
            };
        }
        
        private void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            if(_webSocket != null)
                _webSocket.DispatchMessageQueue();    
            #endif

            lock(_actions)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue().Invoke();
                }
            }
        }
        
        #region Connection Management
        public void Connect()
        {
            // Check if already connected or in the process of connecting
            if (_webSocket != null && 
                (_webSocket.State == WebSocketState.Open || 
                 _webSocket.State == WebSocketState.Connecting || 
                 _isConnecting))
            {
                //Debug.Log("Already connected or connecting. Ignoring connection request.");
                return;
            }

            _isConnecting = true;
            ConnectAsync().ContinueWith(task => 
            {
                _isConnecting = false;
                if (task.IsFaulted)
                {
                    Debug.LogError($"Connection failed: {task.Exception}");
                }
            });
        }
        private async Task ConnectAsync()
        {
            try 
            {
                if (_webSocket != null)
                {
                    await _webSocket.Close();
                    _webSocket = null;
                }

                _webSocket = new NativeWebSocket.WebSocket(ServerUrl);

                _webSocket.OnMessage += ProcessIncomingMessage;
                _webSocket.OnOpen += HandleOpen;
                _webSocket.OnError += HandleError;
                
                await _webSocket.Connect();
            }
            catch (Exception e)
            {
                _isConnecting = false;
                Debug.LogError($"Connection error: {e.Message}");
                throw;
            }
        }

        private void HandleOpen()
        {
            Debug.Log("Connected");
        }
        private void HandleError(string error)
        {
            Debug.LogError($"WebSocket Error: {error}");
        }
        private void OnApplicationQuit()
        {
            CloseWebSocketConnection();
        }

        private void CloseWebSocketConnection()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                StartCoroutine(CloseWebSocketCoroutine());
            }
        }

        private IEnumerator CloseWebSocketCoroutine()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var closeTask = _webSocket.Close();
                while (!closeTask.IsCompleted)
                {
                    yield return null;
                }

                if (closeTask.Exception != null)
                {
                    Debug.LogError($"Error closing WebSocket: {closeTask.Exception}");
                }
            }
        }
        #endregion
        
        #region Sending
        
        // public void SendPacket<T>(T package) where T : BaseNetworkPacket
        // {
        //     if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        //     {
        //         Debug.LogError("Cannot send message: WebSocket is not connected");
        //         return;
        //     }
        //
        //     package.SenderId = _clientId;
        //
        //     try 
        //     {
        //         byte[] bytes = MessagePackSerializer.Serialize(package);
        //         SendWebSocketPackageAsync(bytes).ContinueWith(task => 
        //         {
        //             if (task.IsFaulted)
        //             {
        //                 Debug.LogError($"Send failed: {task.Exception?.Message}");
        //             }
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogError($"Serialization failed: {ex}");
        //     }
        // }
        
        public void SendPacket<T>(T package) where T : BaseNetworkPacket
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogError("Cannot send message: WebSocket is not connected");
                return;
            }

            package.SenderId = _clientId;

            try 
            {
                byte[] bytes = MessagePackSerializer.Serialize(package);
                SendPacketInternalAsync(package).ContinueWith(task => 
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Send failed: {task.Exception?.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Serialization failed: {ex}");
            }
        }
        
                
        // private readonly Dictionary<PacketType, TaskCompletionSource<bool>> _pendingRequests = new();
        // public async Task<bool> SendPacketReliableWithTasks<T>(T packet) where T : BaseNetworkPacket
        // {
        //     if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        //     {
        //         Debug.LogError("Cannot send message: WebSocket is not connected");
        //         return false;
        //     }
        //
        //     // Set up response waiting
        //     var tcs = new TaskCompletionSource<bool>();
        //     _pendingRequests[packet.Type] = tcs;
        //
        //     packet.SenderId = _clientId;
        //
        //     try
        //     {
        //         // Send the packet
        //         await SendPacketInternalAsync(packet);
        //
        //         // Wait for server response with timeout (WebGL-safe coroutine timeout)
        //         bool timeoutReached = false;
        //         StartCoroutine(TaskTimeoutCoroutine(packet.Type, 5f, () => timeoutReached = true));
        //
        //         // Wait for either response or timeout
        //         while (!tcs.Task.IsCompleted && !timeoutReached)
        //         {
        //             await Task.Delay(50); // Small delay
        //         }
        //
        //         // Clean up
        //         _pendingRequests.Remove(packet.Type);
        //
        //         if (timeoutReached)
        //         {
        //             Debug.LogError($"{packet.Type} confirmation timed out");
        //             return false;
        //         }
        //
        //         return await tcs.Task;
        //     }
        //     catch (Exception ex)
        //     {
        //         _pendingRequests.Remove(packet.Type);
        //         Debug.LogError($"Send failed: {ex.Message}");
        //         return false;
        //     }
        // }
        
        // VERSION 2: Callback-based (should be WebGL-safe)
        private readonly Dictionary<PacketType, Action<bool>> _pendingCallbacks = new();

        public void SendPacketReliableWithCallbacks<T>(T packet, Action<bool> onComplete) where T : BaseNetworkPacket
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogError("Cannot send message: WebSocket is not connected");
                onComplete?.Invoke(false);
                return;
            }

            packet.SenderId = _clientId;
    
            // Store the callback for this packet type
            _pendingCallbacks[packet.Type] = onComplete;
    
            try 
            {
                // Send packet using coroutine (WebGL-safe)
                StartCoroutine(SendWithCallbackCoroutine(packet));
            }
            catch (Exception ex)
            {
                _pendingCallbacks.Remove(packet.Type);
                Debug.LogError($"Send failed: {ex.Message}");
                onComplete?.Invoke(false);
            }
        }

        private IEnumerator SendWithCallbackCoroutine<T>(T packet) where T : BaseNetworkPacket
        {
            // Send the packet
            var sendTask = SendPacketInternalAsync(packet);
            yield return new WaitUntil(() => sendTask.IsCompleted);
    
            if (sendTask.IsFaulted)
            {
                _pendingCallbacks.Remove(packet.Type);
                Debug.LogError($"Send failed: {sendTask.Exception?.GetBaseException().Message}");
                _pendingCallbacks[packet.Type]?.Invoke(false);
                yield break;
            }
    
            // Start timeout coroutine
            StartCoroutine(CallbackTimeoutCoroutine(packet.Type, 5f));
        }

        private IEnumerator CallbackTimeoutCoroutine(PacketType packetType, float timeout)
        {
            yield return new WaitForSeconds(timeout);
    
            // Check if callback is still pending (server never responded)
            if (_pendingCallbacks.TryGetValue(packetType, out var callback))
            {
                _pendingCallbacks.Remove(packetType);
                Debug.LogError($"{packetType} confirmation timed out");
                callback?.Invoke(false); // Timeout = failure
            }
        }


        // private IEnumerator TaskTimeoutCoroutine(PacketType packetType, float timeout, System.Action onTimeout)
        // {
        //     yield return new WaitForSeconds(timeout);
        //     if (_pendingRequests.ContainsKey(packetType))
        //     {
        //         onTimeout?.Invoke();
        //     }
        // }
        
        private async Task SendPacketInternalAsync<T>(T packet) where T : BaseNetworkPacket
        {
            try
            {
                byte[] bytes = MessagePackSerializer.Serialize(packet);
        
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.Send(bytes);
                }
                else
                {
                    throw new InvalidOperationException("WebSocket is not connected. Cannot send message.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Serialization or send failed: {ex.Message}");
                throw; // Re-throw so reliable version can handle it
            }
        }


        #endregion

        #region Receiving

        private void EnqueueMainThread(Action action)
        {
            lock(_actions)
            {
                _actions.Enqueue(action);
            }
        }
        // private void ProcessIncomingMessage(byte[] data)
        // {
        //     var decoded = MessagePackSerializer.Deserialize<object[]>(data);
        //     if (decoded == null || decoded.Length < 2) return;
        //
        //     var packetType = (PacketType)Convert.ToInt32(decoded[1]);
        //
        //     if (_messageHandlers.TryGetValue(packetType, out var handler))
        //     {
        //         EnqueueMainThread(() => handler(data));
        //         //todo instead of sending data, we can send var decoded object[] instead so we don't have to deserialize twice
        //     }
        // }
        
        private void ProcessIncomingMessage(byte[] data)
        {
            var reader = new MessagePackReader(data);
            if (reader.ReadArrayHeader() < 2) return;
            
            reader.Skip();
            var packetType = (PacketType)reader.ReadInt32();

            if (_messageHandlers.TryGetValue(packetType, out var handler)) 
            {
                EnqueueMainThread(() => handler(data));
            }
        }
        
        private void ProcessChatMessage(byte[] messagePackData) =>
            _chatHandler?.ProcessIncomingChatData(messagePackData);
        private void ProcessPosition(byte[] messagePackData) =>
            _movementHandler?.ProcessRemotePositionUpdate(messagePackData);
        private void ProcessHiddenGame(byte[] messagePackData) => 
            _hiddenGame?.NetworkHandler.ReceiveMove(messagePackData);
        private void ProcessExtraTurnPacket(byte[] messagePackData) => 
            _hiddenGame?.NetworkHandler.ReceiveMoves(messagePackData);
        private void ProcessImmune(byte[] messagePackData) => 
            _hiddenGame?.NetworkHandler.UpdateClientImmunePieces(messagePackData);
        private void ProcessMatchFound(byte[] messagePackData) => 
            _matchmaking?.HandleMatchFoundPacket(messagePackData);
        private void HandleIdAssign(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                _clientId = (byte)decoded[2];
            }
        }

        private void HandleTimeSync(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                long serverTime = Convert.ToInt64(decoded[2]);
                // would attach a delegate here and broadcast to whoever needs to know the time
            }
        }

        // private void HandleServerResponse(byte[] data)
        // {
        //     var decoded = MessagePackSerializer.Deserialize<object[]>(data);
        //     if (decoded != null && decoded.Length >= 3)
        //     {
        //         bool response = Convert.ToBoolean(decoded[2]);
        //         OnServerResponse?.Invoke(response);
        //     }
        // }
        
        // SHARED: Handle server responses for both versions
        private void HandleServerResponse(byte[] data)
        {
            //Debug.Log("Receiving server response");
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);

            if (decoded != null && decoded.Length >= 4)
            {
                bool response = Convert.ToBoolean(decoded[2]);
                PacketType originalPacketType = (PacketType)Convert.ToInt32(decoded[3]);

                // // Handle TASK version
                // if (_pendingRequests.TryGetValue(originalPacketType, out var tcs))
                // {
                //     tcs.SetResult(response);
                //     _pendingRequests.Remove(originalPacketType);
                //     Debug.Log($"[TASK] Resolved {originalPacketType} request with response: {response}");
                // }
        
                // Handle CALLBACK version
                if (_pendingCallbacks.TryGetValue(originalPacketType, out var callback))
                {
                    callback?.Invoke(response);
                    _pendingCallbacks.Remove(originalPacketType);
                    //Debug.Log($"[CALLBACK] Resolved {originalPacketType} request with response: {response}");
                }
            }
        }

        
        private void StartGameConfirmationFromServer(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                // Extract the first player ID
                var firstPlayerId = Convert.ToInt32(decoded[2]);
        
                // Determine if local player goes first
                bool isLocalPlayerFirst = (firstPlayerId == _clientId);
        
                Debug.Log($"Game starting! First player: {firstPlayerId} (You: {(isLocalPlayerFirst ? "Yes" : "No")})");
        
                // Trigger the event with information about who goes first
                OnGameStartConfirmation?.Invoke(isLocalPlayerFirst);
            }
        }

        
        private void HandleUserInfo(byte[] data)
        {
            // We could scale this up and set the dictionnary into its own class if we need more than just user info + his name
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                var userList = MessagePackSerializer.Deserialize<UserEntry[]>(MessagePackSerializer.Serialize(decoded[2]));
                if (userList != null)
                {
                    foreach (var user in userList)
                    {
                        _users[user.UserId] = user.UserName;
                    }
                }
            }
        }
        
        #endregion
        
        private void LogPackageDebugInfo(BaseNetworkPacket package)
        {
            Debug.Log($"Sending package of type: {package.GetType().Name}");
            Debug.Log($"Package contents: SenderId={package.SenderId}, Type={package.Type}");
    
            if (package is StringPacket chatData)
            {
                Debug.Log($"Chat text: {chatData.Text}");
            }
    
            byte[] bytes = MessagePackSerializer.Serialize(package.GetType(), package);
            Debug.Log($"Serialized bytes: [{string.Join(", ", bytes)}]");
        }
    }
}
