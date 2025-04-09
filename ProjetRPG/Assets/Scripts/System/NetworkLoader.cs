using UnityEngine;
using Unity.Netcode;

public class NetworkLoader : MonoBehaviour
{
    public GameObject networkPrefab;
    private static bool isNetworkReady = false;

    void Awake()
    {
        if (!isNetworkReady)
        {
            GameObject go = Instantiate(networkPrefab);
            DontDestroyOnLoad(go);
            isNetworkReady = true;
        }
    }

    void Start()
{
    Debug.Log("✅ NetworkLoader prêt");

    if (NetworkSession.connectionMode == NetworkSession.Mode.None &&
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Menu")
    {
        Debug.Log("🕒 Aucun mode défini. Chargement de la scène Menu...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}

}
