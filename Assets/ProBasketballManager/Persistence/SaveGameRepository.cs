using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace ProBasketballManager.Persistence
{
    public sealed class SaveSlotInfo
    {
        public string SlotName { get; set; }

        public string FilePath { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public string Description { get; set; }

        public string Error { get; set; }

        public bool IsReadable => Error == null;
    }

    public static class SaveGameRepository
    {
        private const string SaveFolderName = "saves";
        private const string SaveFileExtension = ".pbmsave";
        private const string TemporaryExtension = ".tmp";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveFolderName);

        public static void Save(GameSessionSnapshot snapshot, string slotName)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var safeName = SanitiseSlotName(slotName);

            var dto = SaveGameMapper.ToDto(snapshot, safeName);
            var json = JsonConvert.SerializeObject(dto, SerializerSettings);

            Directory.CreateDirectory(SaveDirectory);

            var finalPath = GetPathForSlot(safeName);
            var temporaryPath = finalPath + TemporaryExtension;

            // Write, then swap. If the write fails halfway the existing save survives.
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            File.Move(temporaryPath, finalPath);
        }

        public static GameSessionSnapshot Load(string slotName)
        {
            var path = GetPathForSlot(SanitiseSlotName(slotName));

            if (!File.Exists(path))
            {
                throw new SaveGameException($"No save called '{slotName}' was found.");
            }

            return LoadFromPath(path);
        }

        public static GameSessionSnapshot LoadFromPath(string path)
        {
            string json;

            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                throw new SaveGameException($"The save file could not be read: {exception.Message}", exception);
            }

            SaveGameDto dto;

            try
            {
                dto = JsonConvert.DeserializeObject<SaveGameDto>(json, SerializerSettings);
            }
            catch (JsonException exception)
            {
                throw new SaveGameException("The save file is not valid JSON and may be corrupt.", exception);
            }

            // Any reference that fails to resolve is reported here rather than
            // producing a half built season that breaks later.
            return SaveGameMapper.FromDto(dto);
        }

        public static IReadOnlyList<SaveSlotInfo> ListSaves()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                return Array.Empty<SaveSlotInfo>();
            }

            var results = new List<SaveSlotInfo>();

            foreach (var path in Directory.GetFiles(SaveDirectory, "*" + SaveFileExtension))
            {
                results.Add(ReadSlotInfo(path));
            }

            return results
                .OrderByDescending(slot => slot.SavedAtUtc)
                .ToList();
        }

        private static SaveSlotInfo ReadSlotInfo(string path)
        {
            var slotName = Path.GetFileNameWithoutExtension(path);

            try
            {
                var dto = JsonConvert.DeserializeObject<SaveGameDto>(File.ReadAllText(path), SerializerSettings);

                if (dto == null)
                {
                    return new SaveSlotInfo { SlotName = slotName, FilePath = path, Error = "The file is empty." };
                }

                DateTime.TryParse(dto.SavedAtUtc, out var savedAt);

                return new SaveSlotInfo
                {
                    SlotName = slotName,
                    FilePath = path,
                    SavedAtUtc = savedAt,
                    Description = dto.SchemaVersion == SaveGameMapper.CurrentSchemaVersion
                        ? dto.Description
                        : $"Incompatible save (schema {dto.SchemaVersion})",
                    Error = dto.SchemaVersion == SaveGameMapper.CurrentSchemaVersion
                        ? null
                        : $"Written by a different version of the game."
                };
            }
            catch (Exception exception)
            {
                return new SaveSlotInfo
                {
                    SlotName = slotName,
                    FilePath = path,
                    Error = exception.Message
                };
            }
        }

        public static bool Exists(string slotName)
        {
            return File.Exists(GetPathForSlot(SanitiseSlotName(slotName)));
        }

        public static void Delete(string slotName)
        {
            var path = GetPathForSlot(SanitiseSlotName(slotName));

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string GetPathForSlot(string safeSlotName)
        {
            return Path.Combine(SaveDirectory, safeSlotName + SaveFileExtension);
        }

        public static string SanitiseSlotName(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return "save";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(slotName.Length);

            foreach (var character in slotName.Trim())
            {
                builder.Append(Array.IndexOf(invalid, character) >= 0 || character == '.' ? '_' : character);
            }

            var cleaned = builder.ToString().Trim('_', ' ');

            return cleaned.Length == 0 ? "save" : cleaned;
        }
    }
}