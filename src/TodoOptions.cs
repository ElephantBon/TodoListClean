using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace TodoListClean
{
    /// <summary>
    /// Shown as "Options" next to the mod in the main-menu Mods screen (via PLib).
    /// Saved to config.json in the mod's folder. Read when a colony loads.
    /// </summary>
    [JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public sealed class TodoOptions
    {
        [Option("Item background opacity", "Background behind each task row, in percent (default 10). 0 = no background.")]
        [Limit(0, 100)]
        [JsonProperty]
        public int ItemBackground { get; set; }

        public TodoOptions()
        {
            ItemBackground = 10;
        }

        /// <summary>Current settings from config.json (defaults if the file is missing or unreadable).</summary>
        public static TodoOptions Load()
        {
            TodoOptions opts = null;
            try { opts = POptions.ReadSettings<TodoOptions>(); }
            catch { opts = null; }
            if (opts == null) opts = new TodoOptions();
            if (opts.ItemBackground < 0) opts.ItemBackground = 0;
            if (opts.ItemBackground > 100) opts.ItemBackground = 100;
            return opts;
        }
    }
}
