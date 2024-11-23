using UnityEngine;

public class Vase : Obstacle
{
    [Header("Vase Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite damagedSprite;

    private bool hasTakenBlastDamageThisTurn;
    private const int ITEM_LAYER = 1;

    protected override void Awake()
    {
        maxHealth = 2;
        currentHealth = maxHealth;
        canTakeDamageFromBlast = true;
        canTakeDamageFromTNT = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = normalSprite;
            spriteRenderer.sortingOrder = ITEM_LAYER;
        }
    }

    public override bool CanFall() => true;

    public override void TakeDamage(DamageType damageType)
    {
        if (!CanTakeDamage(damageType)) return;
        bool shouldTakeDamage = false;
        if (damageType == DamageType.Blast)
        {
            if (!hasTakenBlastDamageThisTurn || IsDamaged)
            {
                hasTakenBlastDamageThisTurn = true;
                shouldTakeDamage = true;
            }
        }
        else if (damageType == DamageType.TNT || damageType == DamageType.TNTCombo)
        {
            shouldTakeDamage = true;
        }
        if (shouldTakeDamage)
        {
            currentHealth--;
            OnDamaged();
            UpdateVisuals();
            if (IsDestroyed)
            {
                if (gridController != null)
                {
                    gridController.SetGridPosition(gridPosition.x, gridPosition.y, null);
                }
                OnDestroyed();
            }
            else
            {
                if (gridController != null)
                {
                    gridController.SetGridPosition(gridPosition.x, gridPosition.y, this);
                }
            }
        }
    }

    protected override bool CanTakeDamage(DamageType damageType)
    {
        if (damageType == DamageType.TNT || damageType == DamageType.TNTCombo)
        {
            return canTakeDamageFromTNT;
        }
        return damageType == DamageType.Blast && canTakeDamageFromBlast;
    }

    protected override void OnDamaged()
    {
        base.OnDamaged();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = IsDamaged ? damagedSprite : normalSprite;
            spriteRenderer.sortingOrder = ITEM_LAYER;
        }
    }

    protected override void UpdateVisuals()
    {
        base.UpdateVisuals();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = IsDamaged ? damagedSprite : normalSprite;
            spriteRenderer.sortingOrder = ITEM_LAYER;
        }
    }

    public override void Initialize(Vector2Int position)
    {
        base.Initialize(position);
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = ITEM_LAYER;
        }
    }
}