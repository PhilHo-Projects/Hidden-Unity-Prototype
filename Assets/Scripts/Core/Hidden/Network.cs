using System.Collections.Generic;
using UnityEngine;
using Core.WebSocket;
using MessagePack;

namespace Core.Hidden
{
    public class Network
    {
        private readonly WebSocketNetworkHandler _wsHandler;
        private readonly HiddenMain _gameRef;
        private readonly GameGUI _gameGUI;
        public Network(HiddenMain gameReference, GameGUI gameGUI, WebSocketNetworkHandler wsHandler)
        {
            _gameRef = gameReference;
            _gameGUI = gameGUI;
            _wsHandler = wsHandler;
            _wsHandler.OnGameStartConfirmation += ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound += MatchFound;
        }

        public void Cleanup()
        {
            _wsHandler.OnGameStartConfirmation -= ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound -= MatchFound;
        }

        #region Send Methods

        public void SendReady(bool isReady)
        {
            var isReadyPacket = new BooleanPacket
            {
                Type = PacketType.HiddenGameConfirmStart,
                Response = isReady
            };
            _wsHandler.SendPacket(isReadyPacket);
        }

        public void SendMove(int index, string colorBeforeResolution)
        {
            var createData = new HiddenGameData
            {
                Type = PacketType.HiddenGamePacket,
                Index = (byte)index,
                SquareColor = colorBeforeResolution
            };
            _wsHandler.SendPacket(createData);
        }
        
        public void SendMoves(List<int> indices, List<string> colors)
        {
            byte[] indicesArray = new byte[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                indicesArray[i] = (byte)indices[i];
            }
    
            var createData = new ExtraTurnPacket
            {
                Type = PacketType.ExtraTurnPacket,
                Indices = indicesArray,
                Colors = colors.ToArray()
            };
    
            _wsHandler.SendPacket(createData);
        }

        public void SendImmuneStatus(byte index)
        {
            SendImmuneStatus(new [] { index });
        }

        public void SendImmuneStatus(byte[] indexes)
        {
            var immunePacket = new HiddenGameIndices 
            {
                Type = PacketType.HiddenGameImmune,
                Index = indexes
            };
            _wsHandler.SendPacket(immunePacket);
        }


        #endregion

        #region Receive Methods

        public void ReceiveMove(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<HiddenGameData>(messagePackData);
            if (receivedData.SenderId != _wsHandler.ClientId)
            {
                int index = receivedData.Index;
                _gameRef.HandleMoveReceived(index, receivedData.SquareColor);
            }
        }

        public void ReceiveMoves(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<ExtraTurnPacket>(messagePackData);
            if (receivedData.SenderId != _wsHandler.ClientId)
            {
                List<int> indices = new List<int>();
                List<string> colors = new List<string>();
                
                for (int i = 0; i < receivedData.Indices.Length; i++)
                {
                    indices.Add(receivedData.Indices[i]);
                    colors.Add(receivedData.Colors[i]);
                }
                
                _gameRef.HandleMultipleMovesReceived(indices, colors);
            }
        }

        public void UpdateClientImmunePieces(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<HiddenGameIndices>(messagePackData);
            if (receivedData.SenderId != _wsHandler.ClientId)
            {
                _gameRef.HandleImmuneStatusReceived(receivedData.Index);
            }
        }

        private void MatchFound(string roomId)
        {
            Debug.Log("Found match and we're in the room called: " + roomId);
            _gameGUI.ShowMatchFoundScreen("Player Found! Ready?");
        }

        private void ReceiveReadyState(bool isMyTurn)
        {
            _gameRef.HandleReadyStateReceived(isMyTurn);
        }

        #endregion
    }
}
