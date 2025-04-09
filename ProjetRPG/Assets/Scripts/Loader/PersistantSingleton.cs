using UnityEngine;

public class PersistantSingleton : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
