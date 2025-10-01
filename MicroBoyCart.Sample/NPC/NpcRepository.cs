using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.NPCs;

public sealed class NpcRepository
{
    public static NpcDefinition[] CreateDefaultNpcs()
    {
        return new[]
        {
            // Overworld NPCs
            new NpcDefinition(
                id: "elder",
                name: "ELDER OAK",
                tileX: 15,
                tileY: 5,
                mapId: "overworld",
                dialogPages: new[]
                {
                    "Welcome young traveler! This world is full of dangers.",
                    "Press A to interact with people and objects.",
                    "Use the arrows to move around. Good luck!"
                },
                behavior: NpcBehavior.Static,
                spriteColorPrimary: Tileset.ColorPathDark,
                spriteColorSecondary: Tileset.ColorStone
            ),

            new NpcDefinition(
                id: "wanderer",
                name: "WANDERER",
                tileX: 12,
                tileY: 15,
                mapId: "overworld",
                dialogPages: new[]
                {
                    "I love to walk around and explore!",
                    "You should try the tall grass... but be careful!"
                },
                behavior: NpcBehavior.Wander,
                spriteColorPrimary: Tileset.ColorGrassHighlight,
                spriteColorSecondary: Tileset.ColorGrassMid,
                wanderRadius: 5
            ),

            new NpcDefinition(
                id: "fisherman",
                name: "OLD FISHER",
                tileX: 22,
                tileY: 26,
                mapId: "overworld",
                dialogPages: new[]
                {
                    "Ahoy! These waters are deep and mysterious.",
                    "Press A when standing on water to learn SURF!",
                    "Then you can cross water tiles safely."
                },
                behavior: NpcBehavior.Static,
                spriteColorPrimary: Tileset.ColorWaterDeep,
                spriteColorSecondary: Tileset.ColorWaterLight
            ),

            new NpcDefinition(
                id: "guard",
                name: "TOWN GUARD",
                tileX: 23,
                tileY: 10,
                mapId: "overworld",
                dialogPages: new[]
                {
                    "This door leads to my house.",
                    "Feel free to visit anytime!"
                },
                behavior: NpcBehavior.Static,
                spriteColorPrimary: Tileset.ColorRug,
                spriteColorSecondary: Tileset.ColorPathDark
            ),

            // House NPCs
            new NpcDefinition(
                id: "house_owner",
                name: "HOMEOWNER",
                tileX: 8,
                tileY: 8,
                mapId: "house",
                dialogPages: new[]
                {
                    "Welcome to my humble home!",
                    "Press START to save your game anywhere.",
                    "Press SELECT to load your saved game."
                },
                behavior: NpcBehavior.Static,
                spriteColorPrimary: Tileset.ColorPathLight,
                spriteColorSecondary: Tileset.ColorPathDark
            ),

            new NpcDefinition(
                id: "house_cat",
                name: "MEOW",
                tileX: 11,
                tileY: 6,
                mapId: "house",
                dialogPages: new[]
                {
                    "Meow! Meow meow!",
                    "...",
                    "The cat seems happy."
                },
                behavior: NpcBehavior.Wander,
                spriteColorPrimary: Tileset.ColorGrassLight,
                spriteColorSecondary: Tileset.ColorGrassMid,
                wanderRadius: 3
            ),
        };
    }
}