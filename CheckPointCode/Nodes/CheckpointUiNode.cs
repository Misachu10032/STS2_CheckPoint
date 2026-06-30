using Godot;
using CheckPoint.Checkpoints;

namespace CheckPoint.Nodes;

// Static UI manager — deliberately NOT a Node subclass.
// Custom partial Node classes crash in mod context because Harmony's MonoMod
// JIT hook fires when Godot tries to JIT-compile InvokeGodotClassMethod and
// throws ArgumentException. Using a static class with built-in Godot nodes
// (CanvasLayer, Label, etc.) avoids this entirely.
// Input polling and HUD updates are driven by a Harmony patch on NRun._Process.
internal static class CheckpointUi
{
    private static Button?        _hudButton;
    private static Button?        _quickLoadButton;
    private static Control?       _panel;
    private static GridContainer? _list;

    private static double _lHeld;
    private static double _sHeld;
    private const  double ToggleHold = 0.3;

    // Called from NRunReadyPatch — builds the UI once per run scene.
    public static void Initialize(Node sceneRoot)
    {
        if (sceneRoot.HasNode("CheckpointUiLayer")) return;

        var layer = new CanvasLayer { Layer = 128, Name = "CheckpointUiLayer" };
        sceneRoot.AddChild(layer);
        BuildLayout(layer);
        MainFile.Logger.Info("[Checkpoint] CheckpointUi initialized.");
    }

    // Called every frame from NRunProcessPatch.
    public static void Update(double delta)
    {
        if (_hudButton == null) return;

        _hudButton.Text = "Checkpoint";

        if (Input.IsKeyPressed(Key.L))
        {
            if (_lHeld >= 0)
            {
                _lHeld += delta;
                if (_lHeld >= ToggleHold)
                {
                    _lHeld = double.MinValue;
                    TogglePanel();
                }
            }
        }
        else
        {
            _lHeld = 0;
        }

        if (Input.IsKeyPressed(Key.R))
        {
            if (_sHeld >= 0)
            {
                _sHeld += delta;
                if (_sHeld >= ToggleHold)
                {
                    _sHeld = double.MinValue;
                    QuickLoad();
                }
            }
        }
        else
        {
            _sHeld = 0;
        }

        if (_panel?.Visible == true && Input.IsKeyPressed(Key.Escape))
            _panel.Visible = false;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private static void BuildLayout(CanvasLayer layer)
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        layer.AddChild(root);

        _hudButton = new Button { Text = "Checkpoint" };
        _hudButton.AnchorLeft   = 0.5f;
        _hudButton.AnchorRight  = 0.5f;
        _hudButton.AnchorTop    = 0f;
        _hudButton.AnchorBottom = 0f;
        _hudButton.OffsetLeft   = -50f;
        _hudButton.OffsetRight  = 150f;
        _hudButton.OffsetTop    = 15f;
        _hudButton.OffsetBottom = 40f;
        _hudButton.Pressed      += TogglePanel;
        root.AddChild(_hudButton);

        _quickLoadButton = new Button { Text = "Quick SL" };
        _quickLoadButton.AnchorLeft   = 0.5f;
        _quickLoadButton.AnchorRight  = 0.5f;
        _quickLoadButton.AnchorTop    = 0f;
        _quickLoadButton.AnchorBottom = 0f;
        _quickLoadButton.OffsetLeft   = -50f;
        _quickLoadButton.OffsetRight  = 150f;
        _quickLoadButton.OffsetTop    = 43f;
        _quickLoadButton.OffsetBottom = 68f;
        _quickLoadButton.Pressed      += QuickLoad;
        root.AddChild(_quickLoadButton);

        _panel = new Control();
        _panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _panel.MouseFilter = Control.MouseFilterEnum.Stop;
        _panel.Visible     = false;
        root.AddChild(_panel);

        var overlay = new ColorRect { Color = new Color(0f, 0f, 0f, 0.7f) };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true }) _panel.Visible = false;
        };
        _panel.AddChild(overlay);

        var bg = new Panel();
        bg.AnchorLeft   = 0.20f;
        bg.AnchorTop    = 0.05f;
        bg.AnchorRight  = 0.80f;
        bg.AnchorBottom = 0.95f;
        _panel.AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_top",    16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        bg.AddChild(margin);

        var outer = new VBoxContainer();
        margin.AddChild(outer);

        var header = new HBoxContainer();
        outer.AddChild(header);
        header.AddChild(new Label
        {
            Text                = "Checkpoint History  (hold L or Esc to close)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        var closeBtn = new Button { Text = "X" };
        closeBtn.Pressed += () => _panel.Visible = false;
        header.AddChild(closeBtn);

        outer.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        outer.AddChild(scroll);

        _list = new GridContainer { Columns = 4 };
        _list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_list);
    }

    // ── Quick load ────────────────────────────────────────────────────────────

    private static void QuickLoad()
    {
        if (_panel?.Visible == true) _panel.Visible = false;
        CheckpointManager.LoadForCurrentFloor();
    }

    // ── Panel ─────────────────────────────────────────────────────────────────

    private static void TogglePanel()
    {
        if (_panel == null) return;
        if (_panel.Visible) { _panel.Visible = false; return; }
        Refresh();
        _panel.Visible = true;
    }

    private static void Refresh()
    {
        if (_list == null) return;
        foreach (var child in _list.GetChildren()) child.QueueFree();

        var checkpoints = CheckpointManager.LoadAll();

        if (checkpoints.Count == 0)
        {
            _list.AddChild(new Label { Text = "No checkpoints yet." });
            return;
        }

        foreach (var cp in checkpoints)
        {
            var btn = new Button { Text = $"Floor {cp.Floor}" };
            btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            var captured = cp;
            btn.Pressed += () =>
            {
                CheckpointManager.LoadCheckpoint(captured);
                if (_panel != null) _panel.Visible = false;
            };
            _list.AddChild(btn);
        }
    }
}
