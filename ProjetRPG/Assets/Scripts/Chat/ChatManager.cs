using UnityEngine;
using Unity.Netcode;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance;

    void Awake() => Instance = this;

    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRpc(string senderName, string message, ChatChannel channel, ServerRpcParams serverRpcParams = default)
    {
        if (message.Trim().ToLower() == "/armitage")
        {
            string legendLine = GenerateArmitageLegend();
            ulong senderClientId = serverRpcParams.Receive.SenderClientId;

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { senderClientId }
                }
            };

            ReceivePrivateNarratorMessageClientRpc("Narrateur", legendLine, clientRpcParams);
            return;
        }

        ReceiveChatMessageClientRpc(senderName, message, channel);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRpc(string senderName, string message, ChatChannel channel)
    {
        ChatUI.Instance?.DisplayMessage(senderName, message, channel);
    }

    [ClientRpc]
    void ReceivePrivateNarratorMessageClientRpc(string senderName, string message, ClientRpcParams clientRpcParams = default)
    {
        ChatUI.Instance?.DisplayNarratorMessage(senderName, message);
    }

    private string GenerateArmitageLegend()
    {
        string[] lines = new string[]
        {
            "Armitage, l’ombre d’une époque révolue... on murmure encore son nom dans les tavernes. On dit qu’Exxoduus a vaincu des rois et des dragons… sans jamais lever la voix.",
        };

        int index = Random.Range(0, lines.Length);
        return lines[index];
    }
}
