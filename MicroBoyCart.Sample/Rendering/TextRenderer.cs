using System.Collections.Generic;
using MicroBoy;

namespace MicroBoyCart.Sample.Rendering;

public sealed class TextRenderer
{
    private readonly Dictionary<char, byte[,]> glyphs = new()
    {
        ['A'] = new byte[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
        ['B'] = new byte[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 } },
        ['C'] = new byte[,] { { 0, 1, 1 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 0, 1, 1 } },
        ['D'] = new byte[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 0 } },
        ['E'] = new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
        ['F'] = new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 } },
        ['G'] = new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 } },
        ['H'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
        ['I'] = new byte[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 1, 1, 1 } },
        ['J'] = new byte[,] { { 0, 0, 1 }, { 0, 0, 1 }, { 0, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
        ['K'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 } },
        ['L'] = new byte[,] { { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
        ['M'] = new byte[,] { { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
        ['N'] = new byte[,] { { 1, 0, 1 }, { 1, 1, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
        ['O'] = new byte[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
        ['P'] = new byte[,] { { 1, 1, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 0 }, { 1, 0, 0 } },
        ['Q'] = new byte[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 1 }, { 0, 0, 1 } },
        ['R'] = new byte[,] { { 1, 1, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 1, 0 }, { 1, 0, 1 } },
        ['S'] = new byte[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 1 }, { 0, 0, 1 }, { 1, 1, 1 } },
        ['T'] = new byte[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } },
        ['U'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 } },
        ['V'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
        ['W'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 } },
        ['X'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 } },
        ['Y'] = new byte[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } },
        ['Z'] = new byte[,] { { 1, 1, 1 }, { 0, 0, 1 }, { 0, 1, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
        ['!'] = new byte[,] { { 1 }, { 1 }, { 1 }, { 0 }, { 1 } },
        [' '] = new byte[,] { { 0 }, { 0 }, { 0 }, { 0 }, { 0 } },
    };

    public void DrawText(Span<byte> frame, int startX, int startY, string? text, byte color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int offset = 0;
        foreach (char character in text)
        {
            DrawChar(frame, startX + offset, startY, character, color);
            offset += 4;
        }
    }

    public int MeasureWidth(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return text.Length * 4;
    }

    private void DrawChar(Span<byte> frame, int startX, int startY, char character, byte color)
    {
        if (!glyphs.TryGetValue(character, out var pattern))
        {
            return;
        }

        int height = pattern.GetLength(0);
        int width = pattern.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            int rowY = startY + y;
            if ((uint)rowY >= MicroBoySpec.H)
            {
                continue;
            }

            int framebufferRow = rowY * MicroBoySpec.W;
            for (int x = 0; x < width; x++)
            {
                if (pattern[y, x] == 0)
                {
                    continue;
                }

                int rowX = startX + x;
                if ((uint)rowX >= MicroBoySpec.W)
                {
                    continue;
                }

                frame[framebufferRow + rowX] = color;
            }
        }
    }
}
