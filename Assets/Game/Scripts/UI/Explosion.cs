using UnityEngine;

public class Explosion : MonoBehaviour
{
    public void OnAnimationFinished() { Destroy(gameObject); }
}
