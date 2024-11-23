using UnityEngine;

public class Celebration : MonoBehaviour
{
    [SerializeField] private AudioClip thudClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip spinClip;
    private AudioSource audioSource;
    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayThud() { audioSource.PlayOneShot(thudClip); }
    public void PlayWin() { audioSource.PlayOneShot(winClip); }
    public void PlaySpin() { audioSource.PlayOneShot(spinClip); }

}
