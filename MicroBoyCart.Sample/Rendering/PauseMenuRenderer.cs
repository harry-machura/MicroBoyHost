using MicroBoy;
using MicroBoyCart.Sample.Tiles;
using MicroBoyCart.Sample.UI;

namespace MicroBoyCart.Sample.Rendering;

public sealed class PauseMenuRenderer
{
    private readonly TextRenderer textRenderer = new();

    public void Render(Span<byte> frame, PauseMenuState pauseMenuState)
    {
        if (!pauseMenuState.IsOpen)
        {
            return;
        }

        const int panelWidth = 120;
        const int panelHeight = 74;
        int originX = (MicroBoySpec.W - panelWidth) / 2;
        int originY = (MicroBoySpec.H - panelHeight) / 2;

        DrawPanel(frame, originX, originY, panelWidth, panelHeight);

        int titleX = originX + 12;
        int titleY = originY + 8;
        textRenderer.DrawText(frame, titleX, titleY, "PAUSE MENU", Tileset.ColorPathDark);

        int entriesStartY = originY + 24;
        int entriesStartX = originX + 12;
        int entrySpacing = 16;

        for (int i = 0; i < pauseMenuState.Entries.Count; i++)
        {
            var entry = pauseMenuState.Entries[i];
            int entryY = entriesStartY + i * entrySpacing;
            bool isSelected = i == pauseMenuState.SelectedIndex;

            if (isSelected)
            {
                DrawHighlight(frame, originX + 8, entryY - 2, panelWidth - 16, 12);
            }

            textRenderer.DrawText(frame, entriesStartX, entryY, entry.Title, Tileset.ColorPathDark);

            if (!string.IsNullOrWhiteSpace(entry.Subtext))
            {
                textRenderer.DrawText(frame, entriesStartX + 4, entryY + 6, entry.Subtext, Tileset.ColorGrassHighlight);
            }
        }
    }

    private static void DrawPanel(Span<byte> frame, int x, int y, int width, int height)
    {
        for (int offsetY = 0; offsetY < height; offsetY++)
        {
            int rowY = y + offsetY;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int offsetX = 0; offsetX < width; offsetX++)
            {
                int rowX = x + offsetX;
                if ((uint)rowX >= MicroBoySpec.W)
                {
                    continue;
                }

                bool isBorder = offsetY == 0 || offsetY == height - 1 || offsetX == 0 || offsetX == width - 1;
                frame[framebufferRow + rowX] = isBorder ? Tileset.ColorPathDark : Tileset.ColorPathLight;
            }
        }
    }

    private static void DrawHighlight(Span<byte> frame, int x, int y, int width, int height)
    {
        for (int offsetY = 0; offsetY < height; offsetY++)
        {
            int rowY = y + offsetY;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int offsetX = 0; offsetX < width; offsetX++)
            {
                int rowX = x + offsetX;
                if ((uint)rowX >= MicroBoySpec.W)
                {
                    continue;
                }

                frame[framebufferRow + rowX] = Tileset.ColorRug;
            }
        }
    }
}
