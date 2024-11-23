using EditorUtils;
using System.IO;
using TMPro;
using UnityEngine;

public class MainScene : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TextMeshPro;
    [SerializeField] private FolderReference levelsFolder;
    [SerializeField] private int debugLevel = 1;
    private LevelData levelData;

    void Start()
    {
        if (debugLevel > 0) { GameManager.Instance.SetCurrentLevel(debugLevel); }
        levelData = LoadLevelData(GameManager.Instance.CurrentLevel);
        UpdateLevelText();
    }

    private void UpdateLevelText()
    {
        m_TextMeshPro.text = levelData != null ? $"Level {GameManager.Instance.CurrentLevel}" : "Finished";
    }

    private LevelData LoadLevelData(int levelNumber)
    {
        string formattedNumber = levelNumber.ToString("D2");
        string path = Path.Combine(levelsFolder.Path, $"level_{formattedNumber}.json");
        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);
            return JsonUtility.FromJson<LevelData>(jsonContent);
        }
        return null;
    }

    public void OnLevelButtonClicked()
    {
        if (levelData != null)
        {
            GameManager.Instance.CurrentLevelData = levelData;
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelScene");
        }
    }
}