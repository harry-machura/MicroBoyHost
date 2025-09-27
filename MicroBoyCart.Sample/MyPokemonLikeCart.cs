using MicroBoy;
using System;
using System.Collections.Generic;

namespace MicroBoyCart.Sample
{
    // Mini-"Pokémon"-Cartridge: 8x8-Tiles, Grid-Movement, Kollision
    public sealed class MyPokemonLikeCart : ICartridge
    {
        public string Title => "MicroBoy Demo Map";
        public string Author => "Harry";

        // --- Spielfeld-Setup ---
        const int TILE_W = 8, TILE_H = 8;

        const byte TILE_GRASS_ID = 0;
        const byte TILE_PATH_ID = 1;
        const byte TILE_TREE_ID = 2;
        const byte TILE_TALL_GRASS_ID = 3;
        const byte TILE_WATER_ID = 4;
        const byte TILE_DOOR_ID = 5;
        const byte TILE_FLOOR_ID = 6;
        const byte TILE_WALL_ID = 7;
        const byte TILE_RUG_ID = 8;
        const byte TILE_NONE = byte.MaxValue;

        static readonly Dictionary<string, MapDefinition> Maps = BuildMaps();

        // Spielerposition in Tile-Koordinaten
        int pTileX = 5, pTileY = 10;
        int px, py;                  // Pixel-Position (für weiches Gleiten)
        int targetPx, targetPy;      // Zielpixel beim Schritt
        bool isMoving;               // läuft gerade ein Schritt?
        int stepSpeedPx = 2;         // Pixel pro Frame (~120px/s bei 60 FPS)

        // Richtung (0=unten,1=links,2=rechts,3=oben) für Sprite-Flip/Varianz
        int dir;

        MapDefinition currentMap = null!;
        string currentMapId = string.Empty;
        WarpPoint? pendingWarp;
        bool hasSurfAbility;

        public void Init()
        {
            currentMapId = "overworld";
            currentMap = Maps[currentMapId];
            pendingWarp = null;
            hasSurfAbility = false;

            pTileX = 5;
            pTileY = 10;
            // Initial auf Tile zentrieren
            px = pTileX * TILE_W;
            py = pTileY * TILE_H;
            targetPx = px; targetPy = py;
            isMoving = false;
            dir = 0;
        }

        public void Update(Input input, double dt)
        {
            if (input.IsDown(Buttons.A))
                hasSurfAbility = true; // Debug: A schaltet Surf-Fähigkeit frei

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

                    if (pendingWarp is { } warp2)
                    {
                        pendingWarp = null;
                        ExecuteWarp(warp2);
                    }
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

            if (IsWalkable(nx, ny, out var warp))
            {
                pTileX = nx; pTileY = ny;
                targetPx = pTileX * TILE_W;
                targetPy = pTileY * TILE_H;
                isMoving = true;
                pendingWarp = warp.IsValid ? warp : null;
            }
            else
            {
                pendingWarp = null;
            }
        }

        bool IsWalkable(int tx, int ty, out WarpPoint warp)
        {
            warp = WarpPoint.None;
            if (currentMap is null) return false;
            if (tx < 0 || ty < 0 || tx >= currentMap.Width || ty >= currentMap.Height) return false;

            byte overlayId = currentMap.GetOverlay(tx, ty);
            if (overlayId != TILE_NONE)
            {
                var overlayInfo = GetTileInfo(overlayId);
                switch (overlayInfo.Collision)
                {
                    case TileCollisionType.Blocked:
                        return false;
                    case TileCollisionType.Water:
                        return hasSurfAbility;
                    case TileCollisionType.Warp:
                        if (currentMap.TryGetWarp(tx, ty, out var foundWarp))
                        {
                            warp = foundWarp;
                            return true;
                        }
                        return false;
                }
            }

            byte baseId = currentMap.GetGround(tx, ty);
            var baseInfo = GetTileInfo(baseId);
            switch (baseInfo.Collision)
            {
                case TileCollisionType.Blocked:
                    return false;
                case TileCollisionType.Water:
                    return hasSurfAbility;
                case TileCollisionType.Warp:
                    if (currentMap.TryGetWarp(tx, ty, out var foundWarp))
                    {
                        warp = foundWarp;
                        return true;
                    }
                    return false;
                default:
                    return true;
            }
        }

        void ExecuteWarp(WarpPoint warp)
        {
            if (!warp.IsValid) return;
            if (!Maps.TryGetValue(warp.MapId, out var nextMap)) return;

            currentMapId = warp.MapId;
            currentMap = nextMap;

            int destX = Math.Clamp(warp.TargetX, 0, currentMap.Width - 1);
            int destY = Math.Clamp(warp.TargetY, 0, currentMap.Height - 1);

            pTileX = destX;
            pTileY = destY;
            px = destX * TILE_W;
            py = destY * TILE_H;
            targetPx = px;
            targetPy = py;
            isMoving = false;
        }

        public void Render(Span<byte> frame)
        {
            if (currentMap is null) return;

            int mapPixelWidth = currentMap.Width * TILE_W;
            int mapPixelHeight = currentMap.Height * TILE_H;

            // Kamera zentriert den Spieler (mit Clamping)
            int camX = px - MicroBoySpec.W / 2;
            int camY = py - MicroBoySpec.H / 2;
            camX = Math.Max(0, Math.Min(camX, mapPixelWidth - MicroBoySpec.W));
            camY = Math.Max(0, Math.Min(camY, mapPixelHeight - MicroBoySpec.H));

            // Hintergrund löschen (Farbe 0)
            frame.Fill(0);

            // Sichtbare Tiles berechnen
            int firstTileX = camX / TILE_W;
            int firstTileY = camY / TILE_H;
            int offX = -(camX % TILE_W);
            int offY = -(camY % TILE_H);

            // Tiles zeichnen
            for (int ty = 0, my = firstTileY; my < currentMap.Height && ty < MicroBoySpec.H; my++, ty += TILE_H)
            {
                int drawY = offY + ty;
                if (drawY >= MicroBoySpec.H) break;

                for (int tx = 0, mx = firstTileX; mx < currentMap.Width && tx < MicroBoySpec.W; mx++, tx += TILE_W)
                {
                    int drawX = offX + tx;
                    if (drawX >= MicroBoySpec.W) break;

                    byte baseId = currentMap.GetGround(mx, my);
                    byte overlayId = currentMap.GetOverlay(mx, my);
                    BlitTile(frame, drawX, drawY, baseId, overlayId);
                }
            }

            // Spieler (8x8) zeichnen – einfache 2-Frame „Animation“
            DrawPlayer(frame, px - camX, py - camY);
        }

        // --- Tileset: mehrere Tiles in 4 Farben (0..3) ---

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

        static readonly byte[,] TILE_TALL_GRASS =
        {
            {0,3,0,3,0,3,0,3},
            {3,0,3,0,3,0,3,0},
            {0,2,3,2,3,2,0,3},
            {3,0,3,0,3,0,3,0},
            {0,3,0,3,0,3,0,3},
            {3,0,3,0,3,0,3,0},
            {0,2,3,2,3,2,0,3},
            {3,0,3,0,3,0,3,0},
        };

        static readonly byte[,] TILE_WATER =
        {
            {1,1,1,1,1,1,1,1},
            {1,2,2,1,2,2,1,1},
            {1,3,3,2,3,3,2,1},
            {1,2,2,1,2,2,1,1},
            {1,3,3,2,3,3,2,1},
            {1,2,2,1,2,2,1,1},
            {1,3,3,2,3,3,2,1},
            {1,1,1,1,1,1,1,1},
        };

        static readonly byte[,] TILE_DOOR =
        {
            {0,0,3,3,3,3,0,0},
            {0,3,2,2,2,2,3,0},
            {0,3,2,2,2,2,3,0},
            {0,3,3,3,3,3,3,0},
            {0,3,2,2,2,2,3,0},
            {0,3,2,2,2,2,3,0},
            {0,3,2,2,2,2,3,0},
            {0,0,3,3,3,3,0,0},
        };

        static readonly byte[,] TILE_FLOOR =
        {
            {1,0,1,0,1,0,1,0},
            {0,1,0,1,0,1,0,1},
            {1,0,1,0,1,0,1,0},
            {0,1,0,1,0,1,0,1},
            {1,0,1,0,1,0,1,0},
            {0,1,0,1,0,1,0,1},
            {1,0,1,0,1,0,1,0},
            {0,1,0,1,0,1,0,1},
        };

        static readonly byte[,] TILE_WALL =
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

        static readonly byte[,] TILE_RUG =
        {
            {0,3,3,3,3,3,3,0},
            {3,2,2,2,2,2,2,3},
            {3,2,3,3,3,3,2,3},
            {3,2,3,2,2,3,2,3},
            {3,2,3,2,2,3,2,3},
            {3,2,3,3,3,3,2,3},
            {3,2,2,2,2,2,2,3},
            {0,3,3,3,3,3,3,0},
        };

        void BlitTile(Span<byte> fb, int dx, int dy, byte baseId, byte overlayId)
        {
            DrawTileLayer(fb, dx, dy, baseId, transparent: false);
            if (overlayId != TILE_NONE)
                DrawTileLayer(fb, dx, dy, overlayId, transparent: true);
        }

        void DrawTileLayer(Span<byte> fb, int dx, int dy, byte tileId, bool transparent)
        {
            var tile = GetTileSprite(tileId);
            for (int y = 0; y < TILE_H; y++)
            {
                int ry = dy + y; if ((uint)ry >= MicroBoySpec.H) continue;
                int row = ry * MicroBoySpec.W;
                for (int x = 0; x < TILE_W; x++)
                {
                    int rx = dx + x; if ((uint)rx >= MicroBoySpec.W) continue;
                    byte c = tile[y, x];
                    if (transparent && c == 0) continue;
                    fb[row + rx] = c;
                }
            }
        }

        static byte[,] GetTileSprite(byte id)
            => TileLookup.TryGetValue(id, out var tile) ? tile : TILE_GRASS;

        static TileInfo GetTileInfo(byte id)
            => TileRules.TryGetValue(id, out var info) ? info : DefaultTileInfo;

        static readonly Dictionary<byte, byte[,]> TileLookup = new()
        {
            [TILE_GRASS_ID] = TILE_GRASS,
            [TILE_PATH_ID] = TILE_PATH,
            [TILE_TREE_ID] = TILE_TREE,
            [TILE_TALL_GRASS_ID] = TILE_TALL_GRASS,
            [TILE_WATER_ID] = TILE_WATER,
            [TILE_DOOR_ID] = TILE_DOOR,
            [TILE_FLOOR_ID] = TILE_FLOOR,
            [TILE_WALL_ID] = TILE_WALL,
            [TILE_RUG_ID] = TILE_RUG,
        };

        static readonly TileInfo DefaultTileInfo = new(TileCollisionType.Walkable);

        static readonly Dictionary<byte, TileInfo> TileRules = new()
        {
            [TILE_GRASS_ID] = new TileInfo(TileCollisionType.Walkable),
            [TILE_PATH_ID] = new TileInfo(TileCollisionType.Walkable),
            [TILE_TREE_ID] = new TileInfo(TileCollisionType.Blocked),
            [TILE_TALL_GRASS_ID] = new TileInfo(TileCollisionType.Walkable),
            [TILE_WATER_ID] = new TileInfo(TileCollisionType.Water),
            [TILE_DOOR_ID] = new TileInfo(TileCollisionType.Warp),
            [TILE_FLOOR_ID] = new TileInfo(TileCollisionType.Walkable),
            [TILE_WALL_ID] = new TileInfo(TileCollisionType.Blocked),
            [TILE_RUG_ID] = new TileInfo(TileCollisionType.Walkable),
        };

        static Dictionary<string, MapDefinition> BuildMaps()
        {
            var baseLegend = new Dictionary<char, byte>
            {
                ['G'] = TILE_GRASS_ID,
                ['P'] = TILE_PATH_ID,
                ['H'] = TILE_TALL_GRASS_ID,
                ['W'] = TILE_WATER_ID,
                ['F'] = TILE_FLOOR_ID,
            };

            var overlayLegend = new Dictionary<char, byte>
            {
                ['T'] = TILE_TREE_ID,
                ['t'] = TILE_TALL_GRASS_ID,
                ['D'] = TILE_DOOR_ID,
                ['#'] = TILE_WALL_ID,
                ['r'] = TILE_RUG_ID,
            };

            const string OverworldGroundData = """
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGPPPPPPPPPPPPPPPPPPPPPPPPPPPPGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGHHHHHHHHHGGGGGGGGGGGGGGGGGGG
GGGGHHHHHHHHHGGGGGGGGGGGGGGGGGGG
GGGGHHHHHHHHHGGGGGGGGGGGGGGGGGGG
GGGGHHHHHHHHHGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGPPPPPPPPPPPPPPPPGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGWWWWWWWWGGGG
GGGGGGGGGGGGGGGGGGGGWWWWWWWWGGGG
GGGGGGGGGGGGGGGGGGGGWWWWWWWWGGGG
GGGGGGGGGGGGGGGGGGGGWWWWWWWWGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
""";

            const string OverworldOverlayData = """
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
T..............................T
T..............................T
T..............................T
T..............................T
T...............T..............T
T...............T..............T
T...............T..............T
T...............T..............T
T...............T..............T
T...............T.......D......T
T...............T..............T
T...............T..............T
T...............T..............T
T...ttttttttt..................T
T...ttttttttt..................T
T...ttttttttt..................T
T...ttttttttt..................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
T..............................T
TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
""";

            const string HouseGroundData = """
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
FFFFFFFFFFFFFFFF
""";

            const string HouseOverlayData = """
################
#..............#
#..............#
#..............#
#...rrrrrrrr...#
#...rrrrrrrr...#
#...rrrrrrrr...#
#...rrrrrrrr...#
#..............#
#..............#
#..............#
#..............#
#..............#
#..............#
#..............#
########D#######
""";

            return new Dictionary<string, MapDefinition>
            {
                ["overworld"] = new MapDefinition(
                    ParseLayer(OverworldGroundData, baseLegend),
                    ParseLayer(OverworldOverlayData, overlayLegend, TILE_NONE),
                    new Dictionary<(int x, int y), WarpPoint>
                    {
                        [(24, 10)] = new WarpPoint("house", 8, 14),
                    }),
                ["house"] = new MapDefinition(
                    ParseLayer(HouseGroundData, baseLegend),
                    ParseLayer(HouseOverlayData, overlayLegend, TILE_NONE),
                    new Dictionary<(int x, int y), WarpPoint>
                    {
                        [(8, 15)] = new WarpPoint("overworld", 24, 11),
                    }),
            };
        }

        static byte[,] ParseLayer(string data, Dictionary<char, byte> legend, byte? defaultValue = null)
        {
            var rows = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length == 0)
                throw new InvalidOperationException("Keine Layerdaten vorhanden.");

            int width = rows[0].TrimEnd('\r').Length;
            var result = new byte[rows.Length, width];

            for (int y = 0; y < rows.Length; y++)
            {
                var row = rows[y].TrimEnd('\r');
                if (row.Length != width)
                    throw new InvalidOperationException("Uneinheitliche Zeilenlänge in den Map-Daten.");

                for (int x = 0; x < width; x++)
                {
                    char ch = row[x];
                    if (legend.TryGetValue(ch, out var id))
                    {
                        result[y, x] = id;
                    }
                    else if (defaultValue.HasValue)
                    {
                        result[y, x] = defaultValue.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unbekanntes Tile-Zeichen '{ch}'.");
                    }
                }
            }

            return result;
        }

        sealed class MapDefinition
        {
            readonly byte[,] ground;
            readonly byte[,] overlay;
            readonly Dictionary<(int x, int y), WarpPoint> warps;

            public MapDefinition(byte[,] ground, byte[,] overlay, Dictionary<(int x, int y), WarpPoint> warps)
            {
                if (ground.GetLength(0) != overlay.GetLength(0) || ground.GetLength(1) != overlay.GetLength(1))
                    throw new ArgumentException("Layer-Größen stimmen nicht überein.", nameof(overlay));

                this.ground = ground;
                this.overlay = overlay;
                this.warps = warps;
            }

            public int Width => ground.GetLength(1);
            public int Height => ground.GetLength(0);

            public byte GetGround(int x, int y) => ground[y, x];
            public byte GetOverlay(int x, int y) => overlay[y, x];

            public bool TryGetWarp(int x, int y, out WarpPoint warp)
            {
                if (warps.TryGetValue((x, y), out warp))
                    return true;

                warp = WarpPoint.None;
                return false;
            }
        }

        readonly record struct WarpPoint(string MapId, int TargetX, int TargetY)
        {
            public bool IsValid => !string.IsNullOrEmpty(MapId);
            public static WarpPoint None => new(string.Empty, 0, 0);
        }

        enum TileCollisionType
        {
            Walkable,
            Blocked,
            Water,
            Warp,
        }

        readonly record struct TileInfo(TileCollisionType Collision);

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
