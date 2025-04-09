using System.Collections; // ✅ Ajout nécessaire pour IEnumerator
using UnityEngine;
using UnityEngine.SceneManagement;

public class SafeStart : MonoBehaviour
{
    [Tooltip("Nom de la scène à charger si aucun mode réseau n’est actif")]
    public string fallbackScene = "Menu";

    void Start()
    {
        if (NetworkSession.connectionMode == NetworkSession.Mode.None &&
            SceneManager.GetActiveScene().name != fallbackScene)
        {
            Debug.Log("🕒 SafeStart : Scène fallback va être chargée...");
            StartCoroutine(LoadFallbackNextFrame());
        }
    }

    IEnumerator LoadFallbackNextFrame()
    {
        yield return null; // attend une frame complète
        SceneManager.LoadScene(fallbackScene, LoadSceneMode.Single);
    }
}
