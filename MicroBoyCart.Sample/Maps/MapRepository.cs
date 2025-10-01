using System;
using System.Collections.Generic;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.Maps;

public sealed class MapRepository
{
    private readonly Dictionary<string, MapDefinition> maps;

    public MapRepository()
    {
        maps = BuildMaps();
    }

    public string DefaultMapId => "overworld";

    public MapDefinition GetDefaultMap() => maps[DefaultMapId];

    public bool TryGetMap(string mapId, out MapDefinition map) => maps.TryGetValue(mapId, out map);

    private Dictionary<string, MapDefinition> BuildMaps()
    {
        var baseLegend = new Dictionary<char, byte>
        {
            ['G'] = Tileset.TileGrassId,
            ['P'] = Tileset.TilePathId,
            ['H'] = Tileset.TileTallGrassId,
            ['W'] = Tileset.TileWaterId,
            ['F'] = Tileset.TileFloorId,
        };

        var overlayLegend = new Dictionary<char, byte>
        {
            ['T'] = Tileset.TileTreeId,
            ['t'] = Tileset.TileTallGrassId,
            ['D'] = Tileset.TileDoorId,
            ['#'] = Tileset.TileWallId,
            ['r'] = Tileset.TileRugId,
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
                ParseLayer(OverworldOverlayData, overlayLegend, Tileset.TileNone),
                new Dictionary<(int x, int y), WarpPoint>
                {
                    [(24, 10)] = new WarpPoint("house", 8, 14),
                }),
            ["house"] = new MapDefinition(
                ParseLayer(HouseGroundData, baseLegend),
                ParseLayer(HouseOverlayData, overlayLegend, Tileset.TileNone),
                new Dictionary<(int x, int y), WarpPoint>
                {
                    [(8, 15)] = new WarpPoint("overworld", 24, 11),
                }),
        };
    }

    private static byte[,] ParseLayer(string data, Dictionary<char, byte> legend, byte? defaultValue = null)
    {
        var rows = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length == 0)
        {
            throw new InvalidOperationException("Keine Layerdaten vorhanden.");
        }

        int width = rows[0].TrimEnd('\r').Length;
        var result = new byte[rows.Length, width];

        for (int y = 0; y < rows.Length; y++)
        {
            var row = rows[y].TrimEnd('\r');
            if (row.Length != width)
            {
                throw new InvalidOperationException("Uneinheitliche Zeilenlänge in den Map-Daten.");
            }

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
}

public sealed class MapDefinition
{
    private readonly byte[,] ground;
    private readonly byte[,] overlay;
    private readonly Dictionary<(int x, int y), WarpPoint> warps;

    public MapDefinition(byte[,] ground, byte[,] overlay, Dictionary<(int x, int y), WarpPoint> warps)
    {
        if (ground.GetLength(0) != overlay.GetLength(0) || ground.GetLength(1) != overlay.GetLength(1))
        {
            throw new ArgumentException("Layer-Größen stimmen nicht überein.", nameof(overlay));
        }

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
        {
            return true;
        }

        warp = WarpPoint.None;
        return false;
    }
}

public readonly record struct WarpPoint(string MapId, int TargetX, int TargetY)
{
    public bool IsValid => !string.IsNullOrEmpty(MapId);
    public static WarpPoint None => new(string.Empty, 0, 0);
}
