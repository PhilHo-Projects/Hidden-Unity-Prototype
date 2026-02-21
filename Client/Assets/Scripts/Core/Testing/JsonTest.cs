using UnityEngine;
using System.IO;
using Core.WebSocket;

public class JsonTest : MonoBehaviour
{
    void Start()
    {
        var chatMessage = new StringPacket
        {
            Type = PacketType.Chat,
            Text = "Test Message",
            SenderId = 123
        };

        string jsonMessage = JsonUtility.ToJson(chatMessage, true); // true for pretty print
        Debug.Log($"JSON created: {jsonMessage}");

        string filePath = @"E:\Downloads\wtf.json";
        File.WriteAllText(filePath, jsonMessage);
        Debug.Log($"JSON saved to {filePath}");
    }
}