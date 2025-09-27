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
        // 0 = Gras, 1 = Weg, 2 = Baum/Wand, 3 = hohes Gras, 4 = Wasser,
        // 5 = Tür/Portal, 6 = Innenboden hell, 7 = Innenwand, 8 = Teppichboden
        const int TILE_W = 8, TILE_H = 8;
        const int MAP_W = 32, MAP_H = 32;

        const byte TILE_ID_GRASS = 0;
        const byte TILE_ID_PATH = 1;
        const byte TILE_ID_TREE = 2;
        const byte TILE_ID_TALL_GRASS = 3;
        const byte TILE_ID_WATER = 4;
        const byte TILE_ID_DOOR = 5;
        const byte TILE_ID_INTERIOR_FLOOR = 6;
        const byte TILE_ID_INTERIOR_WALL = 7;
        const byte TILE_ID_INTERIOR_CARPET = 8;

        // Einfache Karte (Rand = Bäume)
        static readonly byte[,] Map = BuildMap();

        static byte[,] BuildMap()
        {
            var m = new byte[MAP_H, MAP_W];
            for (int y = 0; y < MAP_H; y++)
                for (int x = 0; x < MAP_W; x++)
                    m[y, x] = TILE_ID_GRASS; // Gras

            // Rand als Bäume
            for (int x = 0; x < MAP_W; x++) { m[0, x] = TILE_ID_TREE; m[MAP_H - 1, x] = TILE_ID_TREE; }
            for (int y = 0; y < MAP_H; y++) { m[y, 0] = TILE_ID_TREE; m[y, MAP_W - 1] = TILE_ID_TREE; }

            // Ein paar Wege
            for (int x = 2; x < MAP_W - 2; x++) m[10, x] = TILE_ID_PATH;
            for (int y = 3; y < 20; y++) m[y, 8] = TILE_ID_PATH;
            for (int x = 8; x < 24; x++) m[20, x] = TILE_ID_PATH;

            // Kleine „Mauer“
            for (int y = 5; y < 14; y++) m[y, 16] = TILE_ID_TREE;

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
            return t switch
            {
                TILE_ID_GRASS => true,
                TILE_ID_PATH => true,
                TILE_ID_TALL_GRASS => true,
                TILE_ID_DOOR => true,
                TILE_ID_INTERIOR_FLOOR => true,
                TILE_ID_INTERIOR_CARPET => true,
                TILE_ID_TREE => false,
                TILE_ID_WATER => false,
                TILE_ID_INTERIOR_WALL => false,
                _ => false,
            };
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

        static readonly byte[,] TILE_TALL_GRASS = // hohes Gras
        {
            {0,1,0,1,0,1,0,1},
            {1,2,1,2,1,2,1,2},
            {0,1,0,2,0,1,0,2},
            {1,2,1,3,1,2,1,3},
            {0,1,0,2,0,1,0,2},
            {1,2,1,3,1,2,1,3},
            {0,1,0,2,0,1,0,2},
            {1,2,1,2,1,2,1,2},
        };

        static readonly byte[,] TILE_WATER = // Wasser
        {
            {1,2,2,1,1,2,2,1},
            {2,3,3,2,2,3,3,2},
            {1,2,2,1,1,2,2,1},
            {2,3,3,2,2,3,3,2},
            {1,2,2,1,1,2,2,1},
            {2,3,3,2,2,3,3,2},
            {1,2,2,1,1,2,2,1},
            {2,3,3,2,2,3,3,2},
        };

        static readonly byte[,] TILE_DOOR = // Tür/Portal
        {
            {2,2,2,2,2,2,2,2},
            {2,3,3,3,3,3,3,2},
            {2,3,1,1,1,1,3,2},
            {2,3,1,2,2,1,3,2},
            {2,3,1,2,2,1,3,2},
            {2,3,1,1,1,1,3,2},
            {2,3,3,3,3,3,3,2},
            {2,2,2,2,2,2,2,2},
        };

        static readonly byte[,] TILE_INTERIOR_FLOOR = // Innenboden helles Schachbrett
        {
            {1,1,2,2,1,1,2,2},
            {1,1,2,2,1,1,2,2},
            {2,2,1,1,2,2,1,1},
            {2,2,1,1,2,2,1,1},
            {1,1,2,2,1,1,2,2},
            {1,1,2,2,1,1,2,2},
            {2,2,1,1,2,2,1,1},
            {2,2,1,1,2,2,1,1},
        };

        static readonly byte[,] TILE_INTERIOR_CARPET = // Innenboden dunkel/Teppich
        {
            {2,2,3,3,2,2,3,3},
            {2,2,3,3,2,2,3,3},
            {3,3,2,2,3,3,2,2},
            {3,3,2,2,3,3,2,2},
            {2,2,3,3,2,2,3,3},
            {2,2,3,3,2,2,3,3},
            {3,3,2,2,3,3,2,2},
            {3,3,2,2,3,3,2,2},
        };

        static readonly byte[,] TILE_INTERIOR_WALL = // Innenwand
        {
            {3,3,3,3,3,3,3,3},
            {3,2,2,2,2,2,2,3},
            {3,2,3,3,3,3,2,3},
            {3,2,3,3,3,3,2,3},
            {3,2,3,3,3,3,2,3},
            {3,2,3,3,3,3,2,3},
            {3,2,2,2,2,2,2,3},
            {3,3,3,3,3,3,3,3},
        };

        void BlitTile(Span<byte> fb, int dx, int dy, byte id)
        {
            var tile = id switch
            {
                TILE_ID_GRASS => TILE_GRASS,
                TILE_ID_PATH => TILE_PATH,
                TILE_ID_TREE => TILE_TREE,
                TILE_ID_TALL_GRASS => TILE_TALL_GRASS,
                TILE_ID_WATER => TILE_WATER,
                TILE_ID_DOOR => TILE_DOOR,
                TILE_ID_INTERIOR_FLOOR => TILE_INTERIOR_FLOOR,
                TILE_ID_INTERIOR_CARPET => TILE_INTERIOR_CARPET,
                TILE_ID_INTERIOR_WALL => TILE_INTERIOR_WALL,
                _ => TILE_GRASS
            };
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
