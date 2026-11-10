using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TodoListClean
{
    /// <summary>
    /// The Todo List Clean panel, docked directly above the game's warning notifications.
    ///
    /// Normal view:   Task text ......................   (click = fly to saved view)
    /// Settings view: Task text ......... [top][▲][▼][bottom][camera][×]   (click text = rename, no camera move)
    ///
    /// Header: title (click = collapse), + add task, gear = settings.
    ///
    /// Rows are created once and re-bound to tasks on every change (never destroyed and
    /// rebuilt), and layout + the notification offset are applied in the same frame, so
    /// clicking, reordering and renaming don't make the panel flash.
    /// </summary>
    public sealed class TodoScreen : KScreen
    {
        public static TodoScreen Instance { get; private set; }

        private const float PanelWidth = 290f; // room for 6 settings buttons + task name
        private const float RowButton = 20f;
        private const float NotificationGap = 4f;
        private const string PrefCollapsed = "TodoListClean.Collapsed";

        private sealed class RowView
        {
            public GameObject Go;
            public Image Bg;
            public GameObject LabelGo;
            public TextMeshProUGUI Label;
            public TMP_InputField Input;
            public bool InputShown;
            public GameObject Controls;
            public TodoItem Item;
        }

        private RectTransform root;
        private RectTransform body;
        private TextMeshProUGUI titleText;
        private Image titleArrow;
        private Button settingsButton;
        private PanelHover hover;
        private readonly List<RowView> rows = new List<RowView>();

        private bool settingsMode;
        private bool collapsed;
        private float itemBg;        // from config.json: 0 = no background (default), 1 = opaque
        private bool dirty = true;
        private bool layoutDriven;

        private TodoItem editingItem;
        private bool editingIsNew;
        private int lastEditEndFrame = -10;

        // When the notification column is not driven by a layout group, we sit at its
        // top and push it down by our own height.
        private RectTransform notifRT;
        private Vector2 notifOrigin;
        private float appliedShift = -1f;

        private static int ActiveWorld
        {
            get { return ClusterManager.Instance != null ? ClusterManager.Instance.activeWorldId : 0; }
        }

        private static List<TodoItem> Items
        {
            get { return TodoStorage.Instance != null ? TodoStorage.Instance.Items : null; }
        }

        private bool IsTyping { get { return editingItem != null; } }

        // ------------------------------------------------------------------ lifecycle

        public static void Create()
        {
            if (Instance != null) return;
            var gsm = GameScreenManager.Instance;
            if (gsm == null || gsm.ssOverlayCanvas == null) return;

            var go = new GameObject("TodoListClean", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(gsm.ssOverlayCanvas.transform, false); // re-parented in Dock()
            go.AddComponent<TodoScreen>();
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            collapsed = PlayerPrefs.GetInt(PrefCollapsed, 0) == 1;
            itemBg = TodoOptions.Load().ItemBackground / 100f;
            layoutDriven = Dock();
            BuildShell();
            Activate();
            ConsumeMouseScroll = true; // after Activate, or KScreen warns the screen isn't active yet
            Refresh();
        }

        protected override void OnCleanUp()
        {
            if (notifRT != null) notifRT.anchoredPosition = notifOrigin;
            if (Instance == this) Instance = null;
            base.OnCleanUp();
        }

        public override float GetSortKey()
        {
            return 50f; // ahead of tools/camera so typing never triggers hotkeys
        }

        private void LateUpdate()
        {
            if (dirty) Refresh();
            ApplyNotificationShift(); // also catches height changes from text wrapping
        }

        // ------------------------------------------------------------------ placement

        /// <summary>
        /// Put the panel directly above the warning notifications ("No outhouse built" etc.).
        /// Returns true when a vertical layout group positions and sizes us.
        /// </summary>
        private bool Dock()
        {
            root = (RectTransform)transform;
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(PanelWidth, 0f);
            UIKit.Size(gameObject, PanelWidth);

            var ns = NotificationScreen.Instance;
            var nrt = ns != null ? ns.transform as RectTransform : null;
            var parent = nrt != null ? nrt.parent as RectTransform : null;
            if (parent == null)
            {
                root.anchoredPosition = new Vector2(12f, -240f); // fallback: left side, under the HUD
                return false;
            }

            root.SetParent(parent, false);

            var vlg = parent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                // Parent stacks its children: just become the entry before the notifications.
                root.SetSiblingIndex(nrt.GetSiblingIndex());
                return vlg.childControlHeight;
            }

            // Free layout: take the notifications' top-left corner and push them down.
            GetComponent<LayoutElement>().ignoreLayout = true;
            var corners = new Vector3[4];
            nrt.GetWorldCorners(corners); // [1] = top-left
            Vector3 local = parent.InverseTransformPoint(corners[1]);
            var pr = parent.rect;
            root.anchoredPosition = new Vector2(local.x - pr.xMin, local.y - pr.yMax);
            root.SetSiblingIndex(nrt.GetSiblingIndex() + 1);

            notifRT = nrt;
            notifOrigin = nrt.anchoredPosition;
            return false;
        }

        private void ApplyNotificationShift()
        {
            if (notifRT == null || root == null) return;
            float h = root.rect.height;
            if (Mathf.Abs(h - appliedShift) <= 0.5f) return;
            appliedShift = h;
            notifRT.anchoredPosition = notifOrigin + new Vector2(0f, -(h + NotificationGap));
        }

        // ------------------------------------------------------------------ input

        public override void OnKeyDown(KButtonEvent e)
        {
            if (IsTyping || Time.frameCount - lastEditEndFrame <= 1)
            {
                e.Consumed = true; // swallow hotkeys (and the Esc that cancels an edit)
                return;
            }
            if (hover != null && hover.IsOver)
            {
                e.TryConsume(Action.MouseLeft);
                e.TryConsume(Action.MouseRight);
                e.TryConsume(Action.ZoomIn);
                e.TryConsume(Action.ZoomOut);
            }
            if (!e.Consumed) base.OnKeyDown(e);
        }

        public override void OnKeyUp(KButtonEvent e)
        {
            if (IsTyping || Time.frameCount - lastEditEndFrame <= 1)
            {
                e.Consumed = true;
                return;
            }
            if (hover != null && hover.IsOver)
            {
                e.TryConsume(Action.MouseLeft);
                e.TryConsume(Action.MouseRight);
            }
            if (!e.Consumed) base.OnKeyUp(e);
        }

        // ------------------------------------------------------------------ building (once)

        private void BuildShell()
        {
            UIKit.VColumn(gameObject, 0f, new RectOffset(0, 0, 0, 0));
            if (!layoutDriven)
            {
                var fit = gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            hover = gameObject.AddComponent<PanelHover>();

            var header = UIKit.Make("Header", root);
            header.gameObject.AddComponent<Image>().color = UIKit.HeaderBg;
            UIKit.HRow(header.gameObject, 3f, new RectOffset(4, 4, 3, 3));

            var title = UIKit.LabelButton(header, "To Do", 15f, UIKit.Clear, UIKit.TextColor, 22f, ToggleCollapsed, out titleText, "arrow", 12f);
            var arrow = title.transform.Find("Icon");
            if (arrow != null) titleArrow = arrow.GetComponent<Image>();
            UIKit.IconButton(header, "plus", UIKit.Clear, UIKit.TextColor, 22f, AddTask);
            settingsButton = UIKit.IconButton(header, "gear", UIKit.Clear, UIKit.TextColor, 22f, ToggleSettings);

            body = UIKit.Make("Body", root);
            UIKit.VColumn(body.gameObject, 1f, new RectOffset(0, 0, 1, 0));
        }

        private RowView CreateRow()
        {
            var v = new RowView();
            var rt = UIKit.Make("Row", body);
            v.Go = rt.gameObject;
            v.Bg = v.Go.AddComponent<Image>(); // alpha 0 still blocks clicks from reaching the world
            UIKit.HRow(v.Go, 2f, new RectOffset(4, 4, 1, 1));

            TextMeshProUGUI label;
            var labelButton = UIKit.LabelButton(rt, "", 14f, UIKit.Clear, UIKit.TextColor, RowButton,
                () => OnLabelClicked(v), out label);
            v.LabelGo = labelButton.gameObject;
            v.Label = label;

            v.Input = UIKit.Input(rt, "", 14f, value => OnEndEdit(v, value));
            v.Input.gameObject.SetActive(false);

            var crt = UIKit.Make("Controls", rt);
            UIKit.HRow(crt.gameObject, 2f, new RectOffset(0, 0, 0, 0));
            v.Controls = crt.gameObject;
            UIKit.IconButton(crt, "top", UIKit.ButtonBg, UIKit.TextColor, RowButton, () => { if (v.Item != null) MoveToEnd(v.Item, true); });
            UIKit.IconButton(crt, "up", UIKit.ButtonBg, UIKit.TextColor, RowButton, () => { if (v.Item != null) Move(v.Item, -1); });
            UIKit.IconButton(crt, "down", UIKit.ButtonBg, UIKit.TextColor, RowButton, () => { if (v.Item != null) Move(v.Item, +1); });
            UIKit.IconButton(crt, "bottom", UIKit.ButtonBg, UIKit.TextColor, RowButton, () => { if (v.Item != null) MoveToEnd(v.Item, false); });
            UIKit.IconButton(crt, "camera", UIKit.ButtonBg, UIKit.TextColor, RowButton, () => { if (v.Item != null) SetViewHere(v.Item); });
            UIKit.IconButton(crt, "cross", UIKit.Danger, UIKit.TextColor, RowButton, () => { if (v.Item != null) Delete(v.Item); });
            v.Controls.SetActive(false);
            return v;
        }

        // ------------------------------------------------------------------ updating (in place)

        private void MarkDirty() { dirty = true; }

        private void Changed()
        {
            if (TodoStorage.Instance != null) TodoStorage.Instance.Save();
            MarkDirty();
        }

        private void Refresh()
        {
            var items = Items;
            if (items == null) return; // storage not ready yet; stays dirty, retried next frame
            dirty = false;

            SetText(titleText, "To Do");
            if (titleArrow != null) // same arrow as the game's panels: points down, rotated to point right when collapsed
                titleArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, UIKit.ArrowBaseRotation + (collapsed ? 90f : 0f));
            UIKit.SetColors(settingsButton, settingsMode ? UIKit.Accent : UIKit.Clear);

            SetActive(body.gameObject, !collapsed);
            if (!collapsed)
            {
                while (rows.Count < items.Count) rows.Add(CreateRow());
                var bg = new Color(UIKit.RowBgBase.r, UIKit.RowBgBase.g, UIKit.RowBgBase.b, itemBg);
                for (int i = 0; i < rows.Count; i++)
                {
                    var v = rows[i];
                    if (i < items.Count)
                    {
                        Bind(v, items[i], bg);
                        SetActive(v.Go, true);
                    }
                    else
                    {
                        v.Item = null;
                        SetActive(v.Go, false);
                    }
                }
            }

            // Lay out now, not at the next canvas update, so nothing is drawn at a stale size.
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            if (layoutDriven && root.parent is RectTransform parentRT)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
            ApplyNotificationShift();
        }

        private void Bind(RowView v, TodoItem it, Color bg)
        {
            v.Item = it;
            if (v.Bg.color != bg) v.Bg.color = bg;

            bool editing = it == editingItem;
            SetActive(v.LabelGo, !editing);
            if (!editing) SetText(v.Label, it.Text);

            if (editing && !v.InputShown)
            {
                v.InputShown = true;
                v.Input.text = it.Text;
                SetActive(v.Input.gameObject, true);
                StartCoroutine(FocusNextFrame(v.Input));
            }
            else if (!editing && v.InputShown)
            {
                v.InputShown = false;
                SetActive(v.Input.gameObject, false);
            }

            SetActive(v.Controls, settingsMode);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active) go.SetActive(active);
        }

        private static void SetText(TextMeshProUGUI t, string value)
        {
            if (t != null && t.text != value) t.text = value; // avoid needless re-mesh
        }

        private static IEnumerator FocusNextFrame(TMP_InputField f)
        {
            yield return null;
            if (f != null && f.gameObject.activeInHierarchy)
            {
                f.Select();
                f.ActivateInputField();
            }
        }

        // ------------------------------------------------------------------ header actions

        private void ToggleCollapsed()
        {
            collapsed = !collapsed;
            PlayerPrefs.SetInt(PrefCollapsed, collapsed ? 1 : 0);
            Refresh();
        }

        private void ToggleSettings()
        {
            settingsMode = !settingsMode;
            Refresh();
        }

        private void AddTask()
        {
            var items = Items;
            if (items == null || editingItem != null) return;
            var it = new TodoItem();
            CaptureView(it);
            items.Add(it);
            if (collapsed)
            {
                collapsed = false;
                PlayerPrefs.SetInt(PrefCollapsed, 0);
            }
            editingItem = it;
            editingIsNew = true;
            if (TodoStorage.Instance != null) TodoStorage.Instance.Save();
            Refresh();
        }

        // ------------------------------------------------------------------ row actions

        private void Jump(TodoItem it)
        {
            var cam = CameraController.Instance;
            if (cam == null) return;
            var pos = new Vector3(it.X, it.Y, cam.transform.position.z);
            float zoom = it.Zoom > 0f ? it.Zoom : 10f;

            if (DlcManager.IsExpansion1Active() && ClusterManager.Instance != null && it.WorldId != ActiveWorld
                && ClusterManager.Instance.GetWorld(it.WorldId) != null)
            {
                cam.ActiveWorldStarWipe(it.WorldId, pos, zoom, null);
            }
            else
            {
                cam.SetTargetPos(pos, zoom, true);
            }
        }

        private static void CaptureView(TodoItem it)
        {
            var cam = CameraController.Instance;
            if (cam != null)
            {
                var p = cam.transform.position;
                it.X = p.x;
                it.Y = p.y;
            }
            it.Zoom = Camera.main != null ? Camera.main.orthographicSize : 10f;
            it.WorldId = ActiveWorld;
        }

        private void SetViewHere(TodoItem it)
        {
            CaptureView(it);
            if (TodoStorage.Instance != null) TodoStorage.Instance.Save(); // nothing visible changes
        }

        /// <summary>Normal view: fly to the task. Settings view: edit its name in place instead.</summary>
        private void OnLabelClicked(RowView v)
        {
            if (v.Item == null) return;
            if (settingsMode) StartRename(v.Item);
            else Jump(v.Item);
        }

        private void StartRename(TodoItem it)
        {
            if (editingItem != null) return;
            editingItem = it;
            editingIsNew = false;
            Refresh();
        }

        private void OnEndEdit(RowView v, string value)
        {
            var it = editingItem;
            if (it == null || v.Item != it) return;
            editingItem = null;
            lastEditEndFrame = Time.frameCount;

            bool cancelled = v.Input.wasCanceled;
            string text = (value ?? "").Trim();

            if (cancelled)
            {
                if (editingIsNew && string.IsNullOrEmpty(it.Text)) Remove(it);
            }
            else if (text.Length > 0)
            {
                it.Text = text;
            }
            else if (string.IsNullOrEmpty(it.Text))
            {
                Remove(it);
            }
            editingIsNew = false;
            Changed(); // applied in LateUpdate, outside the input field's own callback
        }

        private static void Remove(TodoItem it)
        {
            var items = Items;
            if (items != null) items.Remove(it);
        }

        private void Delete(TodoItem it)
        {
            Remove(it);
            if (editingItem == it) editingItem = null;
            Changed();
            Refresh();
        }

        private void MoveToEnd(TodoItem it, bool toTop)
        {
            var items = Items;
            if (items == null) return;
            int a = items.IndexOf(it);
            if (a < 0 || a == (toTop ? 0 : items.Count - 1)) return;
            items.RemoveAt(a);
            if (toTop) items.Insert(0, it);
            else items.Add(it);
            Changed();
            Refresh();
        }

        private void Move(TodoItem it, int dir)
        {
            var items = Items;
            if (items == null) return;
            int a = items.IndexOf(it);
            int b = a + dir;
            if (a < 0 || b < 0 || b >= items.Count) return;
            items[a] = items[b];
            items[b] = it;
            Changed();
            Refresh();
        }
    }
}
