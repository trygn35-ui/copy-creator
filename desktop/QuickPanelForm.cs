namespace CopyCreator;

internal sealed class QuickPanelForm : Form
{
    private readonly AppData _data;
    private readonly Action<ClipboardRecord> _copyClipboard;
    private readonly Action<Phrase> _copyPhrase;
    private readonly SmoothListView<object> _list = new();
    private readonly Color _ink = Color.FromArgb(229, 225, 228);
    private readonly Color _muted = Color.FromArgb(196, 199, 200);
    private readonly Color _paper = Color.FromArgb(19, 19, 21);
    private readonly Color _surface = Color.FromArgb(28, 27, 29);
    private readonly Color _surfaceLow = Color.FromArgb(14, 14, 16);
    private readonly Color _surfaceHigh = Color.FromArgb(42, 42, 44);
    private readonly Color _line = Color.FromArgb(68, 71, 72);
    private readonly Color _active = Color.White;
    private bool _showingClipboard;

    public QuickPanelForm(AppData data, Action<ClipboardRecord> copyClipboard, Action<Phrase> copyPhrase)
    {
        _data = data;
        _copyClipboard = copyClipboard;
        _copyPhrase = copyPhrase;
        _showingClipboard = data.Settings.QuickShowClipboard;
        Text = "快捷面板";
        Width = 400;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = _paper;
        ForeColor = _ink;
        Font = new Font("Bahnschrift", 9F);
        KeyPreview = true;
        BuildLayout();
        LoadItems();
        KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
        Deactivate += (_, _) => Close();
    }

    /// <summary>
    /// 构建双栏快捷面板，左侧切换模块，右侧点击后复制并关闭。
    /// </summary>
    private void BuildLayout()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = _paper };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = _surface };
        header.MouseDown += DragWindow;
        header.Paint += (_, eventArgs) => ControlPaint.DrawBorder(eventArgs.Graphics, header.ClientRectangle, _line, ButtonBorderStyle.Solid);
        header.Controls.Add(new Label
        {
            Text = "  COPY CREATOR",
            Dock = DockStyle.Fill,
            ForeColor = _active,
            Font = new Font("Bahnschrift SemiBold", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
        header.Controls.Add(new Label
        {
            Text = "ESC  ",
            Dock = DockStyle.Right,
            Width = 56,
            ForeColor = _muted,
            Font = new Font("Consolas", 9F),
            TextAlign = ContentAlignment.MiddleCenter
        });
        shell.Controls.Add(header, 0, 0);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0), BackColor = _paper };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(root, 0, 1);

        var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 16, 0, 0), BackColor = _surface };
        root.Controls.Add(left, 0, 0);
        if (_data.Settings.QuickShowClipboard)
        {
            AddTab(left, "剪贴板", true);
        }

        if (_data.Settings.QuickShowPhrases)
        {
            AddTab(left, "快捷短语", false);
        }

        _list.Dock = DockStyle.Fill;
        _list.BackColor = _surfaceLow;
        _list.ItemHeight = 58;
        _list.ShowScrollBar = false;
        _list.DrawItemContent = DrawQuickItem;
        _list.ItemClicked += (_, _) => CopySelected();
        var host = new Panel { Dock = DockStyle.Fill, BackColor = _surfaceLow, Padding = new Padding(1) };
        host.Paint += (_, eventArgs) => ControlPaint.DrawBorder(eventArgs.Graphics, host.ClientRectangle, _line, ButtonBorderStyle.Solid);
        host.Controls.Add(_list);
        root.Controls.Add(host, 1, 0);
    }

    private void AddTab(Control parent, string text, bool clipboard)
    {
        var button = new Button
        {
            Text = text,
            Width = 64,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            BackColor = _surface,
            ForeColor = _muted,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 4)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = _surfaceHigh;
        button.MouseEnter += (_, _) =>
        {
            _showingClipboard = clipboard;
            LoadItems();
        };
        button.Click += (_, _) =>
        {
            _showingClipboard = clipboard;
            LoadItems();
        };
        parent.Controls.Add(button);
    }

    private void LoadItems()
    {
        if (_showingClipboard)
        {
            _list.SetItems(_data.ClipboardItems.OrderByDescending(item => item.Pinned).ThenByDescending(item => item.UpdatedAt).Take(20));
        }
        else
        {
            _list.SetItems(_data.Phrases.OrderByDescending(phrase => phrase.UpdatedAt).Take(20));
        }
    }

    private void CopySelected()
    {
        if (_showingClipboard && _list.SelectedItem is ClipboardRecord record)
        {
            _copyClipboard(record);
            Close();
        }
        else if (!_showingClipboard && _list.SelectedItem is Phrase phrase)
        {
            _copyPhrase(phrase);
            Close();
        }
    }

    private void DrawQuickItem(Graphics graphics, Rectangle bounds, object item, bool selected)
    {
        using var background = new SolidBrush(selected ? _surfaceHigh : _surfaceLow);
        using var ink = new SolidBrush(_ink);
        using var muted = new SolidBrush(_muted);
        using var line = new Pen(_line);
        using var stripe = new SolidBrush(selected ? _active : _line);
        graphics.FillRectangle(background, bounds);
        graphics.FillRectangle(stripe, bounds.Left, bounds.Top, selected ? 3 : 1, bounds.Height);
        graphics.DrawLine(line, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

        var title = "";
        var detail = "";
        if (item is ClipboardRecord record)
        {
            title = record.Title;
            detail = record.UpdatedAt.ToString("MM/dd HH:mm");
        }
        else if (item is Phrase phrase)
        {
            title = phrase.Title;
            detail = phrase.Content;
        }

        using var titleFont = new Font(Font, FontStyle.Bold);
        using var mono = new Font("Consolas", 9F);
        graphics.DrawString(title, titleFont, ink, bounds.Left + 14, bounds.Top + 8);
        graphics.DrawString(detail, mono, muted, bounds.Left + 14, bounds.Top + 30);
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
}
