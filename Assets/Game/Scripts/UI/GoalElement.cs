using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct GoalInfo
{
    public string type;
    public Sprite icon;
    public int initialCount;
    public int remainingCount;
}
public class GoalElement : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image completedImage;
    [SerializeField] private AudioClip goalCompletedClip;
    private bool isCompleted = false;
    private AudioSource audioSource;

    public void Initialize(Sprite icon, int count)
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        iconImage.sprite = icon;
        UpdateCount(count);
    }

    public void UpdateCount(int remaining)
    {
        if (isCompleted) return;
        if (remaining == 0) {
            isCompleted = true;
            audioSource.PlayOneShot(goalCompletedClip);
            countText.gameObject.SetActive(false);
            completedImage.gameObject.SetActive(true);
        } else { countText.text = remaining.ToString(); }
    }
}