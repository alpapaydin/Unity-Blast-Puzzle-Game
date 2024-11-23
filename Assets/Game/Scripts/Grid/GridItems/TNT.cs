using System.Collections.Generic;
using UnityEngine;

public class TNT : GridItem
{
    private bool isExploding = false;
    private static HashSet<GridItem> explodingTNTs = new HashSet<GridItem>();
    public override bool CanFall() => true;
    public override void TakeDamage(DamageType damageType)
    {
        if (isExploding) return;
        isExploding = true;
        explodingTNTs.Add(this);
        var adjacentTNTs = GetAdjacentTNTs();
        if (adjacentTNTs.Count > 0)
        {
            // TNT Combo
            explosionScale = 8f;
            Explode(DamageType.TNTCombo);
            foreach (var tnt in adjacentTNTs)
            {
                if (!explodingTNTs.Contains(tnt))
                {
                    tnt.TakeDamage(DamageType.TNTCombo);
                }
            }
        }
        else
        {
            // Normal TNT
            explosionScale = 5f;
            Explode(DamageType.TNT);
        }
        gridController.SetGridPosition(GridPosition.x, GridPosition.y, null);
        explodingTNTs.Remove(this);
        LevelScene.TriggerGridUpdate();
        SpawnDestroyParticles();
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
    }
}