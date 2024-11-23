public class Box : Obstacle
{
    protected override void Awake()
    {
        maxHealth = 1;
        canTakeDamageFromBlast = true;
        canTakeDamageFromTNT = true;
        base.Awake();
    }
}