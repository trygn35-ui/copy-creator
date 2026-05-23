namespace CopyCreator;

internal sealed class SmoothListView<T> : Control
{
    private readonly System.Windows.Forms.Timer _scrollTimer = new() { Interval = 15 };
    private readonly List<T> _items = [];
    private float _scrollOffset;
    private float _targetOffset;

    public SmoothListView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
            true);
        TabStop = true;
        ItemHeight = 64;
        ShowScrollBar = false;
        _scrollTimer.Tick += (_, _) => StepScroll();
    }

    public IReadOnlyList<T> Items => _items;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int ItemHeight { get; set; }

    public int SelectedIndex { get; private set; } = -1;

    public T? SelectedItem => SelectedIndex >= 0 && SelectedIndex < _items.Count ? _items[SelectedIndex] : default;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowScrollBar { get; set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color ScrollTrackColor { get; set; } = Color.FromArgb(14, 14, 16);

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color ScrollThumbColor { get; set; } = Color.FromArgb(68, 71, 72);

    public event EventHandler<T>? ItemClicked;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<T, Point, Rectangle, bool>? HandleItemAction { get; set; }

    public event EventHandler? SelectionChanged;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Action<Graphics, Rectangle, T, bool>? DrawItemContent { get; set; }

    /// <summary>
    /// 替换列表数据，并保留合理的滚动和选中范围。
    /// </summary>
    public void SetItems(IEnumerable<T> items)
    {
        _items.Clear();
        _items.AddRange(items);
        if (SelectedIndex >= _items.Count)
        {
            SelectedIndex = _items.Count - 1;
        }

        ClampScroll();
        Invalidate();
    }

    public void SelectItem(T item)
    {
        var index = _items.IndexOf(item);
        if (index >= 0)
        {
            SelectedIndex = index;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            EnsureVisible(index);
            Invalidate();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _targetOffset -= e.Delta / 120f * ItemHeight * 0.85f;
        ClampTarget();
        _scrollTimer.Start();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        var index = (int)((e.Y + _scrollOffset) / ItemHeight);
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        SelectedIndex = index;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
        var handled = HandleItemAction?.Invoke(_items[index], e.Location, new Rectangle(0, index * ItemHeight - (int)_scrollOffset, Width - (ShowScrollBar ? 8 : 0), ItemHeight)) ?? false;
        if (!handled && e.Button == MouseButtons.Left)
        {
            ItemClicked?.Invoke(this, _items[index]);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        var first = Math.Max(0, (int)(_scrollOffset / ItemHeight));
        var y = first * ItemHeight - _scrollOffset;
        for (var index = first; index < _items.Count && y < Height; index++)
        {
            var bounds = new Rectangle(0, (int)y, Width - (ShowScrollBar ? 8 : 0), ItemHeight);
            DrawItemContent?.Invoke(e.Graphics, bounds, _items[index], index == SelectedIndex);
            y += ItemHeight;
        }

        if (ShowScrollBar)
        {
            DrawScrollBar(e.Graphics);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ClampScroll();
    }

    private void StepScroll()
    {
        _scrollOffset += (_targetOffset - _scrollOffset) * 0.28f;
        if (Math.Abs(_targetOffset - _scrollOffset) < 0.6f)
        {
            _scrollOffset = _targetOffset;
            _scrollTimer.Stop();
        }

        Invalidate();
    }

    private void EnsureVisible(int index)
    {
        var itemTop = index * ItemHeight;
        var itemBottom = itemTop + ItemHeight;
        if (itemTop < _targetOffset)
        {
            _targetOffset = itemTop;
        }
        else if (itemBottom > _targetOffset + Height)
        {
            _targetOffset = itemBottom - Height;
        }

        ClampTarget();
        _scrollOffset = _targetOffset;
    }

    private void ClampScroll()
    {
        ClampTarget();
        _scrollOffset = Math.Min(_scrollOffset, MaxOffset);
        _targetOffset = Math.Min(_targetOffset, MaxOffset);
    }

    private void ClampTarget()
    {
        _targetOffset = Math.Clamp(_targetOffset, 0, MaxOffset);
    }

    private float MaxOffset => Math.Max(0, _items.Count * ItemHeight - Height);

    private void DrawScrollBar(Graphics graphics)
    {
        if (_items.Count == 0 || MaxOffset <= 0)
        {
            return;
        }

        var track = new Rectangle(Width - 5, 0, 4, Height);
        using var trackBrush = new SolidBrush(ScrollTrackColor);
        graphics.FillRectangle(trackBrush, track);

        var ratio = Height / (float)(_items.Count * ItemHeight);
        var thumbHeight = Math.Max(28, (int)(Height * ratio));
        var thumbTop = (int)((Height - thumbHeight) * (_scrollOffset / MaxOffset));
        using var thumbBrush = new SolidBrush(ScrollThumbColor);
        graphics.FillRectangle(thumbBrush, new Rectangle(Width - 5, thumbTop, 4, thumbHeight));
    }
}
