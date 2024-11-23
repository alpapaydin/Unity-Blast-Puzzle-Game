using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class TNT : GridItem
{
    private bool isExploding = false;
    private static HashSet<GridItem> explodingTNTs = new HashSet<GridItem>();
    private static int activeExplosionChains = 0;
    private const float EXPLOSION_DELAY = 0.4f;
    private const float TRIGGER_SCALE_UP = 1.3f;
    private const float TRIGGER_ANIMATION_DURATION = 0.2f;
    private const int TRIGGER_ANIMATION_LOOPS = 2;
    private Vector3 originalScale;

    public override bool CanFall() => true;

    public override void Initialize(Vector2Int position)
    {
        base.Initialize(position);
        originalScale = transform.localScale;
    }

    public override void TakeDamage(DamageType damageType)
    {
        if (isExploding) return;
        isExploding = true;
        explodingTNTs.Add(this);
        activeExplosionChains++;
        var adjacentTNTs = GetAdjacentTNTs();
        foreach (var tnt in adjacentTNTs)
        {
            if (!explodingTNTs.Contains(tnt))
            {
                tnt.TakeDamage(damageType);
            }
        }
        StartCoroutine(TriggerAndExplodeSequence(damageType));
    }

    private IEnumerator TriggerAndExplodeSequence(DamageType damageType)
    {
        for (int loop = 0; loop < TRIGGER_ANIMATION_LOOPS; loop++)
        {
            float elapsed = 0f;
            while (elapsed < TRIGGER_ANIMATION_DURATION / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (TRIGGER_ANIMATION_DURATION / 2);
                float easedProgress = 1 - Mathf.Cos(progress * Mathf.PI / 2);
                transform.localScale = Vector3.Lerp(originalScale, originalScale * TRIGGER_SCALE_UP, easedProgress);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < TRIGGER_ANIMATION_DURATION / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (TRIGGER_ANIMATION_DURATION / 2);
                float easedProgress = 1 - Mathf.Cos(progress * Mathf.PI / 2);
                transform.localScale = Vector3.Lerp(originalScale * TRIGGER_SCALE_UP, originalScale, easedProgress);
                yield return null;
            }
        }
        float remainingDelay = EXPLOSION_DELAY - (TRIGGER_ANIMATION_DURATION * TRIGGER_ANIMATION_LOOPS);
        if (remainingDelay > 0)
        {
            yield return new WaitForSeconds(remainingDelay);
        }
        if (GetAdjacentTNTs().Count > 0)
        {
            explosionScale = 8f;
            Explode(DamageType.TNTCombo);
        }
        else
        {
            explosionScale = 5f;
            Explode(DamageType.TNT);
        }
        gridController.TntExploded();
        gridController.SetGridPosition(GridPosition.x, GridPosition.y, null);
        explodingTNTs.Remove(this);
        SpawnDestroyParticles();
        activeExplosionChains--;
        if (activeExplosionChains <= 0)
        {
            activeExplosionChains = 0;
            LevelScene.TriggerGridUpdate();
        }
        Destroy(gameObject);
    }

    private List<TNT> GetAdjacentTNTs()
    {
        List<TNT> adjacentTNTs = new List<TNT>();
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = GridPosition + dir;
            GridItem neighborItem = gridController.GetGridItem(neighborPos.x, neighborPos.y);
            if (neighborItem is TNT tnt && !explodingTNTs.Contains(neighborItem))
            {
                adjacentTNTs.Add(tnt);
            }
        }
        return adjacentTNTs;
    }

    private void Explode(DamageType explosionType)
    {
        int range = explosionType.GetExplosionRange();
        int radius = range / 2;
        List<(GridItem item, Vector2Int position)> itemsToDamage = new List<(GridItem, Vector2Int)>();
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int targetPos = new Vector2Int(
                    GridPosition.x + x,
                    GridPosition.y + y
                );
                GridItem targetItem = gridController.GetGridItem(targetPos.x, targetPos.y);
                if (targetItem != null && targetItem != this && !explodingTNTs.Contains(targetItem))
                {
                    itemsToDamage.Add((targetItem, targetPos));
                }
            }
        }
        foreach (var (item, position) in itemsToDamage)
        {
            gridController.SetGridPosition(position.x, position.y, null);
            item.TakeDamage(explosionType);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        explodingTNTs.Remove(this);
        transform.localScale = originalScale;
    }
}