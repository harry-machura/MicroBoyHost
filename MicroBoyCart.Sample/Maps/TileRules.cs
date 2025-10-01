using System.Collections.Generic;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.Maps;

public enum TileCollisionType
{
    Walkable,
    Blocked,
    Water,
    Warp,
}

public readonly record struct TileInfo(TileCollisionType Collision)
{
    public static readonly TileInfo Walkable = new(TileCollisionType.Walkable);
}

public sealed class TileRules
{
    private readonly Dictionary<byte, TileInfo> rules;

    public TileRules()
    {
        rules = new Dictionary<byte, TileInfo>
        {
            [Tileset.TileGrassId] = TileInfo.Walkable,
            [Tileset.TilePathId] = TileInfo.Walkable,
            [Tileset.TileTreeId] = new TileInfo(TileCollisionType.Blocked),
            [Tileset.TileTallGrassId] = TileInfo.Walkable,
            [Tileset.TileWaterId] = new TileInfo(TileCollisionType.Water),
            [Tileset.TileDoorId] = new TileInfo(TileCollisionType.Warp),
            [Tileset.TileFloorId] = TileInfo.Walkable,
            [Tileset.TileWallId] = new TileInfo(TileCollisionType.Blocked),
            [Tileset.TileRugId] = TileInfo.Walkable,
        };
    }

    public TileInfo Get(byte id)
    {
        if (rules.TryGetValue(id, out var info))
        {
            return info;
        }

        return TileInfo.Walkable;
    }
}
