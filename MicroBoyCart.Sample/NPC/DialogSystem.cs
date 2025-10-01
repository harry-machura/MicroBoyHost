using MicroBoy;
using System;

namespace MicroBoyCart.Sample.NPCs;

public sealed class DialogSystem
{
    private bool isActive;
    private string[] currentPages = Array.Empty<string>();
    private int currentPageIndex;
    private string currentNpcName = string.Empty;
    private bool waitingForInput;
    private const double InputCooldown = 0.2;
    private double inputTimer;

    public bool IsActive => isActive;

    public void StartDialog(string npcName, string[] pages)
    {
        if (pages == null || pages.Length == 0)
            return;

        currentNpcName = npcName;
        currentPages = pages;
        currentPageIndex = 0;
        isActive = true;
        waitingForInput = true;
        inputTimer = InputCooldown;
    }

    public void Update(Input input, double dt)
    {
        if (!isActive)
            return;

        if (inputTimer > 0)
        {
            inputTimer = Math.Max(0, inputTimer - dt);
            return;
        }

        if (waitingForInput && input.IsDown(Buttons.A))
        {
            waitingForInput = false;
            currentPageIndex++;

            if (currentPageIndex >= currentPages.Length)
            {
                CloseDialog();
            }
            else
            {
                inputTimer = InputCooldown;
                waitingForInput = true;
            }
        }
    }

    public void Render(Span<byte> frame)
    {
        if (!isActive || currentPageIndex >= currentPages.Length)
            return;

        const int boxWidth = 140;
        const int boxHeight = 40;
        const int boxX = (MicroBoySpec.W - boxWidth) / 2;
        const int boxY = MicroBoySpec.H - boxHeight - 10;

        // Box zeichnen
        DrawDialogBox(frame, boxX, boxY, boxWidth, boxHeight);

        // NPC Name zeichnen (oben links in der Box)
        DrawText(frame, boxX + 4, boxY + 3, currentNpcName, 5); // ColorPathDark

        // Dialog-Text zeichnen
        string currentText = currentPages[currentPageIndex];
        DrawWrappedText(frame, boxX + 4, boxY + 12, boxWidth - 8, currentText);

        // "Press A" Indikator (wenn mehr Seiten kommen)
        if (currentPageIndex < currentPages.Length - 1)
        {
            DrawText(frame, boxX + boxWidth - 30, boxY + boxHeight - 8, "[A]", 2);
        }
        else
        {
            DrawText(frame, boxX + boxWidth - 40, boxY + boxHeight - 8, "[A] OK", 2);
        }
    }

    private void CloseDialog()
    {
        isActive = false;
        currentPages = Array.Empty<string>();
        currentPageIndex = 0;
        currentNpcName = string.Empty;
    }

    private static void DrawDialogBox(Span<byte> frame, int x, int y, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            int rowY = y + dy;
            if ((uint)rowY >= MicroBoySpec.H) continue;
            int row = rowY * MicroBoySpec.W;

            for (int dx = 0; dx < width; dx++)
            {
                int rowX = x + dx;
                if ((uint)rowX >= MicroBoySpec.W) continue;

                // Rahmen: dunkel, Inneres: hell
                if (dy == 0 || dy == height - 1 || dx == 0 || dx == width - 1)
                    frame[row + rowX] = 5; // ColorPathDark
                else if (dy == 1 || dy == height - 2 || dx == 1 || dx == width - 2)
                    frame[row + rowX] = 8; // ColorStone
                else
                    frame[row + rowX] = 4; // ColorPathLight
            }
        }
    }

    private static void DrawWrappedText(Span<byte> frame, int startX, int startY, int maxWidth, string text)
    {
        const int charWidth = 4;
        const int lineHeight = 7;
        int currentX = startX;
        int currentY = startY;

        foreach (char c in text)
        {
            if (c == '\n' || currentX + charWidth > startX + maxWidth)
            {
                currentX = startX;
                currentY += lineHeight;
                if (c == '\n') continue;
            }

            if (c != ' ' || currentX != startX) // Skip leading spaces
            {
                DrawChar(frame, currentX, currentY, c, 5); // ColorPathDark
                currentX += charWidth;
            }
        }
    }

    private static void DrawText(Span<byte> frame, int x, int y, string text, byte color)
    {
        int offset = 0;
        foreach (char c in text)
        {
            DrawChar(frame, x + offset, y, c, color);
            offset += 4;
        }
    }

    private static void DrawChar(Span<byte> frame, int x, int y, char c, byte color)
    {
        byte[,]? pattern = c switch
        {
            'A' => new byte[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'B' => new byte[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 } },
            'C' => new byte[,] { { 0, 1, 1 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 0, 1, 1 } },
            'D' => new byte[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 0 } },
            'E' => new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
            'F' => new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 } },
            'G' => new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 } },
            'H' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'I' => new byte[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 1, 1, 1 } },
            'J' => new byte[,] { { 0, 0, 1 }, { 0, 0, 1 }, { 0, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
            'K' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'L' => new byte[,] { { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
            'M' => new byte[,] { { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'N' => new byte[,] { { 1, 0, 1 }, { 1, 1, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'O' => new byte[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
            'P' => new byte[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 } },
            'Q' => new byte[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 0 }, { 0, 1, 1 } },
            'R' => new byte[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'S' => new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 1 }, { 0, 0, 1 }, { 1, 1, 1 } },
            'T' => new byte[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } },
            'U' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 } },
            'V' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
            'W' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 } },
            'X' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 } },
            'Y' => new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } },
            'Z' => new byte[,] { { 1, 1, 1 }, { 0, 0, 1 }, { 0, 1, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
            '!' => new byte[,] { { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 0, 0, 0 }, { 1, 0, 0 } },
            '?' => new byte[,] { { 1, 1, 0 }, { 0, 0, 1 }, { 0, 1, 0 }, { 0, 0, 0 }, { 0, 1, 0 } },
            '.' => new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 1, 0, 0 } },
            ',' => new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 1, 0 }, { 1, 0, 0 } },
            '[' => new byte[,] { { 1, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 1, 0 } },
            ']' => new byte[,] { { 0, 1, 1 }, { 0, 0, 1 }, { 0, 0, 1 }, { 0, 0, 1 }, { 0, 1, 1 } },
            ' ' => new byte[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } },
            _ => null
        };

        if (pattern == null) return;

        for (int py = 0; py < 5; py++)
        {
            int ry = y + py;
            if ((uint)ry >= MicroBoySpec.H) continue;
            int row = ry * MicroBoySpec.W;

            for (int px = 0; px < 3; px++)
            {
                int rx = x + px;
                if ((uint)rx >= MicroBoySpec.W) continue;

                if (pattern[py, px] == 1)
                    frame[row + rx] = color;
            }
        }
    }
}