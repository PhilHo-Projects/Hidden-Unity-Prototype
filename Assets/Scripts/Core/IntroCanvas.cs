using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.WebSocket;
using Random = UnityEngine.Random;

namespace Core
{
    public class IntroCanvas : MonoBehaviour
    {
        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup welcomeScreenGroup;
        [SerializeField] private CanvasGroup onlineScreenGroup;
        [SerializeField] private CanvasGroup offlineScreenGroup;
        
        [Header("Welcome Screen Elements")]
        [SerializeField] private Button onlineButton;
        [SerializeField] private Button offlineButton;
        
        [Header("Online Screen Elements")]
        [SerializeField] private TMP_InputField userInputField;
        [SerializeField] private Image usernameTextBackground;
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button confirmNameButton;
        [SerializeField] private Button randomizeNameButton;
        [SerializeField] private Button websocketChatButton;
        [SerializeField] private Button pongGameButton;
        [SerializeField] private Button vinceGameButton;
        [SerializeField] private TMP_InputField[] userInputParameter;

        [Header("Offline Screen Elements")]
        [SerializeField] private Button offlineScene;
        [SerializeField] private Button offlineVinceGame;
        [SerializeField] private Toggle offlineBlindToggle;
        
        [Header("Navigation")]
        [SerializeField] private Button backButton;
        
        private string _savedUsername = string.Empty;

        private void Start()
        {
            SetupButtonListeners();
            _savedUsername = PlayerPrefs.GetString("Username", "");
            ShowWelcomeScreen();
            
            for (int i = 0; i < userInputParameter.Length; i++)
            {
                int index = i;
                userInputParameter[i].contentType = TMP_InputField.ContentType.IntegerNumber;
                userInputParameter[i].onEndEdit.AddListener(value => OnParameterChanged(value, index));
                //Debug.Log($"Input field {i} name: {userInputParameter[i].name}");
            }
        }

        private void SetupButtonListeners()
        {
            onlineButton.onClick.AddListener(ShowOnlineScreen);
            offlineButton.onClick.AddListener(ShowOfflineScreen);
            backButton.onClick.AddListener(HandleBackButton);
            confirmNameButton.onClick.AddListener(HandleConfirmName);
            randomizeNameButton.onClick.AddListener(RandomizeName);
            
            websocketChatButton.onClick.AddListener(() => ConnectToWebsocket("ChatRoom"));
            pongGameButton.onClick.AddListener(() => ConnectToWebsocket("Pong"));

            //vinceGameButton.onClick.AddListener(() => ConnectToWebsocket("HiddenGame"));
            
            // vinceGameButton.onClick.AddListener(() => {
            //     GameManager.Instance.isOnline = true;
            //     GameManager.Instance.blindModeActive = true;
            //     GameManager.Instance.playingAgainstAI = false;
            //     WebSocketNetworkHandler.Instance.Connect();
            //     ConnectWithRetriesTaskVersion("HiddenGame");
            // });

            
            vinceGameButton.onClick.AddListener(() => {
                GameManager.Instance.isOnline = true;
                GameManager.Instance.blindModeActive = true;
                GameManager.Instance.playingAgainstAI = false;
                WebSocketNetworkHandler.Instance.Connect();
                StartCoroutine(ConnectWithRetriesCallbackVersion("HiddenGame"));
            });

            
            offlineScene.onClick.AddListener(() => SceneLoader.Instance.LoadScene("OfflinePrototype"));
            offlineVinceGame.onClick.AddListener(LoadVinceOfflineGame);
            
            userInputField.onValueChanged.AddListener(newValue => usernameText.text = $"Username: {newValue}");
            userInputField.onSubmit.AddListener(_ => HandleConfirmName());
        }
        
        private void ShowWelcomeScreen()
        {
            SetCanvasGroupActive(welcomeScreenGroup, true);
            SetCanvasGroupActive(onlineScreenGroup, false);
            SetCanvasGroupActive(offlineScreenGroup, false);
            backButton.gameObject.SetActive(false);
        }
        
        private void ShowOnlineScreen()
        {
            SetCanvasGroupActive(welcomeScreenGroup, false);
            SetCanvasGroupActive(onlineScreenGroup, true);
            SetCanvasGroupActive(offlineScreenGroup, false);
            backButton.gameObject.SetActive(true);
            
            // Show the name input section
            userInputField.text = _savedUsername;
            userInputField.gameObject.SetActive(true);
            confirmNameButton.gameObject.SetActive(true);
            randomizeNameButton.gameObject.SetActive(true);
            usernameTextBackground.gameObject.SetActive(true);
            usernameText.gameObject.SetActive(true);
            
            // Hide game options until name is confirmed
            websocketChatButton.gameObject.SetActive(false);
            //pongGameButton.gameObject.SetActive(false);
            vinceGameButton.gameObject.SetActive(false);
        }
        
        private void ShowOfflineScreen()
        {
            SetCanvasGroupActive(welcomeScreenGroup, false);
            SetCanvasGroupActive(onlineScreenGroup, false);
            SetCanvasGroupActive(offlineScreenGroup, true);
            backButton.gameObject.SetActive(true);
        }
        
        private void HandleBackButton()
        {
            errorText.text = "";
    
            offlineBlindToggle.isOn = true;
            
            userInputField.gameObject.SetActive(true);
            confirmNameButton.gameObject.SetActive(true);
            randomizeNameButton.gameObject.SetActive(true);
            
            websocketChatButton.gameObject.SetActive(false);
            //pongGameButton.gameObject.SetActive(false);
            vinceGameButton.gameObject.SetActive(false);
    
            ShowWelcomeScreen();
        }
        
        private void HandleConfirmName()
        {
            string newUsername = userInputField.text.Trim();
            if (!string.IsNullOrEmpty(newUsername))
            {
                _savedUsername = newUsername;
                PlayerPrefs.SetString("Username", newUsername);
                PlayerPrefs.Save();
                
                // Hide name input section
                confirmNameButton.gameObject.SetActive(false);
                userInputField.gameObject.SetActive(false);
                randomizeNameButton.gameObject.SetActive(false);

                
                // Show game options
                usernameText.text = $"Username: {newUsername}";
                websocketChatButton.gameObject.SetActive(true);
                //pongGameButton.gameObject.SetActive(true);
                vinceGameButton.gameObject.SetActive(true);
            }
            else
            {
                GameManager.Instance.TextAnimations.PopText(errorText, "Please enter a username!");
            }
        }

        private void RandomizeName()
        {
            string[] names = new string[]
            {
                // 10 chatroom
                "CodeJunkie", "ByteMaster", "ScriptLord", "DataMiner", "NetSurfer", 
                "LogicBolt", "ThreadWeaver", "BinaryBard", "AlgorithmGuy", "SyntaxHero",
                // 10 gamers
                "ApexPredator", "BlazeRunner", "CipherBlade", "DuskFang", "EchoStrike",
                "FrostNova", "GhostHunter", "HavocAgent", "InfernoZero", "JoltShock",
                // 10 wow
                "IronhideMarauder", "Stonebreaker", "GrimAxe", "Thunderhoof", "Bloodfist",
                "VoidCaller", "ShadowCrawler", "Frostbinder", "Earthshaker", "Stormbringer",
                // 10 Normies
                "Henry", "Sarah", "Michael", "Jessica", "David",
                "Emily", "James", "Ashley", "Robert", "Megan",
                // 10 Futuristic
                "CommanderShepard", "TaliZorah", "GarrusVakarian", "LiaraTsoni", "Wrex",
                "Grunt", "MirandaLawson", "JacobTaylor", "MordinSolus", "EDI" 
            };


            string randomName = names[Random.Range(0, names.Length)];
    
            userInputField.text = randomName;
            usernameText.text = $"Username: {randomName}";
        }

        private void SetCanvasGroupActive(CanvasGroup group, bool active)
        {
            group.alpha = active ? 1 : 0;
            group.interactable = active;
            group.blocksRaycasts = active;
        }
        
        private void ConnectToWebsocket(string sceneName)
        {
            GameManager.Instance.isOnline = true;
            GameManager.Instance.blindModeActive = true;
            GameManager.Instance.playingAgainstAI = false;
            WebSocketNetworkHandler.Instance.Connect();
            StartCoroutine(ConnectWithRetries(sceneName, 3));
        }
        
        private IEnumerator ConnectWithRetries(string sceneName, int maxRetries = 2)
        {
            int retryCount = 0;
            bool connected = false;
    
            while (!connected && retryCount <= maxRetries)
            {
                errorText.text = retryCount > 0 ? $"Retrying connection ({retryCount}/{maxRetries})..." : "Connecting...";
        
                WebSocketNetworkHandler.Instance.Connect();
        
                // Wait for connection with timeout
                float timeoutDuration = 3.0f;
                float elapsed = 0;
        
                while (!WebSocketNetworkHandler.Instance.IsConnected && elapsed < timeoutDuration)
                {
                    yield return new WaitForSeconds(0.2f);
                    elapsed += 0.2f;
                }
        
                connected = WebSocketNetworkHandler.Instance.IsConnected;
        
                if (!connected)
                {
                    retryCount++;
                    yield return new WaitForSeconds(0.5f); // Brief pause between retries
                }
            }
    
            if (connected)
            {
                // Connection successful
                var chatMessage = new StringPacket
                {
                    Type = PacketType.UserInfo,
                    Text = _savedUsername
                };
                WebSocketNetworkHandler.Instance.SendPacket(chatMessage);
        
                yield return new WaitForSeconds(0.2f);
                SceneLoader.Instance.LoadScene(sceneName);
            }
            else
            {
                GameManager.Instance.TextAnimations.PopText(errorText, "Failed to connect to server after multiple attempts!");
            }
        }
        
        // TASK VERSION - Test if this breaks WebGL

        // private async void ConnectWithRetriesTaskVersion(string sceneName)
        // {
        //     int retryCount = 0;
        //     int maxRetries = 2;
        //     bool connected = false;
        //
        //     while (!connected && retryCount <= maxRetries)
        //     {
        //         errorText.text = retryCount > 0 ? $"Retrying connection ({retryCount}/{maxRetries})..." : "Connecting...";
        //
        //         WebSocketNetworkHandler.Instance.Connect();
        //
        //         // Wait for connection with timeout (using Time.time instead of yield)
        //         float startTime = Time.time;
        //         float timeoutDuration = 3.0f;
        //
        //         while (!WebSocketNetworkHandler.Instance.IsConnected && (Time.time - startTime) < timeoutDuration)
        //         {
        //             await Task.Delay(200); // This is what we suspect breaks WebGL
        //         }
        //
        //         connected = WebSocketNetworkHandler.Instance.IsConnected;
        //
        //         if (!connected)
        //         {
        //             retryCount++;
        //             await Task.Delay(500); // This too
        //         }
        //     }
        //
        //     if (connected)
        //     {
        //         try
        //         {
        //             // Test 1: Send username with task-based reliable method
        //             var usernamePacket = new StringPacket
        //             {
        //                 Type = PacketType.UserInfo,
        //                 Text = _savedUsername
        //             };
        //
        //             Debug.Log("Sending username with TASK method...");
        //             bool usernameAccepted = await WebSocketNetworkHandler.Instance.SendPacketReliableWithTasks(usernamePacket);
        //
        //             if (!usernameAccepted)
        //             {
        //                 Debug.LogError("Username rejected or timed out (TASK METHOD)");
        //                 GameManager.Instance.TextAnimations.PopText(errorText, "Username failed (TASK)");
        //                 return;
        //             }
        //
        //             Debug.Log("Username accepted by server (TASK METHOD)");
        //
        //             // Test 2: Join lobby with task-based reliable method
        //             var lobbyPacket = new StringPacket
        //             {
        //                 Type = PacketType.RoomJoin,
        //                 Text = "lobbyRoom"
        //             };
        //
        //             Debug.Log("Joining lobby with TASK method...");
        //             bool lobbyJoined = await WebSocketNetworkHandler.Instance.SendPacketReliableWithTasks(lobbyPacket);
        //
        //             if (!lobbyJoined)
        //             {
        //                 Debug.LogError("Failed to join lobby (TASK METHOD)");
        //                 GameManager.Instance.TextAnimations.PopText(errorText, "Lobby join failed (TASK)");
        //                 return;
        //             }
        //
        //             Debug.Log("Lobby joined successfully (TASK METHOD)");
        //             SceneLoader.Instance.LoadScene(sceneName);
        //         }
        //         catch (Exception ex)
        //         {
        //             Debug.LogError($"Task-based connection failed: {ex.Message}");
        //             GameManager.Instance.TextAnimations.PopText(errorText, "Task method crashed!");
        //         }
        //     }
        //     else
        //     {
        //         GameManager.Instance.TextAnimations.PopText(errorText, "Failed to connect to server after multiple attempts!");
        //     }
        // }
        
        // CALLBACK VERSION - Should be WebGL-safe
        private IEnumerator ConnectWithRetriesCallbackVersion(string sceneName)
        {
            int retryCount = 0;
            int maxRetries = 2;
            bool connected = false;

            while (!connected && retryCount <= maxRetries)
            {
                errorText.text = retryCount > 0 ? $"Retrying connection ({retryCount}/{maxRetries})..." : "Connecting...";

                WebSocketNetworkHandler.Instance.Connect();

                // Wait for connection with timeout (WebGL-safe)
                float timeoutDuration = 3.0f;
                float elapsed = 0;

                while (!WebSocketNetworkHandler.Instance.IsConnected && elapsed < timeoutDuration)
                {
                    yield return new WaitForSeconds(0.2f);
                    elapsed += 0.2f;
                }

                connected = WebSocketNetworkHandler.Instance.IsConnected;

                if (!connected)
                {
                    retryCount++;
                    yield return new WaitForSeconds(0.5f);
                }
            }

            if (connected)
            {
                // Test 1: Send username with callback-based reliable method
                bool usernameProcessed = false;
                bool usernameSuccess = false;

                var usernamePacket = new StringPacket
                {
                    Type = PacketType.UserInfo,
                    Text = _savedUsername
                };

                Debug.Log("Sending username with CALLBACK method...");
                WebSocketNetworkHandler.Instance.SendPacketReliableWithCallbacks(usernamePacket, (success) => {
                    usernameSuccess = success;
                    usernameProcessed = true;
                    Debug.Log($"Username callback result: {success}");
                });

                // Wait for username response
                yield return new WaitUntil(() => usernameProcessed);

                if (!usernameSuccess)
                {
                    Debug.LogError("Username rejected or timed out (CALLBACK METHOD)");
                    GameManager.Instance.TextAnimations.PopText(errorText, "Username failed (CALLBACK)");
                    yield break;
                }

                Debug.Log("Username accepted by server (CALLBACK METHOD)");

                // Test 2: Join lobby with callback-based reliable method
                bool lobbyProcessed = false;
                bool lobbySuccess = false;

                var lobbyPacket = new StringPacket
                {
                    Type = PacketType.RoomJoin,
                    Text = "lobbyRoom"
                };

                Debug.Log("Joining lobby with CALLBACK method...");
                WebSocketNetworkHandler.Instance.SendPacketReliableWithCallbacks(lobbyPacket, (success) => {
                    lobbySuccess = success;
                    lobbyProcessed = true;
                    Debug.Log($"Lobby callback result: {success}");
                });

                // Wait for lobby response
                yield return new WaitUntil(() => lobbyProcessed);

                if (!lobbySuccess)
                {
                    Debug.LogError("Failed to join lobby (CALLBACK METHOD)");
                    GameManager.Instance.TextAnimations.PopText(errorText, "Lobby join failed (CALLBACK)");
                    yield break;
                }

                Debug.Log("Lobby joined successfully (CALLBACK METHOD)");
                SceneLoader.Instance.LoadScene(sceneName);
            }
            else
            {
                GameManager.Instance.TextAnimations.PopText(errorText, "Failed to connect to server after multiple attempts!");
            }
        }

        private void LoadVinceOfflineGame()
        {
            GameManager.Instance.playingAgainstAI = true;
            GameManager.Instance.isOnline = false;
            GameManager.Instance.blindModeActive = offlineBlindToggle.isOn;
            SceneLoader.Instance.LoadScene("HiddenGame");
        }
        
        private void OnParameterChanged(string value, int index)
        {
            if (string.IsNullOrEmpty(value)) return;
    
            if (float.TryParse(value, out float inputValue))
            {
                switch (index)
                {
                    case 0:
                        GameManager.Instance.numberOfRounds = (byte)inputValue;
                        break;
                    case 1:
                        GameManager.Instance.timer = inputValue;
                        break;

                }
            }
        }

        private void OnDestroy()
        {
            onlineButton.onClick.RemoveAllListeners();
            offlineButton.onClick.RemoveAllListeners();
            websocketChatButton.onClick.RemoveAllListeners();
            pongGameButton.onClick.RemoveAllListeners();
            vinceGameButton.onClick.RemoveAllListeners();
            offlineScene.onClick.RemoveAllListeners();
            offlineVinceGame.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
            confirmNameButton.onClick.RemoveAllListeners();
            randomizeNameButton.onClick.RemoveAllListeners();
            userInputField.onValueChanged.RemoveAllListeners();
            userInputField.onSubmit.RemoveAllListeners();
        }
    }
}
