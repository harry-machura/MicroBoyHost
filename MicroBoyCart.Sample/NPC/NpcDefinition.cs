namespace MicroBoyCart.Sample.NPCs;

public enum NpcBehavior
{
    Static,      // Steht still
    Wander,      // Läuft zufällig herum
    Patrol       // Läuft feste Route
}

public sealed class NpcDefinition
{
    public string Id { get; }
    public string Name { get; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string MapId { get; }
    public NpcBehavior Behavior { get; }
    public string[] DialogPages { get; }
    public byte SpriteColorPrimary { get; }
    public byte SpriteColorSecondary { get; }

    // Für Patrol-NPCs
    public (int x, int y)[]? PatrolPath { get; }

    // Für Wander-NPCs
    public double WanderCooldown { get; set; }
    public int WanderRadius { get; }

    public NpcDefinition(
        string id,
        string name,
        int tileX,
        int tileY,
        string mapId,
        string[] dialogPages,
        NpcBehavior behavior = NpcBehavior.Static,
        byte spriteColorPrimary = 9,    // Default: ColorRug (rot)
        byte spriteColorSecondary = 5,  // Default: ColorPathDark (braun)
        (int x, int y)[]? patrolPath = null,
        int wanderRadius = 3)
    {
        Id = id;
        Name = name;
        TileX = tileX;
        TileY = tileY;
        MapId = mapId;
        DialogPages = dialogPages;
        Behavior = behavior;
        SpriteColorPrimary = spriteColorPrimary;
        SpriteColorSecondary = spriteColorSecondary;
        PatrolPath = patrolPath;
        WanderRadius = wanderRadius;
        WanderCooldown = 0;
    }
}