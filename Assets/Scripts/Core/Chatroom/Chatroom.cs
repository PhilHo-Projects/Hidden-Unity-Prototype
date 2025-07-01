using Core.WebSocket;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Chatroom
{
    public class Chatroom : MonoBehaviour
    {
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button backButton;
        [SerializeField] private string roomId = "Lobby";
        [SerializeField] private TMP_InputField roomName;
        
        [SerializeField] private GameObject chatBox;
        [SerializeField] private TMP_Text waitResponseMessage;
        
        private PacketType? _awaitingActionType;
        
        private void Start()
        {
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);
            backButton.onClick.AddListener(LeaveRoom);
            WebSocketNetworkHandler.Instance.OnServerResponse += HandleServerResponse;
            
            roomName.onSubmit.AddListener(_ => CreateRoom());
            
            string savedRoom = PlayerPrefs.GetString("LastRoomName", "DefaultRoom");
            roomName.text = savedRoom;
            
            roomName.Select();
            roomName.selectionAnchorPosition = 0;
            roomName.selectionFocusPosition = roomName.text.Length;
        }
        
        private void CreateRoom()
        {
            var createData = new StringPacket
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomCreate,
                Text = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };
            
            PlayerPrefs.SetString("LastRoomName", roomName.text);
        
            createRoomButton.gameObject.SetActive(false);
            roomName.gameObject.SetActive(false);
            joinRoomButton.gameObject.SetActive(false);
            _awaitingActionType = PacketType.RoomCreate;
            waitResponseMessage.text = "creating room...";
            FadeText(waitResponseMessage, true);
            
            //Debug.Log("Creating room... ");
            WebSocketNetworkHandler.Instance.SendPacket(createData);
        }

        private void JoinRoom()
        {
            var joinData = new StringPacket
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomJoin,
                Text = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };

            createRoomButton.gameObject.SetActive(false);
            roomName.gameObject.SetActive(false);
            joinRoomButton.gameObject.SetActive(false);
            _awaitingActionType = PacketType.RoomJoin;
            waitResponseMessage.text = "joining room...";
            FadeText(waitResponseMessage, true);


            //Debug.Log($"Attempting to join room: {roomId}");
            WebSocketNetworkHandler.Instance.SendPacket(joinData);
        }
        
        // this is pinged when we do return/exit out of this mode
        private void LeaveRoom()
        {
            var leaveData = new StringPacket
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomLeave,
                Text = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };

            _awaitingActionType = PacketType.RoomLeave;
            FadeText(waitResponseMessage, true);

            //Debug.Log($"Attempting to leave room: {roomId}");
            WebSocketNetworkHandler.Instance.SendPacket(leaveData);
        }
        
        private void HandleServerResponse(bool serverResponse)
        {
            if (!_awaitingActionType.HasValue) return;

            switch (_awaitingActionType.Value)
            {
                case PacketType.RoomCreate:
                    if (serverResponse)
                    {
                        //Debug.Log("Room created successfully!");
                        chatBox.SetActive(true);
                        backButton.gameObject.SetActive(true);
                        waitResponseMessage.text = "";
                    }
                    else
                    {
                        //Debug.Log("Room creation failed!");
                        waitResponseMessage.text = "Could not create room, either the Server is pooped or the room name exists already. (Click Join)";
                        createRoomButton.gameObject.SetActive(true);
                        roomName.gameObject.SetActive(true);
                        joinRoomButton.gameObject.SetActive(true);
                    }
                    break;

                case PacketType.RoomJoin:
                    if (serverResponse)
                    {
                        //Debug.Log("Successfully joined room!");
                        chatBox.SetActive(true);
                        backButton.gameObject.SetActive(true);
                        waitResponseMessage.text = "";
                    }
                    else
                    {
                        //Debug.Log("Failed to join room!");
                        waitResponseMessage.text = "Could not join room";
                        createRoomButton.gameObject.SetActive(true);
                        roomName.gameObject.SetActive(true);
                        joinRoomButton.gameObject.SetActive(true);
                    }
                    break;

                case PacketType.RoomLeave:
                    if (serverResponse)
                    {
                        //Debug.Log("Successfully left room!");
                        waitResponseMessage.text = " ";
                        backButton.gameObject.SetActive(false);
                        chatBox.SetActive(false);
                        createRoomButton.gameObject.SetActive(true);
                        roomName.gameObject.SetActive(true);
                        joinRoomButton.gameObject.SetActive(true);
                    }
                    else
                    {
                        //Debug.Log("Failed to leave room! (Would show error TMP text here)");
                    }
                    break;
            }

            _awaitingActionType = null;

        }
        
        private void FadeText(TMP_Text textElement, bool fadeIn, float duration = 0.5f, float delay = 0f)
        {
            textElement.DOKill();
    
            float startValue = fadeIn ? 0f : 1f;
            float endValue = fadeIn ? 1f : 0f;
    
            textElement.alpha = startValue;
    
            textElement.DOFade(endValue, duration)
                .SetDelay(delay)
                .SetEase(Ease.InOutSine);
        }
        private void OnDestroy()
        {
            if (WebSocketNetworkHandler.Instance != null)
            {
                WebSocketNetworkHandler.Instance.OnServerResponse -= HandleServerResponse;
            }
        }
    }
}
