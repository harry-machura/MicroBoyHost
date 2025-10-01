using MicroBoy;
using System;
using MicroBoyCart.Sample.Audio;
using MicroBoyCart.Sample.Gameplay;
using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.Rendering;
using MicroBoyCart.Sample.UI;

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
        private readonly PauseMenuState pauseMenuState;
        private readonly PauseMenuRenderer pauseMenuRenderer;

        private Buttons previousButtons;

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
            pauseMenuState = new PauseMenuState();
            pauseMenuRenderer = new PauseMenuRenderer();

            playerController.PlayerDefeated += HandlePlayerDefeat;
        }

        public void Init()
        {
            showSaveMessage = false;
            saveMessageTimer = 0;
            pauseMenuState.Close();
            previousButtons = Buttons.None;
            LoadOrStartNewGame();
        }

        public void Update(Input input, double dt)
        {
            UpdateSaveMessage(dt);

            bool startPressed = input.IsDown(Buttons.Start) && (previousButtons & Buttons.Start) == 0;
            if (startPressed)
            {
                if (pauseMenuState.IsOpen)
                {
                    pauseMenuState.Close();
                }
                else if (!playerController.State.IsMoving)
                {
                    pauseMenuState.Open();
                }
            }

            if (pauseMenuState.IsOpen)
            {
                HandlePauseMenuInput(input);
                previousButtons = input.Buttons;
                return;
            }

            if (!playerController.State.IsMoving && input.IsDown(Buttons.Select) && (previousButtons & Buttons.Select) == 0)
            {
                LoadGameFromDisk();
                previousButtons = input.Buttons;
                return;
            }

            playerController.Update(input, dt);
            previousButtons = input.Buttons;
        }

        public void Render(Span<byte> frame)
        {
            tileRenderer.Render(frame, playerController.CurrentMap, playerController.State);
            hudRenderer.Render(frame, playerController.State, showSaveMessage, currentMessage);
            pauseMenuRenderer.Render(frame, pauseMenuState);
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

        private void HandlePauseMenuInput(Input input)
        {
            if (input.IsDown(Buttons.Down) && (previousButtons & Buttons.Down) == 0)
            {
                pauseMenuState.MoveNext();
            }
            else if (input.IsDown(Buttons.Up) && (previousButtons & Buttons.Up) == 0)
            {
                pauseMenuState.MovePrevious();
            }

            if (input.IsDown(Buttons.B) && (previousButtons & Buttons.B) == 0)
            {
                pauseMenuState.Close();
                return;
            }

            if (input.IsDown(Buttons.A) && (previousButtons & Buttons.A) == 0)
            {
                if (pauseMenuState.IsSaveSelected())
                {
                    SaveGame();
                }
                else if (pauseMenuState.IsSettingsSelected())
                {
                    HandleSettingsSelection();
                }
                else if (pauseMenuState.IsResumeSelected())
                {
                    pauseMenuState.Close();
                }
            }
        }

        private void HandleSettingsSelection()
        {
            ShowMessage("TODO SETTINGS MENU");
        }
    }
}
