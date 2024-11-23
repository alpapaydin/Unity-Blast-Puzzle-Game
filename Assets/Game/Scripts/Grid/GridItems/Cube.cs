using UnityEngine;
using System.Collections.Generic;

public class Cube : GridItem
{
    public enum CubeColor
    {
        Red,
        Green,
        Blue,
        Yellow
    }

    [System.Serializable]
    public struct CubeSprites
    {
        public Sprite normalSprite;
        public Sprite tntIndicatorSprite;
        public Sprite destroyParticle;
    }

    [Header("Sprites")]
    [SerializeField] private CubeSprites redCube;
    [SerializeField] private CubeSprites greenCube;
    [SerializeField] private CubeSprites blueCube;
    [SerializeField] private CubeSprites yellowCube;

    private CubeColor cubeColor;
    private bool canCreateTNT;

    public CubeColor Color => cubeColor;
    public bool CanCreateTNT
    {
        get => canCreateTNT;
        set
        {
            canCreateTNT = value;
            UpdateSprite();
        }
    }

    public override bool CanFall() => true;

    public void SetColor(CubeColor color)
    {
        cubeColor = color;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        CubeSprites currentSprites = cubeColor switch
        {
            CubeColor.Red => redCube,
            CubeColor.Green => greenCube,
            CubeColor.Blue => blueCube,
            CubeColor.Yellow => yellowCube,
            _ => redCube
        };
        spriteRenderer.sprite = canCreateTNT ? currentSprites.tntIndicatorSprite : currentSprites.normalSprite;
    }

    public override void Initialize(Vector2Int position)
    {
        base.Initialize(position);
        canCreateTNT = false;
        UpdateSprite();
    }

    public HashSet<Cube> GetConnectedCubes()
    {
        HashSet<Cube> connectedCubes = new HashSet<Cube>();
        FloodFill(this, connectedCubes);
        connectedCubes.RemoveWhere(cube => cube.IsMoving);
        return connectedCubes;
    }

    private void FloodFill(Cube startCube, HashSet<Cube> connectedCubes)
    {
        if (startCube == null ||
            startCube.IsMoving ||
            connectedCubes.Contains(startCube))
            return;
        connectedCubes.Add(startCube);
        foreach (Vector2Int dir in adjacentDirections)
        {
            Vector2Int neighborPos = startCube.GridPosition + dir;
            GridItem neighborItem = gridController.GetGridItem(neighborPos.x, neighborPos.y);
            if (neighborItem is Cube neighborCube &&
                !neighborCube.IsMoving &&
                neighborCube.Color == startCube.Color)
            {
                FloodFill(neighborCube, connectedCubes);
            }
        }
    }

    private static readonly Vector2Int[] adjacentDirections = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0)
    };

    public override void TakeDamage(DamageType damageType)
    {
        if (damageType == DamageType.Blast)
        {
            var connectedGroup = GetConnectedCubes();
            if (connectedGroup.Count >= 5)
            {
                gridController.SpawnTNT(GridPosition);
            }
            HashSet<Vector2Int> blastPositions = new HashSet<Vector2Int>();
            foreach (var cube in connectedGroup)
            {
                foreach (Vector2Int dir in adjacentDirections)
                {
                    Vector2Int adjacentPos = cube.GridPosition + dir;
                    blastPositions.Add(adjacentPos);
                }
            }
            foreach (Vector2Int pos in blastPositions)
            {
                if (gridController.IsValidPosition(pos.x, pos.y))
                {
                    GridItem item = gridController.GetGridItem(pos.x, pos.y);
                    if (item != null && item is not Cube && item is not TNT)
                    {
                        item.TakeDamage(DamageType.Blast);
                    }
                }
            }
            foreach (var cube in connectedGroup)
            {
                Vector2Int pos = cube.GridPosition;
                if (gridController.GetGridItem(pos.x, pos.y) == cube)
                {
                    gridController.SetGridPosition(pos.x, pos.y, null);
                }
                if (cube != this)
                {
                    cube.SpawnDestroyParticles();
                    Destroy(cube.gameObject);
                }
            }
            SpawnDestroyParticles();
            Destroy(gameObject);
            LevelScene.TriggerGridUpdate();
        }
        else
        {
            Vector2Int pos = GridPosition;
            gridController.SetGridPosition(pos.x, pos.y, null);
            SpawnDestroyParticles();
            Destroy(gameObject);
        }
    }
    protected override Sprite GetDestroyParticleSprite()
    {
        CubeSprites currentSprites = cubeColor switch
        {
            CubeColor.Red => redCube,
            CubeColor.Green => greenCube,
            CubeColor.Blue => blueCube,
            CubeColor.Yellow => yellowCube,
            _ => redCube
        };
        return currentSprites.destroyParticle;
    }

    public bool CheckMatchable()
    {
        foreach (Vector2Int dir in adjacentDirections)
        {
            Vector2Int neighborPos = GridPosition + dir;
            GridItem neighborItem = gridController.GetGridItem(neighborPos.x, neighborPos.y);
            if (neighborItem is Cube neighborCube &&
                neighborCube.Color == this.Color)
            {
                return true;
            }
        }
        return false;
    }
}