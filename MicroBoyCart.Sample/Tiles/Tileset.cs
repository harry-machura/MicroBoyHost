using System.Collections.Generic;

namespace MicroBoyCart.Sample.Tiles;

public static class Tileset
{
    public const int TileWidth = 8;
    public const int TileHeight = 8;

    public const byte TileGrassId = 0;
    public const byte TilePathId = 1;
    public const byte TileTreeId = 2;
    public const byte TileTallGrassId = 3;
    public const byte TileWaterId = 4;
    public const byte TileDoorId = 5;
    public const byte TileFloorId = 6;
    public const byte TileWallId = 7;
    public const byte TileRugId = 8;
    public const byte TileNone = byte.MaxValue;

    public const byte ColorGrassDark = 0;
    public const byte ColorGrassMid = 1;
    public const byte ColorGrassLight = 2;
    public const byte ColorGrassHighlight = 3;
    public const byte ColorPathLight = 4;
    public const byte ColorPathDark = 5;
    public const byte ColorWaterDeep = 6;
    public const byte ColorWaterLight = 7;
    public const byte ColorStone = 8;
    public const byte ColorRug = 9;

    public static readonly byte[,] GrassTile =
    {
        {ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassMid},
        {ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight},
        {ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassHighlight, ColorGrassMid, ColorGrassDark, ColorGrassMid, ColorGrassHighlight},
        {ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight},
        {ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassMid},
        {ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight},
        {ColorGrassDark, ColorGrassMid, ColorGrassDark, ColorGrassHighlight, ColorGrassMid, ColorGrassDark, ColorGrassMid, ColorGrassHighlight},
        {ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight, ColorGrassMid, ColorGrassLight},
    };

    public static readonly byte[,] PathTile =
    {
        {ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark},
        {ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark},
    };

    public static readonly byte[,] TreeTile =
    {
        {ColorGrassDark, ColorGrassDark, ColorGrassDark, ColorGrassDark, ColorGrassDark, ColorGrassDark, ColorGrassDark, ColorGrassDark},
        {ColorGrassDark, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassDark},
        {ColorGrassDark, ColorGrassHighlight, ColorGrassMid, ColorGrassMid, ColorGrassMid, ColorGrassMid, ColorGrassHighlight, ColorGrassDark},
        {ColorGrassDark, ColorGrassHighlight, ColorGrassMid, ColorGrassLight, ColorGrassLight, ColorGrassMid, ColorGrassHighlight, ColorGrassDark},
        {ColorGrassDark, ColorGrassHighlight, ColorGrassMid, ColorGrassLight, ColorGrassLight, ColorGrassMid, ColorGrassHighlight, ColorGrassDark},
        {ColorGrassDark, ColorGrassHighlight, ColorGrassMid, ColorGrassMid, ColorGrassMid, ColorGrassMid, ColorGrassHighlight, ColorGrassDark},
        {ColorGrassDark, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassHighlight, ColorGrassDark},
        {ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark},
    };

    public static readonly byte[,] TallGrassTile =
    {
        {ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight},
        {ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight},
        {ColorGrassLight, ColorGrassMid, ColorGrassHighlight, ColorGrassMid, ColorGrassHighlight, ColorGrassMid, ColorGrassLight, ColorGrassHighlight},
        {ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight},
        {ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight},
        {ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight},
        {ColorGrassLight, ColorGrassMid, ColorGrassHighlight, ColorGrassMid, ColorGrassHighlight, ColorGrassMid, ColorGrassLight, ColorGrassHighlight},
        {ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight, ColorGrassHighlight, ColorGrassLight},
    };

    public static readonly byte[,] WaterTile =
    {
        {ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterLight, ColorWaterDeep, ColorWaterLight, ColorWaterDeep, ColorWaterLight, ColorWaterLight, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterLight, ColorWaterDeep, ColorWaterLight, ColorWaterDeep, ColorWaterLight, ColorWaterLight, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterLight, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterLight, ColorWaterDeep, ColorWaterLight, ColorWaterDeep, ColorWaterLight, ColorWaterLight, ColorWaterDeep},
        {ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep, ColorWaterDeep},
    };

    public static readonly byte[,] DoorTile =
    {
        {ColorGrassDark, ColorGrassDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorGrassDark, ColorGrassDark},
        {ColorGrassDark, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, ColorGrassDark},
        {ColorGrassDark, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, ColorGrassDark},
        {ColorGrassDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorGrassDark},
        {ColorGrassDark, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, ColorGrassDark},
        {ColorGrassDark, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, ColorGrassDark},
        {ColorGrassDark, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, ColorGrassDark},
        {ColorGrassDark, ColorGrassDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorGrassDark, ColorGrassDark},
    };

    public static readonly byte[,] FloorTile =
    {
        {ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight},
        {ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone},
        {ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight},
        {ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone},
        {ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight},
        {ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone},
        {ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight},
        {ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone, ColorPathLight, ColorStone},
    };

    public static readonly byte[,] WallTile =
    {
        {ColorStone, ColorStone, ColorStone, ColorStone, ColorStone, ColorStone, ColorStone, ColorStone},
        {ColorStone, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorStone},
        {ColorStone, ColorPathDark, ColorStone, ColorStone, ColorStone, ColorStone, ColorPathDark, ColorStone},
        {ColorStone, ColorPathDark, ColorStone, ColorStone, ColorStone, ColorStone, ColorPathDark, ColorStone},
        {ColorStone, ColorPathDark, ColorStone, ColorStone, ColorStone, ColorStone, ColorPathDark, ColorStone},
        {ColorStone, ColorPathDark, ColorStone, ColorStone, ColorStone, ColorStone, ColorPathDark, ColorStone},
        {ColorStone, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, ColorStone},
        {ColorStone, ColorStone, ColorStone, ColorStone, ColorStone, ColorStone, ColorStone, ColorStone},
    };

    public static readonly byte[,] RugTile =
    {
        {ColorPathDark, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathDark},
        {ColorRug, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorRug},
        {ColorRug, ColorPathLight, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathLight, ColorRug},
        {ColorRug, ColorPathLight, ColorRug, ColorPathLight, ColorPathLight, ColorRug, ColorPathLight, ColorRug},
        {ColorRug, ColorPathLight, ColorRug, ColorPathLight, ColorPathLight, ColorRug, ColorPathLight, ColorRug},
        {ColorRug, ColorPathLight, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathLight, ColorRug},
        {ColorRug, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorRug},
        {ColorPathDark, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathDark},
    };

    public static readonly byte[,] HeartFull =
    {
        {0, ColorRug, ColorRug, ColorRug, ColorRug, 0},
        {ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug},
        {ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug},
        {0, ColorRug, ColorRug, ColorRug, ColorRug, 0},
        {0, 0, ColorRug, ColorRug, 0, 0},
    };

    public static readonly byte[,] HeartEmpty =
    {
        {0, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, 0},
        {ColorPathLight, 0, 0, 0, 0, ColorPathLight},
        {ColorPathLight, 0, 0, 0, 0, ColorPathLight},
        {0, ColorPathLight, 0, 0, ColorPathLight, 0},
        {0, 0, ColorPathLight, ColorPathLight, 0, 0},
    };

    public static readonly byte[,] PlayerFrame0 =
    {
        {0, 0, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, 0, 0},
        {0, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, 0},
        {ColorPathDark, ColorPathLight, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathDark},
        {0, ColorPathDark, ColorRug, ColorPathDark, ColorPathDark, ColorRug, ColorPathDark, 0},
        {0, ColorPathDark, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathDark, 0},
        {0, ColorPathDark, ColorStone, ColorStone, ColorStone, ColorStone, ColorPathDark, 0},
        {0, 0, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, 0, 0},
    };

    public static readonly byte[,] PlayerFrame1 =
    {
        {0, 0, ColorPathDark, ColorPathDark, ColorPathDark, ColorPathDark, 0, 0},
        {0, ColorPathDark, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathLight, ColorPathDark, 0},
        {ColorPathDark, ColorPathLight, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathLight, ColorPathDark},
        {ColorPathDark, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathDark},
        {0, ColorPathDark, ColorRug, ColorPathDark, ColorPathDark, ColorRug, ColorPathDark, 0},
        {0, ColorPathDark, ColorRug, ColorRug, ColorRug, ColorRug, ColorPathDark, 0},
        {0, 0, ColorPathDark, ColorStone, ColorStone, ColorPathDark, 0, 0},
        {0, ColorPathDark, 0, 0, 0, 0, ColorPathDark, 0},
    };

    private static readonly Dictionary<byte, byte[,]> TileLookup = new()
    {
        [TileGrassId] = GrassTile,
        [TilePathId] = PathTile,
        [TileTreeId] = TreeTile,
        [TileTallGrassId] = TallGrassTile,
        [TileWaterId] = WaterTile,
        [TileDoorId] = DoorTile,
        [TileFloorId] = FloorTile,
        [TileWallId] = WallTile,
        [TileRugId] = RugTile,
    };

    public static byte[,] GetTileSprite(byte id) => TileLookup.TryGetValue(id, out var tile) ? tile : GrassTile;

    public static byte[,] GetPlayerFrame(int pixelX, int pixelY)
        => ((pixelX + pixelY) / TileWidth % 2 == 0) ? PlayerFrame0 : PlayerFrame1;
}
