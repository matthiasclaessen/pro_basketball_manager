using System.IO;
using Newtonsoft.Json;
using ProBasketballManager.Persistence;
using UnityEditor;
using UnityEngine;

namespace ProBasketballManager.EditorTools
{
    public static class DatabaseGeneratorMenu
    {
        [MenuItem("Pro Basketball Manager/Generate Default Database")]
        public static void GenerateDefaultDatabase()
        {
            var directory = GameDatabaseRepository.GetDatabaseDirectory(Application.streamingAssetsPath, GameDatabaseRepository.DefaultDatabaseName);

            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(Path.Combine(directory, "graphics"));

            var path = Path.Combine(directory, "database.json");

            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(DatabaseGenerator.Generate(), Formatting.Indented));
            }
            catch (IOException exception)
            {
                Debug.LogError($"Could not write the database to '{path}': {exception.Message}");

                return;
            }

            AssetDatabase.Refresh();

            Debug.Log($"Generated database at {path}");
        }
    }
}
