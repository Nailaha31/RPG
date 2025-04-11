using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MenuMultijoueur : MonoBehaviour
{
    public void Heberger()
    {
        NetworkSession.connectionMode = NetworkSession.Mode.Host;
        Debug.Log("💻 Mode défini : HOST");

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("🟡 Un host/server est déjà actif !");
            return;
        }

        NetworkManager.Singleton.StartHost(); // démarre le réseau
        NetworkManager.Singleton.SceneManager.LoadScene("Spawn_0_0", LoadSceneMode.Single); // charge via le réseau
    }

    public void Rejoindre()
    {
        NetworkSession.connectionMode = NetworkSession.Mode.Client;
        Debug.Log("🌐 Mode défini : CLIENT");

        if (!NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartClient(); // client tente de se connecter
        }

        // ⚠️ Le client ne doit PAS charger la scène manuellement
        // La scène sera synchronisée automatiquement quand le Host l'aura chargée
    }
}
