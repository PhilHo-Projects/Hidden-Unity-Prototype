using System;
using System.Collections.Generic;
using UnityEngine;
using MessagePack; 

namespace Core.WebSocket
{
    public class MovementHandler : MonoBehaviour
    {
        private WebSocketNetworkHandler _wsHandler;
        private Dictionary<string, GameObject> _trackedObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, Vector3> _lastSentPositions = new Dictionary<string, Vector3>();
        private const float MOVEMENT_THRESHOLD = 0.001f; // Adjust this value as needed

        private void Awake()
        {
            _wsHandler = WebSocketNetworkHandler.Instance;
            _wsHandler.MovementHandler = this;
        }

        public void RegisterObject(string objectId, GameObject obj)
        {
            if (!_trackedObjects.ContainsKey(objectId))
            {
                _trackedObjects.Add(objectId, obj);
            }
        }

        public void UnregisterObject(string objectId)
        {
            if (_trackedObjects.ContainsKey(objectId))
            {
                _trackedObjects.Remove(objectId);
            }
        }

        public void SendPositionUpdate(string objectId, Vector3 position)
        {
            if (!_lastSentPositions.ContainsKey(objectId) || Vector3.Distance(_lastSentPositions[objectId], position) > MOVEMENT_THRESHOLD)
            {
                var positionMessageVector = new ObjectVector3Packet
                {
                    Type = PacketType.Position,
                    ObjectId = objectId,
                    SenderId = _wsHandler.ClientId,
                    Position = position
                };
    
                _lastSentPositions[objectId] = position;
                _wsHandler.SendPacket(positionMessageVector);
            }
        }

        public void ProcessRemotePositionUpdate(byte[] messagePackData)
        {
            var positionData = MessagePackSerializer.Deserialize<ObjectVector3Packet>(messagePackData);
            if (positionData.SenderId == _wsHandler.ClientId) return;

            if (_trackedObjects.TryGetValue(positionData.ObjectId, out GameObject obj))
            {
                // Optional: Add interpolation for smoother movement
                obj.transform.position = positionData.Position;
            }
        }

        private void OnDestroy()
        {
            _trackedObjects.Clear();
        }
    }
}