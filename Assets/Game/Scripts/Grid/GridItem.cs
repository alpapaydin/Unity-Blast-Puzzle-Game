using System.Collections;
using UnityEngine;

public abstract class GridItem : MonoBehaviour
{
    [SerializeField] public SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] protected float explosionScale = 0.5f;
    protected Vector2Int gridPosition;
    public bool isMoving;
    protected GridController gridController;
    protected DamageType currentDamageType = DamageType.None;

    public Vector2Int GridPosition
    {
        get => gridPosition;
        set => gridPosition = value;
    }
    public bool IsMoving => isMoving;

    public virtual void Initialize(Vector2Int position)
    {
        gridPosition = position;
        gridController = transform.parent.GetComponent<GridController>();
    }

    public virtual void TakeDamage(DamageType damageType)
    {
        currentDamageType = damageType;
    }

    public virtual bool CanFall() => false;

    public virtual void MoveTo(Vector2Int newPosition, float duration)
    {
        if (isMoving) return;
        gridPosition = newPosition;
        StartCoroutine(MoveCoroutine(newPosition, duration));
    }

    public virtual void SetSortingOrder(int order)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }

    protected IEnumerator MoveCoroutine(Vector2Int targetPosition, float duration)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Transform targetSlot = gridController.GetSlotTransform(targetPosition.x, targetPosition.y);
        if (targetSlot == null)
        {
            Debug.LogError($"No slot found at {targetPosition}");
            yield break;
        }
        Vector3 endPos = targetSlot.position;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        transform.position = endPos;
        isMoving = false;
    }
    protected virtual Sprite GetDestroyParticleSprite()
    {
        return null;
    }
    public void SpawnDestroyParticles()
    {
        if (particlePrefab != null)
        {
            GameObject particles = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.loop = false;
                Sprite particleSprite = GetDestroyParticleSprite();
                if (particleSprite != null)
                {
                    var textureSheetAnimation = particleSystem.textureSheetAnimation;
                    if (textureSheetAnimation.enabled)
                    {
                        textureSheetAnimation.SetSprite(0, particleSprite);
                    }
                }
                float totalDuration = main.duration;
                Destroy(particles, totalDuration);
            }
            else
            {
                Destroy(particles);
            }
        }
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = new Vector3(explosionScale, explosionScale, 1f);
        }
    }
    protected virtual void OnDestroy() { }
}