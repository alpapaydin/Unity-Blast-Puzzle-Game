using UnityEngine;
using System.Collections;

public abstract class Obstacle : GridItem
{
    [SerializeField] protected string obstacleType;

    protected int maxHealth;
    protected int currentHealth;
    protected bool canTakeDamageFromBlast;
    protected bool canTakeDamageFromTNT;
    protected bool isDestroyed = false;
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        UpdateVisuals();
    }

    public bool IsDamaged => currentHealth < maxHealth;
    public bool IsDestroyed => currentHealth <= 0;

    public override void TakeDamage(DamageType damageType)
    {
        if (!CanTakeDamage(damageType)) return;
        base.TakeDamage(damageType);
        currentHealth--;
        OnDamaged();
        UpdateVisuals();
        if (IsDestroyed)
        {
            OnDestroyed();
        }
    }

    protected virtual bool CanTakeDamage(DamageType damageType)
    {
        return (damageType == DamageType.Blast && canTakeDamageFromBlast) ||
               ((damageType == DamageType.TNT || damageType == DamageType.TNTCombo) && canTakeDamageFromTNT);
    }

    protected virtual void OnDamaged()
    {
        StartCoroutine(DamageAnimation());
    }

    protected virtual void OnDestroyed()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        gridController.levelScene.OnObstacleDestroyed(obstacleType);
        SpawnDestroyParticles();
        Destroy(gameObject);
    }

    protected virtual void UpdateVisuals() {}

    private IEnumerator DamageAnimation()
    {
        if (spriteRenderer == null || !spriteRenderer) yield break;
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null && spriteRenderer)
        {
            spriteRenderer.color = originalColor;
        }
    }
}