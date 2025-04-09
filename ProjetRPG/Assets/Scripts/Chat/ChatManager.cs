using UnityEngine;
using Unity.Netcode;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance;

    void Awake() => Instance = this;

    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRpc(string senderName, string message, ChatChannel channel)
    {
        ReceiveChatMessageClientRpc(senderName, message, channel);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRpc(string senderName, string message, ChatChannel channel)
    {
        ChatUI.Instance?.DisplayMessage(senderName, message, channel);
    }
}
