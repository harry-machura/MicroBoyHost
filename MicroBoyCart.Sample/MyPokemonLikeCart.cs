using MicroBoy;
using MicroBoyCart.Sample.Audio;
using MicroBoyCart.Sample.Gameplay;
using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.NPCs;
using MicroBoyCart.Sample.Rendering;
using System;

namespace MicroBoyCart.Sample
{
    public sealed class MyPokemonLikeCart : ICartridge
    {
        public string Title => "MicroBoy Demo Map";
        public string Author => "Harry";
        public int AudioSampleRate => 44100;
        public int AudioChannelCount => 2;

        private readonly MapRepository mapRepository;
        private readonly TileRules tileRules;
        private readonly PlayerController playerController;
        private readonly NpcController npcController;
        private readonly TileRenderer tileRenderer;
        private readonly NpcRenderer npcRenderer;
        private readonly HudRenderer hudRenderer;
        private readonly WalkingTheme walkingTheme;
        private readonly DialogSystem dialogSystem;

        private bool showSaveMessage;
        private double saveMessageTimer;
        private const double SaveMessageDuration = 2.0;
        private string currentMessage = "GAME SAVED!";

        public MyPokemonLikeCart()
        {
            mapRepository = new MapRepository();
            tileRules = new TileRules();
            playerController = new PlayerController(mapRepository, tileRules);
            npcController = new NpcController(tileRules);
            tileRenderer = new TileRenderer();
            npcRenderer = new NpcRenderer();
            hudRenderer = new HudRenderer();
            walkingTheme = new WalkingTheme();
            dialogSystem = new DialogSystem();

            playerController.PlayerDefeated += HandlePlayerDefeat;

            // NPCs laden
            foreach (var npc in NpcRepository.CreateDefaultNpcs())
            {
                npcController.AddNpc(npc);
            }
        }

        public void Init()
        {
            showSaveMessage = false;
            saveMessageTimer = 0;
            LoadOrStartNewGame();
        }

        public void Update(Input input, double dt)
        {
            UpdateSaveMessage(dt);

            // Dialog hat Priorität - blockiert andere Inputs
            if (dialogSystem.IsActive)
            {
                dialogSystem.Update(input, dt);
                return;
            }

            // Interaktion mit NPCs (A-Taste)
            if (!playerController.State.IsMoving && input.IsDown(Buttons.A))
            {
                var npc = npcController.GetNpcInFront(
                    playerController.State.TileX,
                    playerController.State.TileY,
                    playerController.State.Direction,
                    playerController.CurrentMapId
                );

                if (npc != null)
                {
                    dialogSystem.StartDialog(npc.Name, npc.DialogPages);
                    return;
                }
            }

            // Speichern/Laden (nur wenn kein Dialog aktiv)
            if (!playerController.State.IsMoving && input.IsDown(Buttons.Start))
            {
                SaveGame();
                return;
            }

            if (!playerController.State.IsMoving && input.IsDown(Buttons.Select))
            {
                LoadGameFromDisk();
                return;
            }

            // Normale Spieler-Updates
            playerController.Update(input, dt);

            // NPC-Updates (Wander-Bewegung etc.)
            npcController.Update(dt, playerController.CurrentMap, playerController.CurrentMapId);
        }

        public void Render(Span<byte> frame)
        {
            // 1. Map rendern
            tileRenderer.Render(frame, playerController.CurrentMap, playerController.State);

            // 2. NPCs rendern (vor dem HUD, damit Dialog darüber liegt)
            var cameraX = CalculateCameraX();
            var cameraY = CalculateCameraY();
            var visibleNpcs = npcController.GetNpcsForMap(playerController.CurrentMapId);
            npcRenderer.Render(frame, visibleNpcs, cameraX, cameraY);

            // 3. HUD rendern (Health Bar, Save Messages)
            hudRenderer.Render(frame, playerController.State, showSaveMessage, currentMessage);

            // 4. Dialog rendern (immer ganz oben)
            dialogSystem.Render(frame);
        }

        public void MixAudio(Span<float> buffer)
        {
            walkingTheme.MixAudio(buffer, playerController.State.IsMoving, AudioSampleRate, AudioChannelCount);
        }

        private int CalculateCameraX()
        {
            var map = playerController.CurrentMap;
            int mapPixelWidth = map.Width * 8; // TileWidth = 8
            int camX = playerController.State.PixelX - MicroBoySpec.W / 2;
            return Math.Clamp(camX, 0, Math.Max(0, mapPixelWidth - MicroBoySpec.W));
        }

        private int CalculateCameraY()
        {
            var map = playerController.CurrentMap;
            int mapPixelHeight = map.Height * 8; // TileHeight = 8
            int camY = playerController.State.PixelY - MicroBoySpec.H / 2;
            return Math.Clamp(camY, 0, Math.Max(0, mapPixelHeight - MicroBoySpec.H));
        }

        private void LoadOrStartNewGame()
        {
            walkingTheme.Reset();
            var saveData = SaveSystem.Load();
            if (saveData != null)
            {
                playerController.LoadFromSaveData(saveData);
            }
            else
            {
                playerController.StartNewGame();
            }
        }

        private void SaveGame()
        {
            var saveData = playerController.CreateSaveData();
            if (SaveSystem.Save(saveData))
            {
                ShowMessage("GAME SAVED!");
            }
            else
            {
                ShowMessage("SAVE FAILED!");
            }
        }

        private void LoadGameFromDisk()
        {
            var saveData = SaveSystem.Load();
            if (saveData != null)
            {
                playerController.LoadFromSaveData(saveData);
                walkingTheme.Reset();
                ShowMessage("GAME LOADED!");
            }
        }

        private void HandlePlayerDefeat()
        {
            var saveData = SaveSystem.Load();
            if (saveData != null)
            {
                playerController.LoadFromSaveData(saveData);
                ShowMessage("GAME LOADED!");
            }
            else
            {
                playerController.StartNewGame();
                showSaveMessage = false;
            }

            walkingTheme.Reset();
        }

        private void ShowMessage(string message)
        {
            currentMessage = message;
            showSaveMessage = true;
            saveMessageTimer = SaveMessageDuration;
        }

        private void UpdateSaveMessage(double dt)
        {
            if (saveMessageTimer > 0)
            {
                saveMessageTimer = Math.Max(0, saveMessageTimer - dt);
                if (saveMessageTimer == 0)
                {
                    showSaveMessage = false;
                }
            }
        }
    }
}
