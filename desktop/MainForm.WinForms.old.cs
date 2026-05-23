using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace CopyCreator;

internal sealed partial class MainForm : Form
{
    private const int ResizeGrip = 8;
    private const int WmNchittest = 0x0084;
    private const int Htleft = 10;
    private const int Htright = 11;
    private const int Httop = 12;
    private const int Httopleft = 13;
    private const int Httopright = 14;
    private const int Htbottom = 15;
    private const int Htbottomleft = 16;
    private const int Htbottomright = 17;

    private readonly DataStore _store = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly System.Windows.Forms.Timer _clipboardTimer = new() { Interval = 800 };
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly SmoothListView<ClipboardRecord> _clipboardList = new();
    private readonly SmoothListView<Phrase> _phraseList = new();
    private readonly SmoothListView<object> _groupList = new();
    private readonly Label _toast = new();
    private readonly System.Windows.Forms.Timer _toastTimer = new() { Interval = 1200 };
    private readonly List<Button> _filterButtons = [];
    private readonly TextBox _clipboardSearch = new();
    private readonly TextBox _phraseSearch = new();
    private readonly TabControl _tabs = new();
    private readonly Color _line = Color.FromArgb(31, 35, 36);
    private readonly Color _paper = Color.FromArgb(244, 241, 232);
    private readonly Color _surface = Color.FromArgb(255, 252, 242);
    private readonly Color _surfaceLow = Color.FromArgb(238, 244, 238);
    private readonly Color _surfaceHigh = Color.FromArgb(227, 239, 255);
    private readonly Color _ink = Color.FromArgb(25, 29, 30);
    private readonly Color _muted = Color.FromArgb(89, 96, 96);
    private readonly Color _navBlock = Color.FromArgb(173, 215, 204);
    private readonly Color _active = Color.FromArgb(73, 118, 255);
    private readonly Color _danger = Color.FromArgb(238, 91, 75);
    private readonly Color _sun = Color.FromArgb(255, 220, 82);
    private readonly Color _mint = Color.FromArgb(89, 214, 142);
    private readonly Color _coral = Color.FromArgb(255, 128, 109);
    private string _lastSignature = "";
    private int _clipboardFilterIndex;
    private bool _exiting;

    public MainForm()
    {
        Text = "Copy Creator";
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(760, 560);
        Size = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiFont(9F);
        BackColor = _paper;
        ForeColor = _ink;

        BuildLayout();
        BuildTray();
        BuildToast();
        RefreshClipboardList();
        RefreshPhraseGroups();
        RefreshPhraseList();

        _clipboardTimer.Tick += (_, _) => CaptureClipboardSafely();
        _clipboardTimer.Start();
        _store.Log("app_started");
    }

    /// <summary>
    /// 构建主界面布局，左侧为窄导航，右侧为内容页。
    /// </summary>
    private void BuildLayout()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _paper
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);
        shell.Controls.Add(CreateTitleBar(), 0, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _paper
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(root, 0, 1);

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(0, 12, 0, 8),
            BackColor = _navBlock,
            WrapContents = false
        };
        root.Controls.Add(nav, 0, 0);
        root.Controls.Add(_tabs, 1, 0);

        var logo = new Label
        {
            Text = "COPY\nCREATOR",
            Width = 64,
            Height = 72,
            ForeColor = _ink,
            Font = UiFont(10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 12)
        };
        nav.Controls.Add(logo);

        AddNavButton(nav, "剪", 0);
        AddNavButton(nav, "短", 1);
        AddNavButton(nav, "译", 2);
        AddNavButton(nav, "设", 3);
        AddNavButton(nav, "快", -1);

        _tabs.Appearance = TabAppearance.FlatButtons;
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.ItemSize = new Size(0, 1);
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.DrawItem += (_, _) => { };
        _tabs.Dock = DockStyle.Fill;
        _tabs.Controls.Add(CreateClipboardPage());
        _tabs.Controls.Add(CreatePhrasePage());
        _tabs.Controls.Add(CreateTranslatePage());
        _tabs.Controls.Add(CreateSettingsPage());
    }

    /// <summary>
    /// 创建自绘标题栏，替代 Windows 原生蓝色标题栏，并保留窗口拖动与控制按钮。
    /// </summary>
    private Control CreateTitleBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _navBlock
        };
        bar.MouseDown += DragWindow;

        var title = new Label
        {
            Text = "  COPY CREATOR  /  LOCAL CLIPBOARD STATION",
            Dock = DockStyle.Fill,
            ForeColor = _surface,
            Font = UiFont(9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        title.MouseDown += DragWindow;
        bar.Controls.Add(title);

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 44,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _navBlock,
            Padding = new Padding(0)
        };
        bar.Controls.Add(controls);
        AddWindowButton(controls, "×", Close);

        return bar;
    }

    private void AddWindowButton(Control parent, string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Width = 44,
            Height = 34,
            Margin = new Padding(0),
            FlatStyle = FlatStyle.Flat,
            BackColor = _navBlock,
            ForeColor = _surface,
            Font = UiFont(10F, FontStyle.Bold)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = _danger;
        button.FlatAppearance.MouseDownBackColor = _coral;
        button.Click += (_, _) => action();
        parent.Controls.Add(button);
    }

    private void DragWindow(object? sender, MouseEventArgs eventArgs)
    {
        if (eventArgs.Button != MouseButtons.Left)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && e.Y <= 42)
        {
            DragWindow(this, e);
        }
    }

    private void AddNavButton(FlowLayoutPanel nav, string text, int tabIndex)
    {
        var button = new Button
        {
            Text = text,
            Width = 64,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = _navBlock,
            ForeColor = _ink,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0),
            Font = UiFont(9F, FontStyle.Bold)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = _sun;
        button.FlatAppearance.MouseDownBackColor = _active;
        button.Click += (_, _) =>
        {
            if (tabIndex >= 0)
            {
                _tabs.SelectedIndex = tabIndex;
            }
            else
            {
                ShowQuickPanel();
            }
        };
        nav.Controls.Add(button);
    }

    private TabPage CreateClipboardPage()
    {
        var page = CreatePage("剪贴板 Clipboard");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(0), BackColor = _paper };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);
        layout.Controls.Add(CreatePageHeader("剪贴板", "历史记录 · 搜索 · 分类 · 置顶"), 0, 0);

        var tools = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12, 6, 12, 6) };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        _clipboardSearch.PlaceholderText = "搜索剪贴板...";
        StyleTextBox(_clipboardSearch);
        _clipboardSearch.Dock = DockStyle.Fill;
        _clipboardSearch.TextChanged += (_, _) => RefreshClipboardList();
        tools.Controls.Add(_clipboardSearch, 0, 0);
        tools.Controls.Add(CreateFilterButtons(), 1, 0);
        layout.Controls.Add(tools, 0, 1);

        _clipboardList.Dock = DockStyle.Fill;
        _clipboardList.BackColor = _surface;
        _clipboardList.ItemHeight = _store.Data.Settings.Density == "compact" ? 52 : 76;
        _clipboardList.ShowScrollBar = true;
        _clipboardList.ScrollTrackColor = _surfaceLow;
        _clipboardList.ScrollThumbColor = _line;
        _clipboardList.DrawItemContent = DrawClipboardItem;
        _clipboardList.ItemClicked += (_, _) => CopySelectedClipboard();
        _clipboardList.HandleItemAction = HandleClipboardListAction;
        layout.Controls.Add(CreateFramedHost(_clipboardList), 0, 2);
        return page;
    }

    private TabPage CreatePhrasePage()
    {
        var page = CreatePage("快捷短语 Quick Phrases");
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(0), BackColor = _paper };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(shell);
        shell.Controls.Add(CreatePageHeader("快捷短语", "分组管理 · 一键复制 · AI / 客服 / 网址"), 0, 0);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(layout, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        _groupList.Dock = DockStyle.Fill;
        _groupList.BackColor = _surfaceLow;
        _groupList.ItemHeight = 40;
        _groupList.ShowScrollBar = false;
        _groupList.DrawItemContent = DrawGroupItem;
        _groupList.ItemClicked += (_, _) => RefreshPhraseList();
        left.Controls.Add(CreateFramedHost(_groupList), 0, 0);
        var groupButtons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        AddSmallButton(groupButtons, "新建分组", AddGroup);
        AddSmallButton(groupButtons, "删除分组", DeleteGroup);
        left.Controls.Add(groupButtons, 0, 1);
        layout.Controls.Add(left, 0, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        _phraseSearch.PlaceholderText = "搜索短语...";
        _phraseSearch.Dock = DockStyle.Fill;
        StyleTextBox(_phraseSearch);
        _phraseSearch.TextChanged += (_, _) => RefreshPhraseList();
        _phraseList.Dock = DockStyle.Fill;
        _phraseList.BackColor = _surfaceLow;
        _phraseList.ItemHeight = _store.Data.Settings.Density == "compact" ? 46 : 62;
        _phraseList.ShowScrollBar = false;
        _phraseList.DrawItemContent = DrawPhraseItem;
        _phraseList.ItemClicked += (_, _) => CopySelectedPhrase();
        right.Controls.Add(_phraseSearch, 0, 0);
        right.Controls.Add(CreateFramedHost(_phraseList), 0, 1);
        var phraseButtons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        AddSmallButton(phraseButtons, "新建", AddPhrase);
        AddSmallButton(phraseButtons, "编辑", EditPhrase);
        AddSmallButton(phraseButtons, "删除", DeletePhrase);
        AddSmallButton(phraseButtons, "复制", CopySelectedPhrase);
        right.Controls.Add(phraseButtons, 0, 2);
        layout.Controls.Add(right, 1, 0);
        return page;
    }

    private TabPage CreateTranslatePage()
    {
        var page = CreatePage("翻译 Translate");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(12), BackColor = _paper };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        page.Controls.Add(layout);
        layout.Controls.Add(CreatePageHeader("翻译", "手动输入 · OpenAI 兼容接口 · 结果复制"), 0, 0);

        var source = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.None, PlaceholderText = "输入要翻译的内容" };
        StyleTextBox(source);
        var target = new TextBox { Dock = DockStyle.Fill, Text = _store.Data.Settings.DefaultTargetLanguage, PlaceholderText = "English / Chinese / Japanese..." };
        StyleTextBox(target);
        var result = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.None, ReadOnly = true };
        StyleTextBox(result);
        var translate = new Button { Text = "翻译", Dock = DockStyle.Right, Width = 110, FlatStyle = FlatStyle.Flat };
        StyleButton(translate);
        translate.Click += async (_, _) =>
        {
            result.Text = await TranslateAsync(source.Text, target.Text);
        };
        var copy = new Button { Text = "复制结果", Dock = DockStyle.Right, Width = 110, FlatStyle = FlatStyle.Flat };
        StyleButton(copy);
        copy.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                Clipboard.SetText(result.Text);
            }
        };

        layout.Controls.Add(source, 0, 1);
        layout.Controls.Add(target, 0, 2);
        layout.Controls.Add(result, 0, 3);
        var buttons = new Panel { Dock = DockStyle.Fill };
        buttons.Controls.Add(copy);
        buttons.Controls.Add(translate);
        layout.Controls.Add(buttons, 0, 4);
        return page;
    }

    private TabPage CreateSettingsPage()
    {
        var page = CreatePage("设置 Settings");
        var settings = _store.Data.Settings;
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(12), BackColor = _paper };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(shell);
        shell.Controls.Add(CreatePageHeader("设置", "本地存储 · 缓存上限 · 翻译接口 · 调试"), 0, 0);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        shell.Controls.Add(layout, 0, 1);

        layout.Controls.Add(CreateSettingsGroup("常规", [
            CreateChoice("密度", settings.Density, ["compact", "normal", "relaxed"], value => { settings.Density = value; ApplyDensity(); }),
            CreateCheck("开机启动", settings.StartOnBoot, value => { settings.StartOnBoot = value; ApplyStartOnBoot(value); }),
            CreateCheck("关闭隐藏到托盘", settings.HideOnClose, value => settings.HideOnClose = value)
        ]), 0, 0);
        layout.Controls.Add(CreateSettingsGroup("剪贴板", [
            CreateNumber("保存天数", settings.SaveDays, value => settings.SaveDays = value),
            CreateNumber("最大条数", settings.MaxItems, value => settings.MaxItems = value),
            CreateCheck("记录文本", settings.RecordText, value => settings.RecordText = value),
            CreateCheck("记录链接", settings.RecordLinks, value => settings.RecordLinks = value),
            CreateCheck("记录图片", settings.RecordImages, value => settings.RecordImages = value),
            CreateCheck("记录文件", settings.RecordFiles, value => settings.RecordFiles = value),
            CreateCheck("敏感内容检测", settings.SensitiveDetection, value => settings.SensitiveDetection = value)
        ]), 1, 0);
        layout.Controls.Add(CreateSettingsGroup("缓存", [
            CreateNumber("单文件 MB", settings.FileMaxMb, value => settings.FileMaxMb = value),
            CreateNumber("总缓存 GB", settings.CacheMaxGb, value => settings.CacheMaxGb = value),
            CreateAction("清理失效缓存", CleanupInvalidCache)
        ]), 0, 1);
        layout.Controls.Add(CreateSettingsGroup("快捷面板", [
            CreateCheck("显示剪贴板", settings.QuickShowClipboard, value => settings.QuickShowClipboard = value),
            CreateCheck("显示短语", settings.QuickShowPhrases, value => settings.QuickShowPhrases = value)
        ]), 1, 1);
        layout.Controls.Add(CreateSettingsGroup("翻译", [
            CreateText("API 地址", settings.ApiBaseUrl, value => settings.ApiBaseUrl = value),
            CreateText("API Key", settings.ApiKey, value => settings.ApiKey = value),
            CreateText("模型名称", settings.ModelName, value => settings.ModelName = value),
            CreateText("默认目标语言", settings.DefaultTargetLanguage, value => settings.DefaultTargetLanguage = value)
        ]), 0, 2);
        layout.Controls.Add(CreateSettingsGroup("高级调试", [
            CreateAction("打开数据目录", () => Process.Start("explorer.exe", _store.DataDir)),
            CreateAction("打开日志目录", () => Process.Start("explorer.exe", _store.LogDir)),
            CreateAction("数据库健康检查", () => ShowToast("JSON STORE OK")),
            CreateAction("保存设置", SaveSettings),
            CreateAction("退出程序", ExitApplication)
        ]), 1, 2);
        return page;
    }

    private TabPage CreatePage(string title) => new(title) { BackColor = _paper, Padding = new Padding(0) };

    private Control CreatePageHeader(string title, string subtitle)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = _active, Padding = new Padding(16, 0, 16, 0) };
        panel.Paint += (_, eventArgs) => ControlPaint.DrawBorder(eventArgs.Graphics, panel.ClientRectangle, _line, ButtonBorderStyle.Solid);
        panel.Controls.Add(new Label
        {
            Text = title.ToUpperInvariant(),
            Dock = DockStyle.Left,
            Width = 190,
            BackColor = _active,
            ForeColor = Color.White,
            Font = UiFont(18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
        panel.Controls.Add(new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            BackColor = _active,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            Font = UiFont(8.5F, FontStyle.Bold)
        });
        return panel;
    }

    private Control CreateFramedHost(Control child)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = _surface, Padding = new Padding(1), Margin = new Padding(0, 0, 0, 0) };
        panel.Paint += (_, eventArgs) => ControlPaint.DrawBorder(eventArgs.Graphics, panel.ClientRectangle, _line, ButtonBorderStyle.Solid);
        panel.Controls.Add(child);
        return panel;
    }

    private Control CreateFilterButtons()
    {
        var labels = new[] { "ALL", "TEXT", "IMAGE", "LINK", "FILE" };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = labels.Length, BackColor = _paper };
        _filterButtons.Clear();
        for (var index = 0; index < labels.Length; index++)
        {
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            var captured = index;
            var button = new Button
            {
                Text = labels[index],
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = captured == _clipboardFilterIndex ? _ink : _surface,
                ForeColor = captured == _clipboardFilterIndex ? Color.White : _muted,
                Font = UiFont(8.5F, FontStyle.Bold),
                Margin = new Padding(index == 0 ? 0 : 4, 0, 0, 0)
            };
            button.FlatAppearance.BorderColor = _line;
            button.Click += (_, _) =>
            {
                _clipboardFilterIndex = captured;
                UpdateFilterButtons();
                RefreshClipboardList();
            };
            _filterButtons.Add(button);
            panel.Controls.Add(button, index, 0);
        }

        return panel;
    }

    private void UpdateFilterButtons()
    {
        for (var index = 0; index < _filterButtons.Count; index++)
        {
            var active = index == _clipboardFilterIndex;
            _filterButtons[index].BackColor = active ? _ink : _surface;
            _filterButtons[index].ForeColor = active ? Color.White : _muted;
        }
    }

    private void AddSmallButton(Control parent, string text, Action action)
    {
        var button = new Button { Text = text, Width = 88, Height = 34, FlatStyle = FlatStyle.Flat };
        StyleButton(button);
        button.Click += (_, _) => action();
        parent.Controls.Add(button);
    }

    /// <summary>
    /// 构建设置分组区域，采用固定边框和直角样式。
    /// </summary>
    private GroupBox CreateSettingsGroup(string title, Control[] controls)
    {
        var box = new GroupBox { Text = title, Dock = DockStyle.Top, Height = 230, Padding = new Padding(12), ForeColor = _ink, BackColor = _surface };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        foreach (var control in controls)
        {
            stack.Controls.Add(control);
        }

        box.Controls.Add(stack);
        return box;
    }

    private Control CreateCheck(string text, bool value, Action<bool> onChange)
    {
        var enabled = value;
        var button = new Button
        {
            Text = $"{(enabled ? "[X]" : "[ ]")} {text}",
            Width = 260,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
                Font = UiFont(9F, FontStyle.Bold)
        };
        StyleButton(button);
        button.Click += (_, _) =>
        {
            enabled = !enabled;
            button.Text = $"{(enabled ? "[X]" : "[ ]")} {text}";
            button.ForeColor = enabled ? _active : _muted;
            onChange(enabled);
        };
        button.ForeColor = enabled ? _active : _muted;
        return button;
    }

    private Control CreateNumber(string text, int value, Action<int> onChange)
    {
        var panel = new Panel { Width = 280, Height = 32, BackColor = _surface };
        var label = new Label { Text = text, Width = 120, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, ForeColor = _muted, BackColor = _surface };
        var input = new TextBox { Text = value.ToString(), Dock = DockStyle.Fill };
        StyleTextBox(input);
        input.TextChanged += (_, _) =>
        {
            if (int.TryParse(input.Text, out var next))
            {
                onChange(Math.Clamp(next, 0, 100000));
            }
        };
        panel.Controls.Add(input);
        panel.Controls.Add(label);
        return panel;
    }

    private Control CreateText(string text, string value, Action<string> onChange)
    {
        var panel = new Panel { Width = 320, Height = 32, BackColor = _surface };
        var label = new Label { Text = text, Width = 100, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, ForeColor = _muted, BackColor = _surface };
        var input = new TextBox { Text = value, Dock = DockStyle.Fill };
        StyleTextBox(input);
        input.TextChanged += (_, _) => onChange(input.Text);
        panel.Controls.Add(input);
        panel.Controls.Add(label);
        return panel;
    }

    private Control CreateChoice(string text, string value, string[] values, Action<string> onChange)
    {
        var panel = new Panel { Width = 320, Height = 32, BackColor = _surface };
        var label = new Label { Text = text, Width = 80, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, ForeColor = _muted, BackColor = _surface };
        var choices = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = _surface };
        var buttons = new List<Button>();
        foreach (var item in values)
        {
            var captured = item;
            var button = new Button
            {
                Text = item.ToUpperInvariant(),
                Width = 72,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                Font = UiFont(8F, FontStyle.Bold),
                Margin = new Padding(0, 0, 4, 0)
            };
            StyleButton(button);
            buttons.Add(button);
            button.Click += (_, _) =>
            {
                foreach (var each in buttons)
                {
                    each.BackColor = _surface;
                    each.ForeColor = _muted;
                }

                button.BackColor = _active;
                button.ForeColor = Color.Black;
                onChange(captured);
            };
            if (item.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                button.BackColor = _active;
                button.ForeColor = Color.Black;
            }

            choices.Controls.Add(button);
        }

        panel.Controls.Add(choices);
        panel.Controls.Add(label);
        return panel;
    }

    private Control CreateAction(string text, Action action)
    {
        var button = new Button { Text = text, Width = 180, Height = 32, FlatStyle = FlatStyle.Flat };
        StyleButton(button);
        button.Click += (_, _) => action();
        return button;
    }

    private void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = _surface;
        textBox.ForeColor = _ink;
        textBox.Font = UiFont(9F);
    }

    private void StyleButton(Button button)
    {
        button.BackColor = _surface;
        button.ForeColor = _muted;
        button.FlatAppearance.BorderColor = _line;
        button.FlatAppearance.MouseOverBackColor = _sun;
        button.FlatAppearance.MouseDownBackColor = _active;
    }

    private void BuildTray()
    {
        _trayIcon.Text = "Copy Creator";
        _trayIcon.Icon = SystemIcons.Application;
        _trayIcon.Visible = true;
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    /// <summary>
    /// 创建非阻塞复制提示，短暂显示后自动隐藏。
    /// </summary>
    private void BuildToast()
    {
        _toast.Visible = false;
        _toast.AutoSize = false;
        _toast.Width = 118;
        _toast.Height = 34;
        _toast.TextAlign = ContentAlignment.MiddleCenter;
        _toast.BackColor = _ink;
        _toast.ForeColor = Color.White;
        _toast.Font = UiFont(9F, FontStyle.Bold);
        _toast.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(_toast);
        _toast.BringToFront();
        Resize += (_, _) => PositionToast();
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            _toast.Visible = false;
        };
    }

    private void ShowToast(string message)
    {
        _toast.Text = message;
        PositionToast();
        _toast.Visible = true;
        _toast.BringToFront();
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void PositionToast()
    {
        _toast.Left = ClientSize.Width - _toast.Width - 22;
        _toast.Top = ClientSize.Height - _toast.Height - 22;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exiting && _store.Data.Settings.HideOnClose)
        {
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(1000, "Copy Creator", "已隐藏到托盘，剪贴板记录继续运行。", ToolTipIcon.Info);
            return;
        }

        SaveSettingsSilently();
        _trayIcon.Visible = false;
        base.OnFormClosing(e);
    }

    /// <summary>
    /// 为无边框窗口补回边缘缩放命中区域，避免自绘标题栏后窗口不能调整大小。
    /// </summary>
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (message.Msg != WmNchittest || WindowState == FormWindowState.Maximized)
        {
            return;
        }

        var cursor = PointToClient(Cursor.Position);
        var left = cursor.X <= ResizeGrip;
        var right = cursor.X >= ClientSize.Width - ResizeGrip;
        var top = cursor.Y <= ResizeGrip;
        var bottom = cursor.Y >= ClientSize.Height - ResizeGrip;

        if (left && top)
        {
            message.Result = Httopleft;
        }
        else if (right && top)
        {
            message.Result = Httopright;
        }
        else if (left && bottom)
        {
            message.Result = Htbottomleft;
        }
        else if (right && bottom)
        {
            message.Result = Htbottomright;
        }
        else if (left)
        {
            message.Result = Htleft;
        }
        else if (right)
        {
            message.Result = Htright;
        }
        else if (top)
        {
            message.Result = Httop;
        }
        else if (bottom)
        {
            message.Result = Htbottom;
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    /// <summary>
    /// 安全读取剪贴板变化，支持文本、链接、图片和文件列表。
    /// </summary>
    private void CaptureClipboardSafely()
    {
        try
        {
            var record = TryReadClipboardRecord();
            if (record is null)
            {
                return;
            }

            var signature = $"{record.Kind}:{record.ContentHash}";
            if (signature == _lastSignature)
            {
                return;
            }

            _lastSignature = signature;
            AddOrUpdateClipboardRecord(record);
        }
        catch (Exception ex)
        {
            _store.Log($"clipboard_error type={ex.GetType().Name} message={ex.Message}");
        }
    }

    private ClipboardRecord? TryReadClipboardRecord()
    {
        var settings = _store.Data.Settings;
        if (Clipboard.ContainsFileDropList() && settings.RecordFiles)
        {
            var files = Clipboard.GetFileDropList().Cast<string>().ToList();
            if (files.Count == 0)
            {
                return null;
            }

            return CreateFileRecord(files);
        }

        if (Clipboard.ContainsImage() && settings.RecordImages)
        {
            return CreateImageRecord();
        }

        if (Clipboard.ContainsText() && (settings.RecordText || settings.RecordLinks))
        {
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (settings.SensitiveDetection && LooksSensitive(text))
            {
                _store.Log("clipboard_skipped reason=sensitive_text");
                return null;
            }

            var isLink = Uri.TryCreate(text.Trim(), UriKind.Absolute, out _);
            if (isLink && !settings.RecordLinks)
            {
                return null;
            }

            if (!isLink && !settings.RecordText)
            {
                return null;
            }

            return new ClipboardRecord
            {
                Kind = isLink ? ClipboardKind.Link : ClipboardKind.Text,
                Title = isLink ? "链接" : FirstLine(text, 42),
                Content = text,
                ContentHash = HashBytes(Encoding.UTF8.GetBytes(text)),
                Cached = true,
                SizeBytes = Encoding.UTF8.GetByteCount(text)
            };
        }

        return null;
    }

    private ClipboardRecord CreateImageRecord()
    {
        using var image = Clipboard.GetImage();
        if (image is null)
        {
            throw new InvalidOperationException("剪贴板图片读取失败。");
        }

        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);
        var bytes = stream.ToArray();
        var hash = HashBytes(bytes);
        var original = Path.Combine(_store.ImageCacheDir, $"{hash}.png");
        var thumb = Path.Combine(_store.ImageCacheDir, $"{hash}.thumb.png");
        if (!File.Exists(original))
        {
            File.WriteAllBytes(original, bytes);
        }
        using var preview = image.GetThumbnailImage(96, 96, null, IntPtr.Zero);
        if (!File.Exists(thumb))
        {
            preview.Save(thumb, ImageFormat.Png);
        }
        return new ClipboardRecord
        {
            Kind = ClipboardKind.Image,
            Title = "剪贴板图片",
            Content = hash,
            ContentHash = hash,
            PreviewPath = thumb,
            CachedPath = original,
            Cached = true,
            SizeBytes = new FileInfo(original).Length
        };
    }

    private ClipboardRecord CreateFileRecord(List<string> files)
    {
        var settings = _store.Data.Settings;
        var id = Guid.NewGuid();
        var first = files[0];
        var totalSize = files.Where(File.Exists).Sum(path => new FileInfo(path).Length);
        var maxBytes = settings.FileMaxMb * 1024L * 1024L;
        string? cachedPath = null;
        var cached = false;
        if (files.Count == 1 && File.Exists(first) && totalSize <= maxBytes)
        {
            cachedPath = Path.Combine(_store.FileCacheDir, $"{id}_{Path.GetFileName(first)}");
            File.Copy(first, cachedPath, true);
            cached = true;
        }

        return new ClipboardRecord
        {
            Id = id,
            Kind = ClipboardKind.File,
            Title = files.Count == 1 ? Path.GetFileName(first) : $"{files.Count} 个文件",
            Content = string.Join(Environment.NewLine, files),
            ContentHash = HashBytes(Encoding.UTF8.GetBytes(string.Join("|", files))),
            OriginalPath = first,
            CachedPath = cachedPath,
            Cached = cached,
            SizeBytes = totalSize
        };
    }

    private void AddOrUpdateClipboardRecord(ClipboardRecord record)
    {
        var hash = string.IsNullOrWhiteSpace(record.ContentHash)
            ? HashBytes(Encoding.UTF8.GetBytes($"{record.Kind}:{record.Content}"))
            : record.ContentHash;
        record.ContentHash = hash;
        var existing = _store.Data.ClipboardItems.FirstOrDefault(item => item.Kind == record.Kind && item.ContentHash == hash);
        if (existing is not null)
        {
            existing.UpdatedAt = DateTime.Now;
            existing.Cached = existing.Cached || record.Cached;
            existing.CachedPath ??= record.CachedPath;
            existing.PreviewPath ??= record.PreviewPath;
        }
        else
        {
            record.CreatedAt = DateTime.Now;
            record.UpdatedAt = DateTime.Now;
            _store.Data.ClipboardItems.Insert(0, record);
        }

        CleanupHistory();
        _store.Save();
        RefreshClipboardList();
    }

    private void CleanupHistory()
    {
        var settings = _store.Data.Settings;
        var cutoff = DateTime.Now.AddDays(-settings.SaveDays);
        _store.Data.ClipboardItems = _store.Data.ClipboardItems
            .Where(item => item.Pinned || item.UpdatedAt >= cutoff)
            .OrderByDescending(item => item.Pinned)
            .ThenByDescending(item => item.UpdatedAt)
            .Take(settings.MaxItems)
            .ToList();
    }

    private void RefreshClipboardList()
    {
        var keyword = _clipboardSearch.Text.Trim();
        var filter = _clipboardFilterIndex;
        var items = _store.Data.ClipboardItems.AsEnumerable();
        items = filter switch
        {
            1 => items.Where(item => item.Kind == ClipboardKind.Text),
            2 => items.Where(item => item.Kind == ClipboardKind.Image),
            3 => items.Where(item => item.Kind == ClipboardKind.Link),
            4 => items.Where(item => item.Kind == ClipboardKind.File),
            _ => items
        };
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            items = items.Where(item => $"{item.Title} {item.Content} {item.OriginalPath}".Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        _clipboardList.SetItems(items.OrderByDescending(item => item.Pinned).ThenByDescending(item => item.UpdatedAt));
    }

    private void DrawClipboardItem(Graphics graphics, Rectangle bounds, ClipboardRecord record, bool selected)
    {
        using var rowBg = new SolidBrush(selected ? _surfaceHigh : _surface);
        graphics.FillRectangle(rowBg, bounds);
        using var separator = new Pen(_line);
        graphics.DrawLine(separator, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

        using var stripe = new SolidBrush(GetKindColor(record.Kind, record.Pinned || selected));
        graphics.FillRectangle(stripe, bounds.Left, bounds.Top, record.Pinned || selected ? 4 : 2, bounds.Height);
        using var titleFont = UiFont(9.5F, FontStyle.Bold);
        using var muted = new SolidBrush(_muted);
        using var ink = new SolidBrush(_ink);
        var title = $"{KindLabel(record.Kind).ToUpperInvariant()} · {(record.Pinned ? "PINNED · " : "")}{record.Title}";
        graphics.DrawString(title, titleFont, ink, bounds.Left + 16, bounds.Top + 8);
        var content = record.Kind == ClipboardKind.Image ? record.CachedPath ?? record.Content : record.Content;
        using var body = UiFont(9F);
        graphics.DrawString(FirstLine(content, 88), body, ink, bounds.Left + 16, bounds.Top + 30);
        graphics.DrawString($"{record.UpdatedAt:MM/dd HH:mm} · {SizeLabel(record.SizeBytes)} · {(record.Cached ? "已缓存" : "仅路径")}", body, muted, bounds.Left + 16, bounds.Bottom - 22);
        using var actionFont = UiFont(8.5F, FontStyle.Bold);
        graphics.DrawString(record.Pinned ? "UNPIN" : "PIN", actionFont, muted, bounds.Right - 92, bounds.Top + 10);
        graphics.DrawString("DEL", actionFont, new SolidBrush(_danger), bounds.Right - 48, bounds.Top + 10);
    }

    private void DrawPhraseItem(Graphics graphics, Rectangle bounds, Phrase phrase, bool selected)
    {
        using var rowBg = new SolidBrush(selected ? _surfaceHigh : _surface);
        using var ink = new SolidBrush(_ink);
        using var muted = new SolidBrush(_muted);
        using var titleFont = UiFont(9.5F, FontStyle.Bold);
        using var separator = new Pen(_line);
        graphics.FillRectangle(rowBg, bounds);
        graphics.DrawLine(separator, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        using var stripe = new SolidBrush(selected ? _active : _line);
        graphics.FillRectangle(stripe, bounds.Left, bounds.Top, selected ? 3 : 1, bounds.Height);
        using var body = UiFont(9F);
        graphics.DrawString(phrase.Title, titleFont, ink, bounds.Left + 16, bounds.Top + 8);
        graphics.DrawString(FirstLine(phrase.Content, 76), body, muted, bounds.Left + 16, bounds.Top + 30);
    }

    private void DrawGroupItem(Graphics graphics, Rectangle bounds, object item, bool selected)
    {
        var text = item is PhraseGroup group ? group.Name : "全部";
        using var background = new SolidBrush(selected ? _surfaceHigh : _surfaceLow);
        using var ink = new SolidBrush(selected ? _active : _muted);
        using var line = new Pen(_line);
        using var stripe = new SolidBrush(selected ? _active : _line);
        graphics.FillRectangle(background, bounds);
        graphics.FillRectangle(stripe, bounds.Left, bounds.Top, selected ? 3 : 1, bounds.Height);
        graphics.DrawLine(line, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        using var font = UiFont(9F, FontStyle.Bold);
        graphics.DrawString(text.ToUpperInvariant(), font, ink, bounds.Left + 14, bounds.Top + 12);
    }

    private void RefreshPhraseGroups()
    {
        var groups = new List<object> { "全部" };
        groups.AddRange(_store.Data.PhraseGroups.OrderBy(group => group.SortOrder));
        _groupList.SetItems(groups);
    }

    private void RefreshPhraseList()
    {
        var keyword = _phraseSearch.Text.Trim();
        var selectedGroup = _groupList.SelectedItem as PhraseGroup;
        var phrases = _store.Data.Phrases.AsEnumerable();
        if (selectedGroup is not null)
        {
            phrases = phrases.Where(phrase => phrase.GroupId == selectedGroup.Id);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            phrases = phrases.Where(phrase => $"{phrase.Title} {phrase.Content}".Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        _phraseList.SetItems(phrases.OrderByDescending(phrase => phrase.UpdatedAt));
    }

    private void CopySelectedClipboard()
    {
        if (_clipboardList.SelectedItem is not ClipboardRecord record)
        {
            return;
        }

        if (record.Kind == ClipboardKind.Image && record.CachedPath is not null && File.Exists(record.CachedPath))
        {
            using var image = Image.FromFile(record.CachedPath);
            Clipboard.SetImage(new Bitmap(image));
            ShowToast("图片已复制");
        }
        else if (record.Kind == ClipboardKind.File)
        {
            var paths = new System.Collections.Specialized.StringCollection();
            if (record.Cached && record.CachedPath is not null && File.Exists(record.CachedPath))
            {
                paths.Add(record.CachedPath);
            }
            else if (record.OriginalPath is not null && File.Exists(record.OriginalPath))
            {
                paths.Add(record.OriginalPath);
            }
            else
            {
                ShowToast("FILE CACHE LOST");
                return;
            }

            Clipboard.SetFileDropList(paths);
            ShowToast("文件已复制");
        }
        else
        {
            Clipboard.SetText(record.Content);
            ShowToast("已复制");
        }
    }

    private bool HandleClipboardListAction(ClipboardRecord record, Point location, Rectangle bounds)
    {
        if (location.X >= bounds.Right - 96 && location.X < bounds.Right - 54)
        {
            record.Pinned = !record.Pinned;
            _store.Save();
            RefreshClipboardList();
            return true;
        }
        else if (location.X >= bounds.Right - 54)
        {
            DeleteCache(record);
            _store.Data.ClipboardItems.Remove(record);
            _store.Save();
            RefreshClipboardList();
            return true;
        }

        return false;
    }

    private void ToggleSelectedPin()
    {
        if (_clipboardList.SelectedItem is ClipboardRecord record)
        {
            record.Pinned = !record.Pinned;
            _store.Save();
            RefreshClipboardList();
        }
    }

    private void DeleteSelectedClipboard()
    {
        if (_clipboardList.SelectedItem is ClipboardRecord record)
        {
            DeleteCache(record);
            _store.Data.ClipboardItems.Remove(record);
            _store.Save();
            RefreshClipboardList();
        }
    }

    private void CopySelectedPhrase()
    {
        if (_phraseList.SelectedItem is Phrase phrase)
        {
            Clipboard.SetText(phrase.Content);
            ShowToast("短语已复制");
        }
    }

    private void AddGroup()
    {
        var name = Prompt("新建分组", "分组名称");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        _store.Data.PhraseGroups.Add(new PhraseGroup { Name = name, SortOrder = _store.Data.PhraseGroups.Count + 1 });
        _store.Save();
        RefreshPhraseGroups();
    }

    private void DeleteGroup()
    {
        if (_groupList.SelectedItem is not PhraseGroup group)
        {
            return;
        }

        _store.Data.PhraseGroups.Remove(group);
        _store.Data.Phrases.RemoveAll(phrase => phrase.GroupId == group.Id);
        _store.Save();
        RefreshPhraseGroups();
        RefreshPhraseList();
    }

    private void AddPhrase()
    {
        var group = _groupList.SelectedItem as PhraseGroup ?? _store.Data.PhraseGroups.FirstOrDefault();
        if (group is null)
        {
            return;
        }

        var title = Prompt("新建短语", "标题");
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var content = Prompt("新建短语", "内容");
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        _store.Data.Phrases.Add(new Phrase { GroupId = group.Id, Title = title, Content = content, UpdatedAt = DateTime.Now });
        _store.Save();
        RefreshPhraseList();
    }

    private void EditPhrase()
    {
        if (_phraseList.SelectedItem is not Phrase phrase)
        {
            return;
        }

        var title = Prompt("编辑短语", "标题", phrase.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var content = Prompt("编辑短语", "内容", phrase.Content);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        phrase.Title = title;
        phrase.Content = content;
        phrase.UpdatedAt = DateTime.Now;
        _store.Save();
        RefreshPhraseList();
    }

    private void DeletePhrase()
    {
        if (_phraseList.SelectedItem is Phrase phrase)
        {
            _store.Data.Phrases.Remove(phrase);
            _store.Save();
            RefreshPhraseList();
        }
    }

    private async Task<string> TranslateAsync(string source, string targetLanguage)
    {
        var settings = _store.Data.Settings;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return "还没有配置 API Key，请先到设置里填写。";
        }

        try
        {
            var endpoint = settings.ApiBaseUrl.TrimEnd('/') + "/v1/chat/completions";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            var body = new
            {
                model = settings.ModelName,
                messages = new[]
                {
                    new { role = "system", content = $"Translate the user text into {targetLanguage}. Return translation only." },
                    new { role = "user", content = source }
                }
            };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _store.Log($"translate_failed status={(int)response.StatusCode}");
                return $"翻译失败：接口返回 {(int)response.StatusCode}";
            }

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception ex)
        {
            _store.Log($"translate_error type={ex.GetType().Name} message={ex.Message}");
            return "翻译失败，请检查网络、API 地址或模型名称。";
        }
    }

    private void ShowQuickPanel()
    {
        using var form = new QuickPanelForm(_store.Data, CopyQuickClipboard, CopyQuickPhrase);
        form.ShowDialog(this);
    }

    private void CopyQuickClipboard(ClipboardRecord record)
    {
        var old = _clipboardList.SelectedItem;
        _clipboardList.SelectItem(record);
        CopySelectedClipboard();
        if (old is not null)
        {
            _clipboardList.SelectItem(old);
        }
    }

    private static void CopyQuickPhrase(Phrase phrase)
    {
        Clipboard.SetText(phrase.Content);
    }

    private void SaveSettings()
    {
        SaveSettingsSilently();
        ShowToast("SETTINGS SAVED");
    }

    private void SaveSettingsSilently()
    {
        _store.Save();
    }

    private void ExitApplication()
    {
        _exiting = true;
        _trayIcon.Visible = false;
        SaveSettingsSilently();
        Application.Exit();
    }

    private void CleanupInvalidCache()
    {
        foreach (var record in _store.Data.ClipboardItems.ToList())
        {
            if (!record.Pinned && record.CachedPath is not null && !File.Exists(record.CachedPath))
            {
                record.Cached = false;
            }
        }

        _store.Save();
        RefreshClipboardList();
    }

    private void DeleteCache(ClipboardRecord record)
    {
        TryDelete(record.PreviewPath);
        TryDelete(record.CachedPath);
    }

    private static void TryDelete(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void ApplyTheme()
    {
        var dark = _store.Data.Settings.Theme == "dark";
        BackColor = dark ? Color.FromArgb(24, 24, 24) : _paper;
        ForeColor = dark ? Color.WhiteSmoke : _ink;
    }

    private void ApplyDensity()
    {
        _clipboardList.ItemHeight = _store.Data.Settings.Density == "compact" ? 52 : _store.Data.Settings.Density == "relaxed" ? 88 : 76;
        _phraseList.ItemHeight = _store.Data.Settings.Density == "compact" ? 46 : _store.Data.Settings.Density == "relaxed" ? 74 : 62;
        _store.Save();
        RefreshClipboardList();
        RefreshPhraseList();
    }

    private void ApplyStartOnBoot(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null)
            {
                ShowToast("STARTUP KEY LOST");
                return;
            }

            if (enabled)
            {
                key.SetValue("CopyCreator", $"\"{Application.ExecutablePath}\"");
                ShowToast("STARTUP ON");
            }
            else
            {
                key.DeleteValue("CopyCreator", false);
                ShowToast("STARTUP OFF");
            }
        }
        catch (Exception ex)
        {
            _store.Log($"startup_error type={ex.GetType().Name} message={ex.Message}");
            ShowToast("STARTUP FAILED");
        }
    }

    private static bool LooksSensitive(string text)
    {
        return Regex.IsMatch(text, "(api[_-]?key|secret|token|password|sk-[a-zA-Z0-9])", RegexOptions.IgnoreCase)
            || text.Length is > 24 and < 240 && Regex.IsMatch(text, "^[A-Za-z0-9_\\-\\.]{24,}$");
    }

    private string Prompt(string title, string label, string value = "")
    {
        using var form = new Form
        {
            Text = title,
            Width = 420,
            Height = 170,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.None,
            BackColor = _paper,
            ForeColor = _ink,
            Font = UiFont(9F)
        };
        form.Paint += (_, eventArgs) => ControlPaint.DrawBorder(eventArgs.Graphics, form.ClientRectangle, _line, ButtonBorderStyle.Solid);
        var titleControl = new Label
        {
            Text = title.ToUpperInvariant(),
            Left = 14,
            Top = 10,
            Width = 260,
            Height = 24,
            ForeColor = _active,
            BackColor = _paper,
            Font = UiFont(10F, FontStyle.Bold)
        };
        titleControl.MouseDown += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(form.Handle, 0xA1, 0x2, 0);
            }
        };
        var labelControl = new Label { Text = label.ToUpperInvariant(), Left = 16, Top = 42, Width = 360, ForeColor = _muted, BackColor = _paper, Font = UiFont(8.5F) };
        var input = new TextBox { Left = 16, Top = 66, Width = 388, Text = value };
        input.BorderStyle = BorderStyle.FixedSingle;
        input.BackColor = _surfaceLow;
        input.ForeColor = _ink;
        input.Font = UiFont(9F);
        var ok = new Button { Text = "SAVE", Left = 244, Width = 76, Top = 112, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "CLOSE", Left = 328, Width = 76, Top = 112, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
        StyleButton(ok);
        StyleButton(cancel);
        form.Controls.Add(titleControl);
        form.Controls.AddRange([labelControl, input, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK ? input.Text : "";
    }

    private static string FirstLine(string value, int maxLength)
    {
        var line = value.ReplaceLineEndings(" ").Trim();
        return line.Length > maxLength ? line[..maxLength] + "..." : line;
    }

    private static string KindLabel(ClipboardKind kind) => kind switch
    {
        ClipboardKind.Link => "链接",
        ClipboardKind.Image => "图片",
        ClipboardKind.File => "文件",
        _ => "文本"
    };

    private Color GetKindColor(ClipboardKind kind, bool active) => kind switch
    {
        ClipboardKind.Link => active ? Color.FromArgb(255, 199, 82) : Color.FromArgb(166, 118, 36),
        ClipboardKind.Image => active ? Color.FromArgb(82, 232, 149) : Color.FromArgb(39, 142, 90),
        ClipboardKind.File => active ? Color.FromArgb(184, 144, 255) : Color.FromArgb(112, 83, 176),
        _ => active ? Color.FromArgb(118, 176, 255) : Color.FromArgb(54, 105, 178)
    };

    private static string SizeLabel(long size)
    {
        if (size > 1024 * 1024)
        {
            return $"{size / 1024d / 1024d:F1} MB";
        }

        return size > 1024 ? $"{size / 1024d:F1} KB" : $"{size} B";
    }

    private static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static Font UiFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("MiSans", size, style, GraphicsUnit.Point);
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr SendMessage(IntPtr hwnd, int msg, int wparam, int lparam);
}
