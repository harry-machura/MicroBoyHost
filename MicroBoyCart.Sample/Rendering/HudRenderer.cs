using System;
using MicroBoy;
using MicroBoyCart.Sample.Gameplay;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.Rendering;

public sealed class HudRenderer
{
    private readonly TextRenderer textRenderer = new();

    public void Render(Span<byte> frame, PlayerState playerState, bool showSaveMessage, string? message)
    {
        DrawHealthBar(frame, playerState.MaxHealth, playerState.CurrentHealth);

        if (showSaveMessage && !string.IsNullOrWhiteSpace(message))
        {
            DrawSaveMessage(frame, message);
        }
    }

    private static void DrawHealthBar(Span<byte> framebuffer, int maxHealth, int currentHealth)
    {
        const int startX = 4;
        const int startY = 4;
        const int spacing = 2;
        const int heartWidth = 6;

        for (int i = 0; i < maxHealth; i++)
        {
            int drawX = startX + i * (heartWidth + spacing);
            DrawHeartSprite(framebuffer, drawX, startY, i < currentHealth);
        }
    }

    private static void DrawHeartSprite(Span<byte> framebuffer, int drawX, int drawY, bool filled)
    {
        var sprite = filled ? Tileset.HeartFull : Tileset.HeartEmpty;
        int height = sprite.GetLength(0);
        int width = sprite.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            int rowY = drawY + y;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int x = 0; x < width; x++)
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

    private void DrawSaveMessage(Span<byte> frame, string message)
    {
        const int boxWidth = 80;
        const int boxHeight = 16;
        const int boxX = (MicroBoySpec.W - boxWidth) / 2;
        const int boxY = 20;

        for (int y = 0; y < boxHeight; y++)
        {
            int rowY = boxY + y;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int x = 0; x < boxWidth; x++)
            {
                int rowX = boxX + x;
                if ((uint)rowX >= MicroBoySpec.W)
                {
                    continue;
                }

                frame[framebufferRow + rowX] = (y == 0 || y == boxHeight - 1 || x == 0 || x == boxWidth - 1)
                    ? Tileset.ColorPathDark
                    : Tileset.ColorPathLight;
            }
        }

        textRenderer.DrawText(frame, boxX + 8, boxY + 5, message, Tileset.ColorPathDark);
    }
}
