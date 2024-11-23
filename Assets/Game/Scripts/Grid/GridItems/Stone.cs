public class Stone : Obstacle
{
    protected override void Awake()
    {
        maxHealth = 1;
        canTakeDamageFromBlast = false;
        canTakeDamageFromTNT = true;
        base.Awake();
    }
}