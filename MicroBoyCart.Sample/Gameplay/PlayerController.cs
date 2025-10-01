using MicroBoy;
using System;
using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.Tiles;
using MicroBoyCart.Sample;

namespace MicroBoyCart.Sample.Gameplay;

public sealed class PlayerController
{
    private const double DamageCooldownDuration = 1.0;

    private readonly MapRepository mapRepository;
    private readonly TileRules tileRules;

    private MapDefinition currentMap;
    private string currentMapId = string.Empty;

    public PlayerController(MapRepository mapRepository, TileRules tileRules)
    {
        this.mapRepository = mapRepository;
        this.tileRules = tileRules;
        currentMap = mapRepository.GetDefaultMap();
        currentMapId = mapRepository.DefaultMapId;
    }

    public PlayerState State { get; } = new();

    public event Action? PlayerDefeated;

    public MapDefinition CurrentMap => currentMap;
    public string CurrentMapId => currentMapId;

    public void StartNewGame()
    {
        SetCurrentMap(mapRepository.DefaultMapId);

        State.MaxHealth = 6;
        State.CurrentHealth = State.MaxHealth;
        State.DamageCooldownTimer = 0;
        State.HasSurfAbility = false;
        State.TotalPlayTime = 0;
        State.Direction = 0;
        State.PendingWarp = null;
        State.StepSpeedPx = Math.Max(1, State.StepSpeedPx);

        State.SnapToTile(5, 10);
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        var mapId = string.IsNullOrWhiteSpace(data.CurrentMapId) ? mapRepository.DefaultMapId : data.CurrentMapId;
        if (!mapRepository.TryGetMap(mapId, out var map))
        {
            mapId = mapRepository.DefaultMapId;
            map = mapRepository.GetDefaultMap();
        }

        currentMap = map;
        currentMapId = mapId;

        int tileX = Math.Clamp(data.PlayerTileX, 0, currentMap.Width - 1);
        int tileY = Math.Clamp(data.PlayerTileY, 0, currentMap.Height - 1);
        State.SnapToTile(tileX, tileY);

        State.CurrentHealth = data.CurrentHealth;
        State.MaxHealth = data.MaxHealth;
        State.HasSurfAbility = data.HasSurfAbility;
        State.TotalPlayTime = data.PlayTimeSeconds;
        State.Direction = 0;
        State.PendingWarp = null;
        State.DamageCooldownTimer = 0;
    }

    public GameSaveData CreateSaveData()
    {
        return new GameSaveData
        {
            PlayerTileX = State.TileX,
            PlayerTileY = State.TileY,
            CurrentMapId = currentMapId,
            CurrentHealth = State.CurrentHealth,
            MaxHealth = State.MaxHealth,
            HasSurfAbility = State.HasSurfAbility,
            PlayTimeSeconds = State.TotalPlayTime,
            SaveDate = DateTime.Now,
            SaveVersion = 1
        };
    }

    public void Update(Input input, double dt)
    {
        State.TotalPlayTime += dt;

        if (State.DamageCooldownTimer > 0)
        {
            State.DamageCooldownTimer = Math.Max(0, State.DamageCooldownTimer - dt);
        }

        if (input.IsDown(Buttons.A))
        {
            State.HasSurfAbility = true;
        }

        if (State.IsMoving)
        {
            AdvanceTowardsTarget();

            if (State.PixelX == State.TargetPixelX && State.PixelY == State.TargetPixelY)
            {
                State.IsMoving = false;
                State.TileX = State.PixelX / Tileset.TileWidth;
                State.TileY = State.PixelY / Tileset.TileHeight;

                if (State.PendingWarp is { } warpPoint)
                {
                    State.PendingWarp = null;
                    ExecuteWarp(warpPoint);
                }

                EvaluateHazardState();
                return;
            }

            EvaluateHazardState();
            return;
        }

        int nextTileX = State.TileX;
        int nextTileY = State.TileY;

        if (input.IsDown(Buttons.Left))
        {
            nextTileX = State.TileX - 1;
            State.Direction = 1;
        }
        else if (input.IsDown(Buttons.Right))
        {
            nextTileX = State.TileX + 1;
            State.Direction = 2;
        }
        else if (input.IsDown(Buttons.Up))
        {
            nextTileY = State.TileY - 1;
            State.Direction = 3;
        }
        else if (input.IsDown(Buttons.Down))
        {
            nextTileY = State.TileY + 1;
            State.Direction = 0;
        }
        else
        {
            EvaluateHazardState();
            return;
        }

        if (IsWalkable(nextTileX, nextTileY, out var warp))
        {
            State.TileX = nextTileX;
            State.TileY = nextTileY;
            State.TargetPixelX = State.TileX * Tileset.TileWidth;
            State.TargetPixelY = State.TileY * Tileset.TileHeight;
            State.IsMoving = true;
            State.PendingWarp = warp.IsValid ? warp : null;
        }
        else
        {
            State.PendingWarp = null;
        }

        EvaluateHazardState();
    }

    private void AdvanceTowardsTarget()
    {
        if (State.PixelX < State.TargetPixelX)
        {
            State.PixelX = Math.Min(State.TargetPixelX, State.PixelX + State.StepSpeedPx);
        }
        else if (State.PixelX > State.TargetPixelX)
        {
            State.PixelX = Math.Max(State.TargetPixelX, State.PixelX - State.StepSpeedPx);
        }

        if (State.PixelY < State.TargetPixelY)
        {
            State.PixelY = Math.Min(State.TargetPixelY, State.PixelY + State.StepSpeedPx);
        }
        else if (State.PixelY > State.TargetPixelY)
        {
            State.PixelY = Math.Max(State.TargetPixelY, State.PixelY - State.StepSpeedPx);
        }
    }

    private bool IsWalkable(int tileX, int tileY, out WarpPoint warp)
    {
        warp = WarpPoint.None;

        if (currentMap is null)
        {
            return false;
        }

        if (tileX < 0 || tileY < 0 || tileX >= currentMap.Width || tileY >= currentMap.Height)
        {
            return false;
        }

        byte overlayId = currentMap.GetOverlay(tileX, tileY);
        if (overlayId != Tileset.TileNone)
        {
            var overlayInfo = tileRules.Get(overlayId);
            switch (overlayInfo.Collision)
            {
                case TileCollisionType.Blocked:
                    return false;
                case TileCollisionType.Water:
                    return State.HasSurfAbility;
                case TileCollisionType.Warp:
                    if (currentMap.TryGetWarp(tileX, tileY, out var foundWarp))
                    {
                        warp = foundWarp;
                        return true;
                    }
                    return false;
            }
        }

        byte baseId = currentMap.GetGround(tileX, tileY);
        var baseInfo = tileRules.Get(baseId);
        switch (baseInfo.Collision)
        {
            case TileCollisionType.Blocked:
                return false;
            case TileCollisionType.Water:
                return State.HasSurfAbility;
            case TileCollisionType.Warp:
                if (currentMap.TryGetWarp(tileX, tileY, out var foundWarp))
                {
                    warp = foundWarp;
                    return true;
                }
                return false;
            default:
                return true;
        }
    }

    private bool IsHazardTile(int tileX, int tileY)
    {
        byte overlayId = currentMap.GetOverlay(tileX, tileY);
        if (overlayId != Tileset.TileNone)
        {
            if (overlayId == Tileset.TileTallGrassId)
            {
                return true;
            }

            var overlayInfo = tileRules.Get(overlayId);
            if (overlayInfo.Collision == TileCollisionType.Water && !State.HasSurfAbility)
            {
                return true;
            }
        }

        byte baseId = currentMap.GetGround(tileX, tileY);
        if (baseId == Tileset.TileTallGrassId)
        {
            return true;
        }

        var baseInfo = tileRules.Get(baseId);
        if (baseInfo.Collision == TileCollisionType.Water && !State.HasSurfAbility)
        {
            return true;
        }

        return false;
    }

    private void EvaluateHazardState()
    {
        if (currentMap is null)
        {
            return;
        }

        if (State.CurrentHealth <= 0)
        {
            PlayerDefeated?.Invoke();
            return;
        }

        if (State.PixelX < 0 || State.PixelY < 0)
        {
            return;
        }

        int tileX = State.PixelX / Tileset.TileWidth;
        int tileY = State.PixelY / Tileset.TileHeight;

        if ((uint)tileX >= (uint)currentMap.Width || (uint)tileY >= (uint)currentMap.Height)
        {
            return;
        }

        if (!IsHazardTile(tileX, tileY))
        {
            return;
        }

        if (State.DamageCooldownTimer > 0)
        {
            return;
        }

        State.CurrentHealth = Math.Max(0, State.CurrentHealth - 1);
        State.DamageCooldownTimer = DamageCooldownDuration;

        if (State.CurrentHealth <= 0)
        {
            PlayerDefeated?.Invoke();
        }
    }

    private void ExecuteWarp(WarpPoint warp)
    {
        if (!warp.IsValid)
        {
            return;
        }

        if (!mapRepository.TryGetMap(warp.MapId, out var nextMap))
        {
            return;
        }

        currentMap = nextMap;
        currentMapId = warp.MapId;

        int destX = Math.Clamp(warp.TargetX, 0, currentMap.Width - 1);
        int destY = Math.Clamp(warp.TargetY, 0, currentMap.Height - 1);

        State.SnapToTile(destX, destY);
    }

    private void SetCurrentMap(string mapId)
    {
        if (!mapRepository.TryGetMap(mapId, out var map))
        {
            currentMap = mapRepository.GetDefaultMap();
            currentMapId = mapRepository.DefaultMapId;
            return;
        }

        currentMap = map;
        currentMapId = mapId;
    }
}
