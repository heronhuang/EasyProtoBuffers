using UnityEngine;
using Warkits.ToolBox.EasyProto;

enum ENetCode : byte
{
    PLAYER_INFO = 10,
    CHAT_MSG    = 20
}
/// <summary>
/// Simulate client: only handles serialization
/// </summary>
public static class FakeClient
{
    public static byte[] SendPlayerInfo()
    {
        PlayerInfo playerData = new PlayerInfo
        {
            Id    = 1001,
            Name  = "Player10",
            Level = 35,
            Hp    = 987.65f
        }; 
        // 2. Client serializes (sends to server)
        byte[] sendData = Utility.Serialize( (byte)ENetCode.PLAYER_INFO, playerData );

        return sendData;
    }

    public static byte[] ChatMsg()
    {
        ChatMessage chat = new ChatMessage
        {
            Sender = "Fake Client",
            Content = "Hello Protobuf!"
        };

        byte[] chatData =  Utility.Serialize( (byte)ENetCode.CHAT_MSG, chat );
        return chatData;
    }
}

/// <summary>
/// Simulate server: only handles deserialization
/// </summary>
public static class FakeServer
{
    public static void Packets_Handler(byte[] packets)
    {
        switch ((ENetCode)packets[0])
        {
            case ENetCode.CHAT_MSG:
                // Server receives & deserializes
                ChatMessage msg = ChatMessage.Parser.ParseFrom( packets, 1, packets.Length - 1 );
                //Display results
                Debug.Log($"[Server] Received: ID={packets[0]} [Chat] {msg.Sender}: <color=green>{msg.Content}</color>");
                break;
            case ENetCode.PLAYER_INFO:
                // Server receives & deserializes
                PlayerInfo receiveData = PlayerInfo.Parser.ParseFrom( packets, 1, packets.Length - 1 );
                //Display results
                Debug.Log($"[Server] Received: ID={packets[0]} Name={receiveData.Name} Level={receiveData.Level} <color=red>HP={receiveData.Hp}</color>"); 
                break;
        }
    }
}
public class ProtoBufferDemo : MonoBehaviour
{
   
    void Start()
    {
        Debug.Log("======= Protobuf FakeServer & FakeClient Demo ======="); 
        // 1. Client creates data
        byte[]  packet_1 =  FakeClient.SendPlayerInfo(); 
        //.....network transfer............... 
        // 2. Server processes data
        FakeServer.Packets_Handler( packet_1 );

        
        
        // ---------------- Chat message demo ----------------
        Debug.Log("--------------------------------------------------");
        byte[]  packet_2 =  FakeClient.ChatMsg();
        //.....network transfer...............
        FakeServer.Packets_Handler( packet_2 ); 
    
    }
}
