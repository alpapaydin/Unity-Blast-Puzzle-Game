using TMPro;
using UnityEngine;

public class MainScene : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip tapClip;
    [SerializeField] private TextMeshProUGUI m_TextMeshPro;
    [SerializeField] private TextAsset[] levelFiles;
    [SerializeField] private int debugLevel = 0;
    private LevelData levelData;
    private AudioSource bgmPlayer;
    private AudioSource sfxPlayer;
    void Start()
    {
        if (bgmPlayer == null)
        {
            sfxPlayer = gameObject.AddComponent<AudioSource>();
            bgmPlayer = gameObject.AddComponent<AudioSource>();
            bgmPlayer.loop = true;
            bgmPlayer.clip = bgmClip;
            bgmPlayer.Play();
        }
        #if UNITY_EDITOR
        if (debugLevel > 0) { GameManager.Instance.SetCurrentLevel(debugLevel - 1); }
        #endif
        levelData = LoadLevelData(GameManager.Instance.CurrentLevel);
        UpdateLevelText();
    }

    private void UpdateLevelText()
    {
        m_TextMeshPro.text = levelData != null ? $"Level {GameManager.Instance.CurrentLevel + 1}" : "Finished";
    }

    private LevelData LoadLevelData(int levelNumber)
    {
        if (levelNumber >= 0 && levelNumber < levelFiles.Length)
        {
            return JsonUtility.FromJson<LevelData>(levelFiles[levelNumber].text);
        }
        return null;
    }

    public void OnLevelButtonClicked()
    {
        sfxPlayer.PlayOneShot(tapClip);
        if (levelData != null)
        {
            GameManager.Instance.CurrentLevelData = levelData;
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelScene");
        }
    }
}