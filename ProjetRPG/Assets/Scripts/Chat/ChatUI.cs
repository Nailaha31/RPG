using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ChatUI : MonoBehaviour
{
    public static ChatUI Instance;

    [Header("UI Références")]
    public TMP_Dropdown channelDropdown;
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform messageContainer;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    [Header("Bouton Masquer/Afficher")]
    public Button toggleButton;
    public CanvasGroup chatCanvasGroup; // ← doit être mis sur ChatPanel

    private Dictionary<ChatChannel, List<GameObject>> channelMessages = new();
    private ChatChannel currentChannel = ChatChannel.General;

    private Dictionary<ChatChannel, Color> channelColors = new()
    {
        { ChatChannel.General, Color.white },
        { ChatChannel.Commerce, Color.yellow },
        { ChatChannel.Donjon, Color.cyan }
    };

    private List<string> sentMessages = new();
    private int messageHistoryIndex = -1;

    private bool isChatVisible = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        channelDropdown.onValueChanged.AddListener(OnChannelChanged);
        inputField.onSubmit.AddListener(OnInputSubmit);
        toggleButton.onClick.AddListener(ToggleChatVisibility);

        // Active au démarrage
        ShowChat(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            inputField.ActivateInputField();
        }

        if (inputField.isFocused && Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (sentMessages.Count > 0 && messageHistoryIndex > 0)
            {
                messageHistoryIndex--;
                inputField.text = sentMessages[messageHistoryIndex];
                inputField.caretPosition = inputField.text.Length;
            }
        }

        if (inputField.isFocused && Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (sentMessages.Count > 0 && messageHistoryIndex < sentMessages.Count - 1)
            {
                messageHistoryIndex++;
                inputField.text = sentMessages[messageHistoryIndex];
                inputField.caretPosition = inputField.text.Length;
            }
            else if (messageHistoryIndex == sentMessages.Count - 1)
            {
                messageHistoryIndex++;
                inputField.text = "";
            }
        }
    }

    void ToggleChatVisibility()
    {
        isChatVisible = !isChatVisible;
        ShowChat(isChatVisible);
    }

    void ShowChat(bool visible)
    {
        chatCanvasGroup.alpha = visible ? 1f : 0f;
        chatCanvasGroup.blocksRaycasts = visible;
        chatCanvasGroup.interactable = visible;

        TMP_Text text = toggleButton.GetComponentInChildren<TMP_Text>();
        text.text = visible ? "" : "";
    }

    void OnChannelChanged(int index)
    {
        currentChannel = (ChatChannel)index;

        foreach (var pair in channelMessages)
        {
            foreach (GameObject msg in pair.Value)
            {
                msg.SetActive(pair.Key == currentChannel);
            }
        }
    }

    void OnSendClicked()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        string playerName = "Joueur_" + (NetworkManager.Singleton?.LocalClientId ?? 0);
        ChatManager.Instance?.SendChatMessageServerRpc(playerName, inputField.text, currentChannel);

        sentMessages.Add(inputField.text);
        messageHistoryIndex = sentMessages.Count;

        inputField.text = "";
        inputField.ActivateInputField();
    }

    void OnInputSubmit(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            OnSendClicked();
        }
    }

    public void DisplayMessage(string senderName, string message, ChatChannel channel)
    {
        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        TMP_Text textComp = msgObj.GetComponent<TMP_Text>();
        textComp.text = $"[{channel}] {senderName} : {message}";
        textComp.color = channelColors[channel];

        if (!channelMessages.ContainsKey(channel))
            channelMessages[channel] = new List<GameObject>();

        channelMessages[channel].Add(msgObj);
        msgObj.SetActive(channel == currentChannel);

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
