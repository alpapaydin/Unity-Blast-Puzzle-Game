using UnityEngine;
using UnityEngine.SceneManagement;

public class Popup : MonoBehaviour {
    [SerializeField] private AudioClip thudClip;
    [SerializeField] private AudioClip loseClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayThud() { audioSource.PlayOneShot(thudClip); }
    public void PlayLose() { audioSource.PlayOneShot(loseClip); }
    public void GoToMainMenu() { SceneManager.LoadScene("MainScene"); }
    public void RestartLevel() { SceneManager.LoadScene("LevelScene"); }
}