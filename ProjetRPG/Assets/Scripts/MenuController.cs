using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void Jouer()
    {
        SceneManager.LoadScene("Map_0_0"); // remplace par le vrai nom de ta carte !
    }
}
