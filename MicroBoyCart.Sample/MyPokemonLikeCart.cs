using MicroBoy;
using System;
using MicroBoyCart.Sample.Audio;
using MicroBoyCart.Sample.Gameplay;
using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.Rendering;

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
        private readonly TileRenderer tileRenderer;
        private readonly HudRenderer hudRenderer;
        private readonly WalkingTheme walkingTheme;

        private bool showSaveMessage;
        private double saveMessageTimer;
        private const double SaveMessageDuration = 2.0;
        private string currentMessage = "GAME SAVED!";

        public MyPokemonLikeCart()
        {
            mapRepository = new MapRepository();
            tileRules = new TileRules();
            playerController = new PlayerController(mapRepository, tileRules);
            tileRenderer = new TileRenderer();
            hudRenderer = new HudRenderer();
            walkingTheme = new WalkingTheme();

            playerController.PlayerDefeated += HandlePlayerDefeat;
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

            playerController.Update(input, dt);
        }

        public void Render(Span<byte> frame)
        {
            tileRenderer.Render(frame, playerController.CurrentMap, playerController.State);
            hudRenderer.Render(frame, playerController.State, showSaveMessage, currentMessage);
        }

        public void MixAudio(Span<float> buffer)
        {
            walkingTheme.MixAudio(buffer, playerController.State.IsMoving, AudioSampleRate, AudioChannelCount);
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
