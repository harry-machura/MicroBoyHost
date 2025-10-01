using System;
using System.Collections.Generic;
using MicroBoy;
using MicroBoyCart.Sample.NPCs;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.Rendering;

public sealed class NpcRenderer
{
    public void Render(Span<byte> frame, IReadOnlyList<NpcDefinition> npcs, int cameraX, int cameraY)
    {
        foreach (var npc in npcs)
        {
            int screenX = npc.TileX * Tileset.TileWidth - cameraX;
            int screenY = npc.TileY * Tileset.TileHeight - cameraY;

            // Nur zeichnen wenn auf dem Bildschirm
            if (screenX + Tileset.TileWidth < 0 || screenX >= MicroBoySpec.W ||
                screenY + Tileset.TileHeight < 0 || screenY >= MicroBoySpec.H)
            {
                continue;
            }

            DrawNpc(frame, screenX, screenY, npc.SpriteColorPrimary, npc.SpriteColorSecondary);
        }
    }

    private static void DrawNpc(Span<byte> frame, int drawX, int drawY, byte primaryColor, byte secondaryColor)
    {
        // Einfacher NPC-Sprite (ähnlich wie Player, aber andere Farben)
        var sprite = new byte[,]
        {
            {0, 0, secondaryColor, secondaryColor, secondaryColor, secondaryColor, 0, 0},
            {0, secondaryColor, primaryColor, primaryColor, primaryColor, primaryColor, secondaryColor, 0},
            {secondaryColor, primaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor, primaryColor, secondaryColor},
            {secondaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor},
            {0, secondaryColor, secondaryColor, primaryColor, primaryColor, secondaryColor, secondaryColor, 0},
            {0, secondaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor, secondaryColor, 0},
            {0, secondaryColor, primaryColor, primaryColor, primaryColor, primaryColor, secondaryColor, 0},
            {0, 0, secondaryColor, secondaryColor, secondaryColor, secondaryColor, 0, 0},
        };

        for (int y = 0; y < Tileset.TileHeight; y++)
        {
            int rowY = drawY + y;
            if ((uint)rowY >= MicroBoySpec.H) continue;
            int row = rowY * MicroBoySpec.W;

            for (int x = 0; x < Tileset.TileWidth; x++)
            {
                int rowX = drawX + x;
                if ((uint)rowX >= MicroBoySpec.W) continue;

                byte color = sprite[y, x];
                if (color != 0)
                {
                    frame[row + rowX] = color;
                }
            }
        }
    }
}