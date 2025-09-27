using MicroBoy;
using System;

namespace MicroBoyCart.Sample
{
    // Mini-"Pokémon"-Cartridge: 8x8-Tiles, Grid-Movement, Kollision
    public sealed class MyPokemonLikeCart : ICartridge
    {
        public string Title => "MicroBoy Demo Map";
        public string Author => "Harry";

        // --- Spielfeld-Setup ---
        // 0 = Gras, 1 = Weg, 2 = Baum/Wand
        const int TILE_W = 8, TILE_H = 8;
        const int MAP_W = 32, MAP_H = 32;

        // Einfache Karte (Rand = Bäume)
        static readonly byte[,] Map = BuildMap();

        static byte[,] BuildMap()
        {
            var m = new byte[MAP_H, MAP_W];
            for (int y = 0; y < MAP_H; y++)
                for (int x = 0; x < MAP_W; x++)
                    m[y, x] = 0; // Gras

            // Rand als Bäume
            for (int x = 0; x < MAP_W; x++) { m[0, x] = 2; m[MAP_H - 1, x] = 2; }
            for (int y = 0; y < MAP_H; y++) { m[y, 0] = 2; m[y, MAP_W - 1] = 2; }

            // Ein paar Wege
            for (int x = 2; x < MAP_W - 2; x++) m[10, x] = 1;
            for (int y = 3; y < 20; y++) m[y, 8] = 1;
            for (int x = 8; x < 24; x++) m[20, x] = 1;

            // Kleine „Mauer“
            for (int y = 5; y < 14; y++) m[y, 16] = 2;

            return m;
        }

        // Spielerposition in Tile-Koordinaten
        int pTileX = 5, pTileY = 10;
        int px, py;                  // Pixel-Position (für weiches Gleiten)
        int targetPx, targetPy;      // Zielpixel beim Schritt
        bool isMoving;               // läuft gerade ein Schritt?
        int stepSpeedPx = 2;         // Pixel pro Frame (~120px/s bei 60 FPS)

        // Richtung (0=unten,1=links,2=rechts,3=oben) für Sprite-Flip/Varianz
        int dir;

        public void Init()
        {
            // Initial auf Tile zentrieren
            px = pTileX * TILE_W;
            py = pTileY * TILE_H;
            targetPx = px; targetPy = py;
            isMoving = false;
            dir = 0;
        }

        public void Update(Input input, double dt)
        {
            // Falls wir gerade unterwegs sind -> zum Ziel gleiten
            if (isMoving)
            {
                // horizontale Annäherung
                if (px < targetPx) px = Math.Min(targetPx, px + stepSpeedPx);
                if (px > targetPx) px = Math.Max(targetPx, px - stepSpeedPx);
                if (py < targetPy) py = Math.Min(targetPy, py + stepSpeedPx);
                if (py > targetPy) py = Math.Max(targetPy, py - stepSpeedPx);

                if (px == targetPx && py == targetPy)
                {
                    isMoving = false;
                    pTileX = px / TILE_W;
                    pTileY = py / TILE_H;
                }
                return;
            }

            // Keine Bewegung aktiv -> Eingaben im Grid prüfen (tileweise)
            int nx = pTileX, ny = pTileY;
            if (input.IsDown(Buttons.Left)) { nx = pTileX - 1; dir = 1; }
            else if (input.IsDown(Buttons.Right)) { nx = pTileX + 1; dir = 2; }
            else if (input.IsDown(Buttons.Up)) { ny = pTileY - 1; dir = 3; }
            else if (input.IsDown(Buttons.Down)) { ny = pTileY + 1; dir = 0; }
            else return; // keine Richtung gedrückt

            if (IsWalkable(nx, ny))
            {
                pTileX = nx; pTileY = ny;
                targetPx = pTileX * TILE_W;
                targetPy = pTileY * TILE_H;
                isMoving = true;
            }
        }

        bool IsWalkable(int tx, int ty)
        {
            if (tx < 0 || ty < 0 || tx >= MAP_W || ty >= MAP_H) return false;
            byte t = Map[ty, tx];
            return t != 2; // 2 = Baum/Wand = gesperrt
        }

        public void Render(Span<byte> frame)
        {
            // Kamera zentriert den Spieler (mit Clamping)
            int camX = px - MicroBoySpec.W / 2;
            int camY = py - MicroBoySpec.H / 2;
            camX = Math.Max(0, Math.Min(camX, MAP_W * TILE_W - MicroBoySpec.W));
            camY = Math.Max(0, Math.Min(camY, MAP_H * TILE_H - MicroBoySpec.H));

            // Hintergrund löschen (Farbe 0)
            frame.Fill(0);

            // Sichtbare Tiles berechnen
            int firstTileX = camX / TILE_W;
            int firstTileY = camY / TILE_H;
            int offX = -(camX % TILE_W);
            int offY = -(camY % TILE_H);

            // Tiles zeichnen
            for (int ty = 0, my = firstTileY; my < MAP_H && ty < MicroBoySpec.H; my++, ty += TILE_H)
            {
                int drawY = offY + ty;
                if (drawY >= MicroBoySpec.H) break;

                for (int tx = 0, mx = firstTileX; mx < MAP_W && tx < MicroBoySpec.W; mx++, tx += TILE_W)
                {
                    int drawX = offX + tx;
                    if (drawX >= MicroBoySpec.W) break;

                    byte id = Map[my, mx];
                    BlitTile(frame, drawX, drawY, id);
                }
            }

            // Spieler (8x8) zeichnen – einfache 2-Frame „Animation“
            DrawPlayer(frame, px - camX, py - camY);
        }

        // --- Tileset: 3 einfache Tiles in 4 Farben (0..3) ---

        static readonly byte[,] TILE_GRASS = // feines Muster
        {
            {0,0,0,0,0,0,0,0},
            {0,1,0,0,1,0,0,0},
            {0,0,0,1,0,0,0,1},
            {0,0,1,0,0,0,1,0},
            {0,0,0,0,0,1,0,0},
            {0,1,0,0,0,0,0,0},
            {0,0,0,1,0,0,0,0},
            {0,0,0,0,0,0,1,0},
        };

        static readonly byte[,] TILE_PATH = // Weg (hell)
        {
            {1,1,1,1,1,1,1,1},
            {1,2,2,2,2,2,2,1},
            {1,2,3,2,3,2,2,1},
            {1,2,2,2,2,2,2,1},
            {1,2,3,2,3,2,2,1},
            {1,2,2,2,2,2,2,1},
            {1,2,3,2,3,2,2,1},
            {1,1,1,1,1,1,1,1},
        };

        static readonly byte[,] TILE_TREE = // Baum/Wand (dunkel)
        {
            {2,2,2,2,2,2,2,2},
            {2,3,3,3,3,3,3,2},
            {2,3,2,2,2,2,3,2},
            {2,3,2,3,3,2,3,2},
            {2,3,2,3,3,2,3,2},
            {2,3,2,2,2,2,3,2},
            {2,3,3,3,3,3,3,2},
            {2,2,2,2,2,2,2,2},
        };

        void BlitTile(Span<byte> fb, int dx, int dy, byte id)
        {
            var tile = id switch { 1 => TILE_PATH, 2 => TILE_TREE, _ => TILE_GRASS };
            for (int y = 0; y < TILE_H; y++)
            {
                int ry = dy + y; if ((uint)ry >= MicroBoySpec.H) continue;
                int row = ry * MicroBoySpec.W;
                for (int x = 0; x < TILE_W; x++)
                {
                    int rx = dx + x; if ((uint)rx >= MicroBoySpec.W) continue;
                    byte c = tile[y, x];
                    fb[row + rx] = c;
                }
            }
        }

        void DrawPlayer(Span<byte> fb, int dx, int dy)
        {
            // Zwei ganz simple Frames (blinkender Körper)
            byte[,] f0 =
            {
                {0,0,2,2,2,2,0,0},
                {0,2,3,3,3,3,2,0},
                {2,3,3,3,3,3,3,2},
                {2,3,3,3,3,3,3,2},
                {0,2,3,2,2,3,2,0},
                {0,2,3,3,3,3,2,0},
                {0,2,2,0,0,2,2,0},
                {0,0,0,2,2,0,0,0},
            };
            byte[,] f1 =
            {
                {0,0,2,2,2,2,0,0},
                {0,2,3,3,3,3,2,0},
                {2,3,3,3,3,3,3,2},
                {2,3,3,3,3,3,3,2},
                {0,2,3,2,2,3,2,0},
                {0,2,3,3,3,3,2,0},
                {0,0,2,2,2,2,0,0},
                {0,2,0,0,0,0,2,0},
            };

            var sprite = ((px + py) / 8 % 2 == 0) ? f0 : f1;

            for (int y = 0; y < 8; y++)
            {
                int ry = dy + y; if ((uint)ry >= MicroBoySpec.H) continue;
                int row = ry * MicroBoySpec.W;
                for (int x = 0; x < 8; x++)
                {
                    int rx = dx + x; if ((uint)rx >= MicroBoySpec.W) continue;
                    byte c = sprite[y, x];
                    if (c != 0) fb[row + rx] = c;
                }
            }
        }
    }
}
