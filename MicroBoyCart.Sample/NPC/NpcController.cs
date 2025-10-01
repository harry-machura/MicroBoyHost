using System;
using System.Collections.Generic;
using System.Linq;
using MicroBoy;
using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.NPCs;

public sealed class NpcController
{
    private readonly List<NpcDefinition> npcs;
    private readonly TileRules tileRules;
    private readonly Random random = new();

    public NpcController(TileRules tileRules)
    {
        this.tileRules = tileRules;
        npcs = new List<NpcDefinition>();
    }

    public IReadOnlyList<NpcDefinition> GetNpcsForMap(string mapId)
    {
        return npcs.Where(n => n.MapId == mapId).ToList();
    }

    public void AddNpc(NpcDefinition npc)
    {
        npcs.Add(npc);
    }

    public void Update(double dt, MapDefinition currentMap, string currentMapId)
    {
        var mapNpcs = GetNpcsForMap(currentMapId);

        foreach (var npc in mapNpcs)
        {
            switch (npc.Behavior)
            {
                case NpcBehavior.Wander:
                    UpdateWanderNpc(npc, dt, currentMap);
                    break;
                case NpcBehavior.Patrol:
                    UpdatePatrolNpc(npc, dt, currentMap);
                    break;
            }
        }
    }

    public NpcDefinition? GetNpcAtPosition(int tileX, int tileY, string mapId)
    {
        return npcs.FirstOrDefault(n =>
            n.MapId == mapId &&
            n.TileX == tileX &&
            n.TileY == tileY);
    }

    public NpcDefinition? GetNpcInFront(int playerTileX, int playerTileY, int direction, string mapId)
    {
        int checkX = playerTileX;
        int checkY = playerTileY;

        switch (direction)
        {
            case 0: checkY++; break; // Down
            case 1: checkX--; break; // Left
            case 2: checkX++; break; // Right
            case 3: checkY--; break; // Up
        }

        return GetNpcAtPosition(checkX, checkY, mapId);
    }

    private void UpdateWanderNpc(NpcDefinition npc, double dt, MapDefinition map)
    {
        npc.WanderCooldown -= dt;

        if (npc.WanderCooldown <= 0)
        {
            npc.WanderCooldown = 2.0 + random.NextDouble() * 3.0; // 2-5 Sekunden

            // 50% Chance zu wandern
            if (random.Next(2) == 0)
            {
                int direction = random.Next(4);
                int newX = npc.TileX;
                int newY = npc.TileY;

                switch (direction)
                {
                    case 0: newY++; break;
                    case 1: newX--; break;
                    case 2: newX++; break;
                    case 3: newY--; break;
                }

                // Prüfe ob innerhalb des Wander-Radius (Startposition als Zentrum)
                // Für Einfachheit: speichern wir die Startposition nicht, daher unbegrenzt
                // Du könntest hier eine Distanz-Prüfung einbauen

                if (IsTileWalkableForNpc(map, newX, newY))
                {
                    npc.TileX = newX;
                    npc.TileY = newY;
                }
            }
        }
    }

    private void UpdatePatrolNpc(NpcDefinition npc, double dt, MapDefinition map)
    {
        // TODO: Implementiere Patrol-Logik wenn gewünscht
        // Benötigt zusätzliche State-Variablen (currentPatrolIndex, movementTimer, etc.)
    }

    private bool IsTileWalkableForNpc(MapDefinition map, int tileX, int tileY)
    {
        if (tileX < 0 || tileY < 0 || tileX >= map.Width || tileY >= map.Height)
            return false;

        // Prüfe ob ein anderer NPC bereits dort steht
        if (npcs.Any(n => n.TileX == tileX && n.TileY == tileY))
            return false;

        byte overlayId = map.GetOverlay(tileX, tileY);
        if (overlayId != Tileset.TileNone)
        {
            var overlayInfo = tileRules.Get(overlayId);
            if (overlayInfo.Collision != TileCollisionType.Walkable)
                return false;
        }

        byte baseId = map.GetGround(tileX, tileY);
        var baseInfo = tileRules.Get(baseId);
        return baseInfo.Collision == TileCollisionType.Walkable;
    }
}