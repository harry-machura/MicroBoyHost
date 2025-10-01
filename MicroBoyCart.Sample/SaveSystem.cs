using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MicroBoyCart.Sample
{
    public static class SaveSystem
    {
        private static readonly string SaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MicroBoy",
            "Saves"
        );

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public static bool Save(GameSaveData data, string saveName = "save1")
        {
            try
            {
                Directory.CreateDirectory(SaveDirectory);

                string filePath = Path.Combine(SaveDirectory, $"{saveName}.json");
                string json = JsonSerializer.Serialize(data, JsonOptions);

                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        public static GameSaveData? Load(string saveName = "save1")
        {
            try
            {
                string filePath = Path.Combine(SaveDirectory, $"{saveName}.json");

                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Load failed: {ex.Message}");
                return null;
            }
        }

        public static bool SaveExists(string saveName = "save1")
        {
            string filePath = Path.Combine(SaveDirectory, $"{saveName}.json");
            return File.Exists(filePath);
        }

        public static bool DeleteSave(string saveName = "save1")
        {
            try
            {
                string filePath = Path.Combine(SaveDirectory, $"{saveName}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete failed: {ex.Message}");
                return false;
            }
        }

        public static SaveInfo[] GetAllSaves()
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    return Array.Empty<SaveInfo>();

                var files = Directory.GetFiles(SaveDirectory, "*.json");
                var saves = new SaveInfo[files.Length];

                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    var saveName = Path.GetFileNameWithoutExtension(fileInfo.Name);

                    var data = Load(saveName);
                    saves[i] = new SaveInfo
                    {
                        SaveName = saveName,
                        LastModified = fileInfo.LastWriteTime,
                        PlayTimeSeconds = data?.PlayTimeSeconds ?? 0,
                        CurrentMap = data?.CurrentMapId ?? "unknown",
                        PlayerHealth = data?.CurrentHealth ?? 0
                    };
                }

                return saves;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllSaves failed: {ex.Message}");
                return Array.Empty<SaveInfo>();
            }
        }
    }

    public class GameSaveData
    {
        public int PlayerTileX { get; set; }
        public int PlayerTileY { get; set; }
        public string CurrentMapId { get; set; } = "overworld";

        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }

        public bool HasSurfAbility { get; set; }
        public bool HasCutAbility { get; set; }
        public bool HasFlashAbility { get; set; }

        public int Potions { get; set; }
        public int Keys { get; set; }

        public HashSet<string> CollectedItems { get; set; } = new();
        public HashSet<string> DefeatedEnemies { get; set; } = new();
        public HashSet<string> CompletedQuests { get; set; } = new();
        public Dictionary<string, bool> GameFlags { get; set; } = new();

        public DateTime SaveDate { get; set; } = DateTime.Now;
        public double PlayTimeSeconds { get; set; }
        public int SaveVersion { get; set; } = 1;
    }

    public struct SaveInfo
    {
        public string SaveName { get; set; }
        public DateTime LastModified { get; set; }
        public double PlayTimeSeconds { get; set; }
        public string CurrentMap { get; set; }
        public int PlayerHealth { get; set; }

        public string PlayTimeFormatted => TimeSpan.FromSeconds(PlayTimeSeconds).ToString(@"hh\:mm\:ss");
    }
}