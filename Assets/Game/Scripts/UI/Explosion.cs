using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private AudioClip comboClip;
    public void OnAnimationFinished() { Destroy(gameObject); }
    public void PlayComboSound()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.PlayOneShot(comboClip);
    }
}
