using System;
using System.Collections.Generic;
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
    /// Task storage, kept inside the colony save (this component lives on the SaveGame object).
    /// Tasks are saved whenever the game saves, so they follow the save everywhere (e.g. Steam
    /// Cloud) and match the point in time of whichever save you load.
    ///
    /// Removing the mod is safe: the game skips this unknown component when loading.
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
            Load(); // payload has been deserialized by now
        }

        protected override void OnCleanUp()
        {
            if (Instance == this) Instance = null;
            base.OnCleanUp();
        }

        private void Load()
        {
            items = null;
            if (!string.IsNullOrEmpty(payload))
            {
                try
                {
                    items = JsonConvert.DeserializeObject<List<TodoItem>>(payload);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TodoListClean] could not read tasks from save: " + ex.Message);
                }
            }
            if (items == null) items = new List<TodoItem>();
        }

        /// <summary>Call after every change: updates the copy written into the next game save.</summary>
        public void Save()
        {
            try
            {
                payload = JsonConvert.SerializeObject(items ?? new List<TodoItem>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TodoListClean] could not serialize tasks: " + ex.Message);
            }
        }
    }
}
