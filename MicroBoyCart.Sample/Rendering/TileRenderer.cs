using System;
using MicroBoy;
using MicroBoyCart.Sample.Gameplay;
using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.Rendering;

public sealed class TileRenderer
{
    public void Render(Span<byte> frame, MapDefinition map, PlayerState playerState)
    {
        if (map is null)
        {
            return;
        }

        int mapPixelWidth = map.Width * Tileset.TileWidth;
        int mapPixelHeight = map.Height * Tileset.TileHeight;

        int camX = playerState.PixelX - MicroBoySpec.W / 2;
        int camY = playerState.PixelY - MicroBoySpec.H / 2;

        camX = Math.Clamp(camX, 0, Math.Max(0, mapPixelWidth - MicroBoySpec.W));
        camY = Math.Clamp(camY, 0, Math.Max(0, mapPixelHeight - MicroBoySpec.H));

        frame.Fill(0);

        int firstTileX = camX / Tileset.TileWidth;
        int firstTileY = camY / Tileset.TileHeight;
        int offsetX = -(camX % Tileset.TileWidth);
        int offsetY = -(camY % Tileset.TileHeight);

        for (int tileY = 0, mapY = firstTileY; mapY < map.Height && tileY < MicroBoySpec.H; mapY++, tileY += Tileset.TileHeight)
        {
            int drawY = offsetY + tileY;
            if (drawY >= MicroBoySpec.H)
            {
                break;
            }

            for (int tileX = 0, mapX = firstTileX; mapX < map.Width && tileX < MicroBoySpec.W; mapX++, tileX += Tileset.TileWidth)
            {
                int drawX = offsetX + tileX;
                if (drawX >= MicroBoySpec.W)
                {
                    break;
                }

                byte baseId = map.GetGround(mapX, mapY);
                byte overlayId = map.GetOverlay(mapX, mapY);
                BlitTile(frame, drawX, drawY, baseId, overlayId);
            }
        }

        DrawPlayer(frame, playerState.PixelX - camX, playerState.PixelY - camY, playerState.PixelX, playerState.PixelY);
    }

    private static void BlitTile(Span<byte> framebuffer, int destX, int destY, byte baseId, byte overlayId)
    {
        DrawTileLayer(framebuffer, destX, destY, baseId, transparent: false);
        if (overlayId != Tileset.TileNone)
        {
            DrawTileLayer(framebuffer, destX, destY, overlayId, transparent: true);
        }
    }

    private static void DrawTileLayer(Span<byte> framebuffer, int destX, int destY, byte tileId, bool transparent)
    {
        var tile = Tileset.GetTileSprite(tileId);
        for (int y = 0; y < Tileset.TileHeight; y++)
        {
            int rowY = destY + y;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int x = 0; x < Tileset.TileWidth; x++)
            {
                int rowX = destX + x;
                if ((uint)rowX >= MicroBoySpec.W)
                {
                    continue;
                }

                byte color = tile[y, x];
                if (transparent && color == 0)
                {
                    continue;
                }

                framebuffer[framebufferRow + rowX] = color;
            }
        }
    }

    private static void DrawPlayer(Span<byte> framebuffer, int drawX, int drawY, int worldX, int worldY)
    {
        var sprite = Tileset.GetPlayerFrame(worldX, worldY);
        for (int y = 0; y < Tileset.TileHeight; y++)
        {
            int rowY = drawY + y;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int x = 0; x < Tileset.TileWidth; x++)
            {
                int rowX = drawX + x;
                if ((uint)rowX >= MicroBoySpec.W)
                {
                    continue;
                }

                byte color = sprite[y, x];
                if (color != 0)
                {
                    framebuffer[framebufferRow + rowX] = color;
                }
            }
        }
    }
}
