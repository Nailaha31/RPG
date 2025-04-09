using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSceneHandler : MonoBehaviour
{
    private bool callbackRegistered = false;

    void Update()
    {
        if (!callbackRegistered && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
            callbackRegistered = true;
            enabled = false; // désactive Update une fois fait
        }
    }

    void OnDisable()
    {
        if (callbackRegistered && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        Debug.Log($"✅ Scène {sceneName} chargée pour le client {clientId}");

        if (sceneName == "Map_0_0")
        {
            if (NetworkSession.connectionMode == NetworkSession.Mode.Host && !NetworkManager.Singleton.IsHost)
            {
                Debug.Log("🚀 Lancement automatique du Host");
                NetworkManager.Singleton.StartHost();
            }
            else if (NetworkSession.connectionMode == NetworkSession.Mode.Client && !NetworkManager.Singleton.IsClient)
            {
                Debug.Log("🔗 Connexion automatique au serveur");
                NetworkManager.Singleton.StartClient();
            }

            NetworkSession.connectionMode = NetworkSession.Mode.None; // Reset pour éviter les répétitions
        }
    }
}
