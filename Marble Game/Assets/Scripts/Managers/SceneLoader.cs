using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public static int  SelectedCharacterIndex = 0;
    public static int  HumanPlayerCount       = 1;
    public static int  AIPlayerCount          = 1;
    public static bool IsSinglePlay           = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void GoMainMenu()   => SceneManager.LoadScene(0);
    public static void GoCharSelect() => SceneManager.LoadScene(1);
    public static void GoGame()       => SceneManager.LoadScene(2);
}