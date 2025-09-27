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

        public int AudioSampleRate => 44100;
        public int AudioChannelCount => 2;

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
        const byte TILE_CHEST_ID = 9;
        const byte TILE_ITEM_ID = 10;
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

        // Einfache Audio-State-Maschine
        double audioPhase;
        double melodyTimer;
        int melodyIndex;
        static readonly double[] Melody = { 220.0, 246.0, 262.0, 294.0, 330.0, 294.0, 262.0, 246.0 };

        MapDefinition currentMap = null!;
        string currentMapId = string.Empty;
        WarpPoint? pendingWarp;
        bool hasSurfAbility;
        bool wasAButtonDown;
        readonly List<ItemSlot> inventory = new();

        public void Init()
        {
            foreach (var map in Maps.Values)
                map.Reset();

            currentMapId = "overworld";
            currentMap = Maps[currentMapId];
            pendingWarp = null;
            hasSurfAbility = false;
            wasAButtonDown = false;
            inventory.Clear();

            pTileX = 5;
            pTileY = 10;
            // Initial auf Tile zentrieren
            px = pTileX * TILE_W;
            py = pTileY * TILE_H;
            targetPx = px; targetPy = py;
            isMoving = false;
            dir = 0;

            audioPhase = 0;
            melodyTimer = 0;
            melodyIndex = 0;
        }

        public void Update(Input input, double dt)
        {
            bool aDown = input.IsDown(Buttons.A);
            if (!isMoving && aDown && !wasAButtonDown)
            {
                HandleInteraction();
            }
            wasAButtonDown = aDown;

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

        void HandleInteraction()
        {
            if (currentMap is null) return;

            var (fx, fy) = GetFacingTile();
            if (TryPickupItemAt(fx, fy))
                return;

            TryPickupItemAt(pTileX, pTileY);
        }

        bool TryPickupItemAt(int tx, int ty)
        {
            if (currentMap is null) return false;
            if (!currentMap.IsInside(tx, ty)) return false;

            if (currentMap.TryTakeItem(tx, ty, out var slot))
            {
                inventory.Add(slot);
                OnItemPicked(slot);
                return true;
            }

            return false;
        }

        (int x, int y) GetFacingTile()
            => dir switch
            {
                0 => (pTileX, pTileY + 1),
                1 => (pTileX - 1, pTileY),
                2 => (pTileX + 1, pTileY),
                3 => (pTileX, pTileY - 1),
                _ => (pTileX, pTileY),
            };

        void OnItemPicked(ItemSlot slot)
        {
            switch (slot.Type)
            {
                case ItemType.SurfKit:
                    hasSurfAbility = true;
                    break;
            }
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
            DrawInventoryHud(frame);
        }

        // --- Tileset: mehrere Tiles mit erweiterten Palettenindizes ---

        // Palette-Indizes für bessere Lesbarkeit
        const byte COLOR_GRASS_DARK = 0;
        const byte COLOR_GRASS_MID = 1;
        const byte COLOR_GRASS_LIGHT = 2;
        const byte COLOR_GRASS_HIGHLIGHT = 3;
        const byte COLOR_PATH_LIGHT = 4;
        const byte COLOR_PATH_DARK = 5;
        const byte COLOR_WATER_DEEP = 6;
        const byte COLOR_WATER_LIGHT = 7;
        const byte COLOR_STONE = 8;
        const byte COLOR_RUG = 9;

        static readonly byte[,] TILE_GRASS = // feines Muster mit helleren Highlights
        {
            {COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID},
            {COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT},
            {COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT},
            {COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT},
            {COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID},
            {COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT},
            {COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_DARK, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT},
            {COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT},
        };

        static readonly byte[,] TILE_PATH = // Weg mit hellen Platten und dunklen Rändern
        {
            {COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK},
        };

        static readonly byte[,] TILE_TREE = // Baum/Wand mit Braun für Stamm
        {
            {COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_MID, COLOR_GRASS_MID, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_MID, COLOR_GRASS_MID, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_DARK},
            {COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK},
        };

        static readonly byte[,] TILE_TALL_GRASS =
        {
            {COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT},
            {COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT},
            {COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT},
            {COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT},
            {COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT},
            {COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT},
            {COLOR_GRASS_LIGHT, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_MID, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT},
            {COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT, COLOR_GRASS_HIGHLIGHT, COLOR_GRASS_LIGHT},
        };

        static readonly byte[,] TILE_WATER =
        {
            {COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_LIGHT, COLOR_WATER_DEEP},
            {COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP, COLOR_WATER_DEEP},
        };

        static readonly byte[,] TILE_DOOR =
        {
            {COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_DARK, COLOR_GRASS_DARK},
            {COLOR_GRASS_DARK, COLOR_GRASS_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_GRASS_DARK, COLOR_GRASS_DARK},
        };

        static readonly byte[,] TILE_FLOOR =
        {
            {COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT},
            {COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT},
            {COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT},
            {COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT},
            {COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE, COLOR_PATH_LIGHT, COLOR_STONE},
        };

        static readonly byte[,] TILE_WALL =
        {
            {COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_PATH_DARK, COLOR_STONE},
            {COLOR_STONE, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_STONE},
            {COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE},
        };

        static readonly byte[,] TILE_RUG =
        {
            {COLOR_PATH_DARK, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_PATH_DARK},
            {COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG},
            {COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG},
            {COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG},
            {COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG},
            {COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_RUG},
            {COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG},
            {COLOR_PATH_DARK, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_PATH_DARK},
        };

        static readonly byte[,] TILE_CHEST =
        {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, 0},
            {0, COLOR_PATH_DARK, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_RUG, COLOR_PATH_DARK, 0},
            {0, COLOR_PATH_DARK, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_PATH_DARK, 0},
            {0, COLOR_PATH_DARK, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG, COLOR_PATH_DARK, 0},
            {0, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, COLOR_PATH_DARK, 0},
            {0, COLOR_PATH_DARK, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_STONE, COLOR_PATH_DARK, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
        };

        static readonly byte[,] TILE_ITEM =
        {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, COLOR_RUG, COLOR_RUG, 0, 0, 0},
            {0, 0, COLOR_RUG, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, COLOR_RUG, 0, 0},
            {0, 0, 0, COLOR_PATH_LIGHT, COLOR_PATH_LIGHT, 0, 0, 0},
            {0, 0, 0, 0, COLOR_PATH_LIGHT, 0, 0, 0},
            {0, 0, 0, COLOR_PATH_LIGHT, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
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

        void DrawInventoryHud(Span<byte> fb)
        {
            const int hudX = 2;
            const int hudY = 2;
            const int hudWidth = 60;
            const int slotHeight = 9;

            int slotCount = Math.Max(inventory.Count, 1);
            int hudHeight = 12 + slotCount * slotHeight;

            if (hudX >= MicroBoySpec.W || hudY >= MicroBoySpec.H)
                return;

            if (hudY + hudHeight > MicroBoySpec.H)
                hudHeight = MicroBoySpec.H - hudY;

            FillRect(fb, hudX, hudY, hudWidth, hudHeight, COLOR_PATH_DARK);
            FillRect(fb, hudX + 1, hudY + 1, hudWidth - 2, hudHeight - 2, COLOR_PATH_LIGHT);

            DrawHudText(fb, hudX + 4, hudY + 2, "INV", COLOR_PATH_DARK);

            int slotTop = hudY + 8;
            if (inventory.Count == 0)
            {
                DrawHudText(fb, hudX + 4, slotTop + 1, "EMPTY", COLOR_PATH_DARK);
                return;
            }

            foreach (var slot in inventory)
            {
                FillRect(fb, hudX + 4, slotTop, hudWidth - 8, slotHeight - 2, COLOR_GRASS_LIGHT);
                FillRect(fb, hudX + 5, slotTop + 1, 8, slotHeight - 4, COLOR_GRASS_MID);
                DrawHudChar(fb, hudX + 6, slotTop + 2, slot.Symbol, COLOR_PATH_DARK);
                DrawHudText(fb, hudX + 15, slotTop + 2, slot.Name.ToUpperInvariant(), COLOR_PATH_DARK);
                slotTop += slotHeight;
            }
        }

        void FillRect(Span<byte> fb, int x, int y, int width, int height, byte color)
        {
            if (width <= 0 || height <= 0) return;

            for (int iy = 0; iy < height; iy++)
            {
                int ry = y + iy;
                if ((uint)ry >= MicroBoySpec.H) continue;
                int rowOffset = ry * MicroBoySpec.W;

                for (int ix = 0; ix < width; ix++)
                {
                    int rx = x + ix;
                    if ((uint)rx >= MicroBoySpec.W) continue;
                    fb[rowOffset + rx] = color;
                }
            }
        }

        void DrawHudText(Span<byte> fb, int x, int y, string text, byte color)
        {
            if (string.IsNullOrEmpty(text)) return;

            int cursor = x;
            foreach (char ch in text)
            {
                cursor += DrawHudChar(fb, cursor, y, ch, color);
            }
        }

        int DrawHudChar(Span<byte> fb, int x, int y, char ch, byte color)
        {
            if (ch == ' ')
                return 3;

            char upper = char.ToUpperInvariant(ch);
            if (!HudFont.TryGetValue(upper, out var rows))
                rows = HudFont['?'];

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                int pattern = rows[rowIndex];
                int ry = y + rowIndex;
                if ((uint)ry >= MicroBoySpec.H) continue;
                int rowOffset = ry * MicroBoySpec.W;

                for (int col = 0; col < 4; col++)
                {
                    if ((pattern & (1 << (3 - col))) == 0) continue;

                    int rx = x + col;
                    if ((uint)rx >= MicroBoySpec.W) continue;
                    fb[rowOffset + rx] = color;
                }
            }

            return 5;
        }

        static byte[,] GetTileSprite(byte id)
            => TileLookup.TryGetValue(id, out var tile) ? tile : TILE_GRASS;

        static TileInfo GetTileInfo(byte id)
            => TileRules.TryGetValue(id, out var info) ? info : DefaultTileInfo;

        public void MixAudio(Span<float> buffer)
        {
            int channels = AudioChannelCount;
            if (channels <= 0)
            {
                return;
            }

            int samples = buffer.Length / channels;
            if (samples <= 0)
            {
                buffer.Clear();
                return;
            }

            double stepDuration = isMoving ? 0.18 : 0.32;
            double vibratoSpeed = isMoving ? 8.0 : 5.0;
            double vibratoDepth = isMoving ? 6.0 : 3.0;
            int sampleRate = AudioSampleRate;
            const double twoPi = Math.PI * 2.0;

            for (int i = 0; i < samples; i++)
            {
                melodyTimer += 1.0 / sampleRate;
                if (melodyTimer >= stepDuration)
                {
                    melodyTimer -= stepDuration;
                    melodyIndex = (melodyIndex + 1) % Melody.Length;
                }

                double freq = Melody[melodyIndex];
                double vibrato = Math.Sin((melodyTimer + i / (double)sampleRate) * twoPi * vibratoSpeed) * vibratoDepth;
                double phaseStep = twoPi * (freq + vibrato) / sampleRate;

                audioPhase += phaseStep;
                if (audioPhase >= twoPi)
                {
                    audioPhase -= twoPi;
                }

                float sample = (float)(Math.Sin(audioPhase) * 0.18);

                int baseIndex = i * channels;
                for (int ch = 0; ch < channels; ch++)
                {
                    buffer[baseIndex + ch] = sample;
                }
            }
        }

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
            [TILE_CHEST_ID] = TILE_CHEST,
            [TILE_ITEM_ID] = TILE_ITEM,
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
            [TILE_CHEST_ID] = new TileInfo(TileCollisionType.Blocked),
            [TILE_ITEM_ID] = new TileInfo(TileCollisionType.Walkable),
        };

        static readonly Dictionary<char, byte[]> HudFont = new()
        {
            ['0'] = new byte[] { 0b0110, 0b1001, 0b1001, 0b1001, 0b0110 },
            ['1'] = new byte[] { 0b0100, 0b1100, 0b0100, 0b0100, 0b1110 },
            ['2'] = new byte[] { 0b1110, 0b0001, 0b0110, 0b1000, 0b1111 },
            ['3'] = new byte[] { 0b1110, 0b0001, 0b0110, 0b0001, 0b1110 },
            ['4'] = new byte[] { 0b1001, 0b1001, 0b1111, 0b0001, 0b0001 },
            ['5'] = new byte[] { 0b1111, 0b1000, 0b1110, 0b0001, 0b1110 },
            ['6'] = new byte[] { 0b0111, 0b1000, 0b1110, 0b1001, 0b0110 },
            ['7'] = new byte[] { 0b1111, 0b0001, 0b0010, 0b0100, 0b0100 },
            ['8'] = new byte[] { 0b0110, 0b1001, 0b0110, 0b1001, 0b0110 },
            ['9'] = new byte[] { 0b0110, 0b1001, 0b0111, 0b0001, 0b1110 },
            ['A'] = new byte[] { 0b0110, 0b1001, 0b1111, 0b1001, 0b1001 },
            ['B'] = new byte[] { 0b1110, 0b1001, 0b1110, 0b1001, 0b1110 },
            ['C'] = new byte[] { 0b0111, 0b1000, 0b1000, 0b1000, 0b0111 },
            ['D'] = new byte[] { 0b1110, 0b1001, 0b1001, 0b1001, 0b1110 },
            ['E'] = new byte[] { 0b1111, 0b1000, 0b1110, 0b1000, 0b1111 },
            ['F'] = new byte[] { 0b1111, 0b1000, 0b1110, 0b1000, 0b1000 },
            ['G'] = new byte[] { 0b0111, 0b1000, 0b1011, 0b1001, 0b0111 },
            ['H'] = new byte[] { 0b1001, 0b1001, 0b1111, 0b1001, 0b1001 },
            ['I'] = new byte[] { 0b1111, 0b0100, 0b0100, 0b0100, 0b1111 },
            ['J'] = new byte[] { 0b1111, 0b0001, 0b0001, 0b1001, 0b0110 },
            ['K'] = new byte[] { 0b1001, 0b1010, 0b1100, 0b1010, 0b1001 },
            ['L'] = new byte[] { 0b1000, 0b1000, 0b1000, 0b1000, 0b1111 },
            ['M'] = new byte[] { 0b1001, 0b1111, 0b1011, 0b1001, 0b1001 },
            ['N'] = new byte[] { 0b1001, 0b1101, 0b1011, 0b1001, 0b1001 },
            ['O'] = new byte[] { 0b0110, 0b1001, 0b1001, 0b1001, 0b0110 },
            ['P'] = new byte[] { 0b1110, 0b1001, 0b1110, 0b1000, 0b1000 },
            ['Q'] = new byte[] { 0b0110, 0b1001, 0b1001, 0b1010, 0b0101 },
            ['R'] = new byte[] { 0b1110, 0b1001, 0b1110, 0b1010, 0b1001 },
            ['S'] = new byte[] { 0b0111, 0b1000, 0b0110, 0b0001, 0b1110 },
            ['T'] = new byte[] { 0b1111, 0b0100, 0b0100, 0b0100, 0b0100 },
            ['U'] = new byte[] { 0b1001, 0b1001, 0b1001, 0b1001, 0b0110 },
            ['V'] = new byte[] { 0b1001, 0b1001, 0b1001, 0b0110, 0b0110 },
            ['W'] = new byte[] { 0b1001, 0b1001, 0b1011, 0b1111, 0b1001 },
            ['X'] = new byte[] { 0b1001, 0b0110, 0b0100, 0b0110, 0b1001 },
            ['Y'] = new byte[] { 0b1001, 0b0110, 0b0100, 0b0100, 0b0100 },
            ['Z'] = new byte[] { 0b1111, 0b0010, 0b0100, 0b1000, 0b1111 },
            ['?'] = new byte[] { 0b1110, 0b0001, 0b0110, 0b0000, 0b0100 },
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
                ['C'] = TILE_CHEST_ID,
                ['I'] = TILE_ITEM_ID,
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
T............I.................T
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
#.....C........#
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

            var overworldItems = new Dictionary<(int x, int y), ItemSlot>
            {
                [(13, 18)] = new ItemSlot(ItemType.Berry, "Berry", 'B'),
            };

            var houseItems = new Dictionary<(int x, int y), ItemSlot>
            {
                [(6, 2)] = new ItemSlot(ItemType.SurfKit, "Surf Kit", 'S'),
            };

            return new Dictionary<string, MapDefinition>
            {
                ["overworld"] = new MapDefinition(
                    ParseLayer(OverworldGroundData, baseLegend),
                    ParseLayer(OverworldOverlayData, overlayLegend, TILE_NONE),
                    new Dictionary<(int x, int y), WarpPoint>
                    {
                        [(24, 10)] = new WarpPoint("house", 8, 14),
                    },
                    overworldItems),
                ["house"] = new MapDefinition(
                    ParseLayer(HouseGroundData, baseLegend),
                    ParseLayer(HouseOverlayData, overlayLegend, TILE_NONE),
                    new Dictionary<(int x, int y), WarpPoint>
                    {
                        [(8, 15)] = new WarpPoint("overworld", 24, 11),
                    },
                    houseItems),
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
            readonly byte[,] overlayInitial;
            readonly byte[,] overlay;
            readonly Dictionary<(int x, int y), WarpPoint> warps;
            readonly Dictionary<(int x, int y), ItemSlot> initialItems;
            readonly Dictionary<(int x, int y), ItemSlot> items;

            public MapDefinition(byte[,] groundLayer, byte[,] overlayLayer, Dictionary<(int x, int y), WarpPoint> warps, Dictionary<(int x, int y), ItemSlot> items)
            {
                if (groundLayer.GetLength(0) != overlayLayer.GetLength(0) || groundLayer.GetLength(1) != overlayLayer.GetLength(1))
                    throw new ArgumentException("Layer-Größen stimmen nicht überein.", nameof(overlayLayer));

                ground = (byte[,])groundLayer.Clone();
                overlayInitial = (byte[,])overlayLayer.Clone();
                overlay = (byte[,])overlayLayer.Clone();
                this.warps = new Dictionary<(int x, int y), WarpPoint>(warps);
                initialItems = new Dictionary<(int x, int y), ItemSlot>(items);
                this.items = new Dictionary<(int x, int y), ItemSlot>(items);
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

            public bool IsInside(int x, int y)
                => x >= 0 && y >= 0 && x < Width && y < Height;

            public bool TryTakeItem(int x, int y, out ItemSlot item)
            {
                if (items.TryGetValue((x, y), out item))
                {
                    items.Remove((x, y));
                    overlay[y, x] = TILE_NONE;
                    return true;
                }

                item = default;
                return false;
            }

            public void Reset()
            {
                Buffer.BlockCopy(overlayInitial, 0, overlay, 0, overlayInitial.Length);
                items.Clear();
                foreach (var entry in initialItems)
                    items[entry.Key] = entry.Value;
            }
        }

        readonly record struct WarpPoint(string MapId, int TargetX, int TargetY)
        {
            public bool IsValid => !string.IsNullOrEmpty(MapId);
            public static WarpPoint None => new(string.Empty, 0, 0);
        }

        enum ItemType
        {
            SurfKit,
            Berry,
        }

        readonly record struct ItemSlot(ItemType Type, string Name, char Symbol);

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
                {0,0,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_PATH_DARK,0,0},
                {0,COLOR_PATH_DARK,COLOR_PATH_LIGHT,COLOR_PATH_LIGHT,COLOR_PATH_LIGHT,COLOR_PATH_LIGHT,COLOR_PATH_DARK,0},
                {COLOR_PATH_DARK,COLOR_PATH_LIGHT,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_PATH_LIGHT,COLOR_PATH_DARK},
                {COLOR_PATH_DARK,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_PATH_DARK},
                {0,COLOR_PATH_DARK,COLOR_RUG,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_RUG,COLOR_PATH_DARK,0},
                {0,COLOR_PATH_DARK,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_PATH_DARK,0},
                {0,COLOR_PATH_DARK,COLOR_STONE,COLOR_STONE,COLOR_STONE,COLOR_STONE,COLOR_PATH_DARK,0},
                {0,0,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_PATH_DARK,0,0},
            };
            byte[,] f1 =
            {
                {0,0,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_PATH_DARK,0,0},
                {0,COLOR_PATH_DARK,COLOR_PATH_LIGHT,COLOR_PATH_LIGHT,COLOR_PATH_LIGHT,COLOR_PATH_LIGHT,COLOR_PATH_DARK,0},
                {COLOR_PATH_DARK,COLOR_PATH_LIGHT,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_PATH_LIGHT,COLOR_PATH_DARK},
                {COLOR_PATH_DARK,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_PATH_DARK},
                {0,COLOR_PATH_DARK,COLOR_RUG,COLOR_PATH_DARK,COLOR_PATH_DARK,COLOR_RUG,COLOR_PATH_DARK,0},
                {0,COLOR_PATH_DARK,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_RUG,COLOR_PATH_DARK,0},
                {0,0,COLOR_PATH_DARK,COLOR_STONE,COLOR_STONE,COLOR_PATH_DARK,0,0},
                {0,COLOR_PATH_DARK,0,0,0,0,COLOR_PATH_DARK,0},
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
