using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public LevelData CurrentLevelData { get; set; }
    private const string SAVE_KEY = "CurrentLevel";
    public int CurrentLevel { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGame()
    {
        CurrentLevel = PlayerPrefs.GetInt(SAVE_KEY, 1);
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt(SAVE_KEY, CurrentLevel);
        PlayerPrefs.Save();
    }

    public void SetCurrentLevel(int level)
    {
        CurrentLevel = level;
        SaveGame();
    }

    public void IncreaseCurrentLevel(int by)
    {
        CurrentLevel += by;
        SaveGame();
    }
}