using System;
using System.Collections.Generic;
using System.IO;
using KSerialization;
using Newtonsoft.Json;

namespace TodoListClean
{
    /// <summary>One task: a name plus the camera view (and asteroid) it jumps to.</summary>
    public sealed class TodoItem
    {
        public string Text = "";
        public float X;
        public float Y;
        public float Zoom = 10f;
        public int WorldId;
    }

    /// <summary>
    /// Task storage.
    ///
    /// Primary: tasks\&lt;colony name&gt;.json in the mod folder, written immediately on every
    /// change. Tasks survive even if you quit without saving or reload an older autosave.
    ///
    /// Secondary: a copy inside the colony save (this component lives on the SaveGame object).
    /// It is only used when no JSON file exists yet, e.g. the first load after updating the
    /// mod, or a save copied from another PC.
    /// </summary>
    [SerializationConfig(KSerialization.MemberSerialization.OptIn)]
    public sealed class TodoStorage : KMonoBehaviour
    {
        public static TodoStorage Instance { get; private set; }

        [Serialize] private string payload = "";

        private List<TodoItem> items;

        public List<TodoItem> Items
        {
            get
            {
                if (items == null) Load();
                return items;
            }
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Load(); // payload has been deserialized and the colony name is known by now
        }

        protected override void OnCleanUp()
        {
            if (Instance == this) Instance = null;
            base.OnCleanUp();
        }

        // ------------------------------------------------------------------ file location

        private static string FilePath
        {
            get
            {
                string dir = TodoListCleanMod.ModPath;
                if (string.IsNullOrEmpty(dir)) return null;
                string colony = null;
                try { colony = SaveGame.Instance != null ? SaveGame.Instance.BaseName : null; }
                catch { colony = null; }
                if (string.IsNullOrEmpty(colony)) colony = "default";
                foreach (char c in Path.GetInvalidFileNameChars()) colony = colony.Replace(c, '_');
                return Path.Combine(Path.Combine(dir, "tasks"), colony + ".json");
            }
        }

        // ------------------------------------------------------------------ load / save

        private void Load()
        {
            items = null;
            string path = FilePath;

            if (path != null && File.Exists(path))
            {
                items = Parse(File.ReadAllText(path), path);
                if (items != null)
                {
                    Debug.Log("[TodoListClean] loaded " + items.Count + " tasks from " + path);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(payload)) items = Parse(payload, "save file");
            if (items == null) items = new List<TodoItem>();
            if (items.Count > 0) WriteFile(); // migrate tasks that only existed in the save
        }

        private static List<TodoItem> Parse(string json, string source)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<TodoItem>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TodoListClean] could not read tasks from " + source + ": " + ex.Message);
                return null;
            }
        }

        /// <summary>Call after every change: writes the JSON file now and updates the in-save copy.</summary>
        public void Save()
        {
            try
            {
                payload = JsonConvert.SerializeObject(items ?? new List<TodoItem>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TodoListClean] could not serialize tasks: " + ex.Message);
                return;
            }
            WriteFile();
        }

        private void WriteFile()
        {
            string path = FilePath;
            if (path == null) return;
            try
            {
                if (string.IsNullOrEmpty(payload) && items != null)
                    payload = JsonConvert.SerializeObject(items);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, payload);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path); // never leaves a half-written file behind
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TodoListClean] could not write " + path + ": " + ex.Message);
            }
        }
    }
}
