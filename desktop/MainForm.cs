using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

namespace CopyCreator;

internal sealed class MainForm : Form
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
    private const int WmHotkey = 0x0312;
    private const int QuickOverlayHotkeyId = 9101;
    private const int WhMouseLl = 14;
    private const int WmXbuttondown = 0x020B;
    private const int MouseXbutton1 = 1;
    private const int MouseXbutton2 = 2;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly DataStore _store = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly System.Windows.Forms.Timer _clipboardTimer = new() { Interval = 800 };
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private QuickOverlayForm? _quickOverlay;
    private string _lastSignature = "";
    private bool _exiting;
    private IntPtr _mouseHook;
    private int _quickMouseButton;
    private IntPtr _quickOverlayReturnWindow;
    private readonly NativeMethods.LowLevelMouseProc _mouseProc;

    public MainForm()
    {
        _mouseProc = HandleGlobalMouse;
        Text = "Copy OS";
        Icon = LoadAppIcon();
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(760, 560);
        Size = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(8, 9, 9);
        Controls.Add(_webView);
        BuildTray();
        InitializeWebView();

        _clipboardTimer.Tick += (_, _) => CaptureClipboardSafely();
        _clipboardTimer.Start();
        RegisterQuickHotkey();
        _store.Log("app_started_webview");
    }

    /// <summary>
    /// 初始化 WebView2，并将桌面能力通过消息桥接给 HTML 界面。
    /// </summary>
    private async void InitializeWebView()
    {
        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.WebMessageReceived += async (_, args) => await HandleWebMessage(args.WebMessageAsJson);
        _webView.NavigateToString(BuildHtml());
    }

    private void BuildTray()
    {
        var trayMenu = new ContextMenuStrip
        {
            ShowImageMargin = false
        };
        trayMenu.Items.Add("打开", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("设置", null, async (_, _) => await RestoreAndOpenTab("settings"));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出", null, (_, _) => ExitApplication());
        _trayIcon.Text = "Copy OS";
        _trayIcon.Icon = LoadAppIcon();
        _trayIcon.ContextMenuStrip = trayMenu;
        _trayIcon.Visible = true;
        _trayIcon.MouseUp += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private static Icon LoadAppIcon()
    {
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exiting && _store.Data.Settings.HideOnClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        SaveSettingsSilently();
        NativeMethods.UnregisterHotKey(Handle, QuickOverlayHotkeyId);
        UnregisterQuickMouseHook();
        _trayIcon.Visible = false;
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey && message.WParam.ToInt32() == QuickOverlayHotkeyId)
        {
            ShowQuickOverlay();
            return;
        }

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
        message.Result = (left, right, top, bottom) switch
        {
            (true, _, true, _) => Httopleft,
            (_, true, true, _) => Httopright,
            (true, _, _, true) => Htbottomleft,
            (_, true, _, true) => Htbottomright,
            (true, _, _, _) => Htleft,
            (_, true, _, _) => Htright,
            (_, _, true, _) => Httop,
            (_, _, _, true) => Htbottom,
            _ => message.Result
        };
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task RestoreAndOpenTab(string tabName)
    {
        RestoreFromTray();
        await PostToWeb("openTab", new { tab = tabName });
    }

    private void ExitApplication()
    {
        _exiting = true;
        _trayIcon.Visible = false;
        SaveSettingsSilently();
        Application.Exit();
    }

    private async Task HandleWebMessage(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString() ?? "";
        switch (type)
        {
            case "ready":
                await PushState();
                break;
            case "drag":
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
                break;
            case "close":
                Close();
                break;
            case "exit":
                ExitApplication();
                break;
            case "copyClipboard":
                CopyClipboardItem(Guid.Parse(root.GetProperty("id").GetString() ?? ""));
                await PostToWeb("toast", new { text = "已复制" });
                break;
            case "pinClipboard":
                TogglePin(Guid.Parse(root.GetProperty("id").GetString() ?? ""));
                await PostToWeb("clipboardPinned", new { id = root.GetProperty("id").GetString() ?? "" });
                break;
            case "deleteClipboard":
                DeleteClipboard(Guid.Parse(root.GetProperty("id").GetString() ?? ""));
                await PostToWeb("clipboardDeleted", new { id = root.GetProperty("id").GetString() ?? "" });
                break;
            case "copyPhrase":
                CopyPhrase(Guid.Parse(root.GetProperty("id").GetString() ?? ""));
                await PostToWeb("toast", new { text = "短语已复制" });
                break;
            case "deletePhrase":
                DeletePhrase(Guid.Parse(root.GetProperty("id").GetString() ?? ""));
                await PostToWeb("phraseDeleted", new { id = root.GetProperty("id").GetString() ?? "" });
                break;
            case "addPhrase":
                var phrase = AddPhrase(
                    Guid.Parse(root.GetProperty("groupId").GetString() ?? ""),
                    root.GetProperty("title").GetString() ?? "",
                    root.GetProperty("content").GetString() ?? "");
                await PostToWeb("phraseAdded", new { phrase, toast = phrase is null ? "短语内容为空" : "短语已添加" });
                break;
            case "addPhraseGroup":
                var group = AddPhraseGroup(root.GetProperty("name").GetString() ?? "");
                await PostToWeb("phraseGroupSaved", new { group, toast = "分类已添加" });
                break;
            case "renamePhraseGroup":
                var renamed = RenamePhraseGroup(
                    Guid.Parse(root.GetProperty("id").GetString() ?? ""),
                    root.GetProperty("name").GetString() ?? "");
                await PostToWeb("phraseGroupSaved", new { group = renamed, toast = renamed is null ? "分类名称为空" : "分类已重命名" });
                break;
            case "reorderPhrases":
                ReorderPhrases(root.GetProperty("ids").EnumerateArray()
                    .Select(item => Guid.Parse(item.GetString() ?? ""))
                    .ToList());
                await PostToWeb("phrasesReordered", new { ok = true });
                break;
            case "copyText":
                Clipboard.SetText(root.GetProperty("text").GetString() ?? "");
                await PostToWeb("toast", new { text = "已复制" });
                break;
            case "readClipboardText":
                await PostToWeb("clipboardText", new { text = Clipboard.ContainsText() ? Clipboard.GetText() : "" });
                break;
            case "saveSetting":
                SaveSetting(root.GetProperty("key").GetString() ?? "", root.GetProperty("value"));
                SaveSettingsSilently();
                await PostToWeb("settingsSaved", new
                {
                    ok = true,
                    key = root.GetProperty("key").GetString() ?? ""
                });
                break;
            case "translate":
                var text = root.GetProperty("text").GetString() ?? "";
                var target = root.GetProperty("target").GetString() ?? "English";
                await PushTranslation(await TranslateAsync(text, target));
                break;
            case "testApi":
                var testApiBaseUrl = root.TryGetProperty("apiBaseUrl", out var apiBaseUrl) ? apiBaseUrl.GetString() : null;
                var testApiKey = root.TryGetProperty("apiKey", out var apiKey) ? apiKey.GetString() : null;
                var testModelName = root.TryGetProperty("modelName", out var modelName) ? modelName.GetString() : null;
                SaveApiSettings(testApiBaseUrl, testApiKey, testModelName);
                SaveSettingsSilently();
                await PostToWeb("toast", new
                {
                    text = await TestApiAsync(testApiBaseUrl, testApiKey, testModelName)
                });
                break;
            case "showQuickOverlay":
                ShowQuickOverlay();
                break;
            case "openData":
                Process.Start("explorer.exe", _store.DataDir);
                break;
            case "openLogs":
                Process.Start("explorer.exe", _store.LogDir);
                break;
        }
    }

    private void ShowQuickOverlay()
    {
        if (_quickOverlay is { IsDisposed: false })
        {
            _quickOverlay.Close();
        }

        _quickOverlayReturnWindow = NativeMethods.GetForegroundWindow();
        _quickOverlay = new QuickOverlayForm(_store.Data, CopyClipboardItemFromOverlay, CopyPhraseFromOverlay);
        _quickOverlay.Show();
        _quickOverlay.Activate();
    }

    private void CopyClipboardItemFromOverlay(Guid id)
    {
        if (CopyClipboardItem(id) && ShouldPasteAfterOverlay(id))
        {
            PasteIntoReturnWindow();
        }
    }

    private void CopyPhraseFromOverlay(Guid id)
    {
        if (CopyPhrase(id))
        {
            PasteIntoReturnWindow();
        }
    }

    private bool ShouldPasteAfterOverlay(Guid id)
    {
        var record = _store.Data.ClipboardItems.FirstOrDefault(item => item.Id == id);
        return record?.Kind is ClipboardKind.Text or ClipboardKind.Link or ClipboardKind.Image;
    }

    /// <summary>
    /// 快捷浮窗复制后将焦点还给呼出前窗口，并发送粘贴快捷键实现输入框直填。
    /// </summary>
    private void PasteIntoReturnWindow()
    {
        if (_quickOverlayReturnWindow == IntPtr.Zero || _quickOverlayReturnWindow == Handle)
        {
            return;
        }

        NativeMethods.SetForegroundWindow(_quickOverlayReturnWindow);
        SendKeys.SendWait("^v");
    }

    private async Task PushState(string toast = "")
    {
        var state = new
        {
            clipboard = _store.Data.ClipboardItems
                .OrderByDescending(item => item.Pinned)
                .ThenByDescending(item => item.UpdatedAt)
                .Select(ToClipboardView),
            groups = _store.Data.PhraseGroups.OrderBy(group => group.SortOrder),
            phrases = _store.Data.Phrases
                .OrderBy(phrase => phrase.SortOrder == 0 ? int.MaxValue : phrase.SortOrder)
                .ThenByDescending(phrase => phrase.UpdatedAt),
            settings = _store.Data.Settings,
            toast
        };
        await PostToWeb("state", state);
    }

    /// <summary>
    /// 将剪贴板记录转换成前端稳定消费的数据结构，避免枚举被序列化成数字后破坏分类和颜色。
    /// </summary>
    private static object ToClipboardView(ClipboardRecord item)
    {
        return new
        {
            item.Id,
            Kind = item.Kind.ToString(),
            item.Title,
            item.Content,
            item.PreviewPath,
            PreviewUri = ToImageDataUri(item.PreviewPath),
            item.OriginalPath,
            item.CachedPath,
            item.Cached,
            item.Pinned,
            item.ContentHash,
            item.CreatedAt,
            item.UpdatedAt,
            item.SizeBytes
        };
    }

    private static string ToImageDataUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "";
        }

        var extension = Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(path).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            ? "jpeg"
            : "png";
        return $"data:image/{extension};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
    }

    private Task PushTranslation(string result)
    {
        return PostToWeb("translation", new { result });
    }

    private async Task PostToWeb(string type, object payload)
    {
        var json = JsonSerializer.Serialize(new { type, payload });
        await _webView.CoreWebView2.ExecuteScriptAsync($"window.copyCreatorReceive({json});");
    }

    private void SaveSetting(string key, JsonElement value)
    {
        var settings = _store.Data.Settings;
        switch (key)
        {
            case "language":
                settings.Language = value.GetString() ?? settings.Language;
                break;
            case "theme":
                settings.Theme = value.GetString() ?? settings.Theme;
                break;
            case "density":
                settings.Density = value.GetString() ?? "normal";
                break;
            case "startOnBoot":
                settings.StartOnBoot = value.GetBoolean();
                ApplyStartOnBoot(settings.StartOnBoot);
                break;
            case "hideOnClose":
                settings.HideOnClose = value.GetBoolean();
                break;
            case "saveDays":
                settings.SaveDays = value.GetInt32();
                break;
            case "maxItems":
                settings.MaxItems = value.GetInt32();
                break;
            case "recordText":
                settings.RecordText = value.GetBoolean();
                break;
            case "recordLinks":
                settings.RecordLinks = value.GetBoolean();
                break;
            case "recordImages":
                settings.RecordImages = value.GetBoolean();
                break;
            case "recordFiles":
                settings.RecordFiles = value.GetBoolean();
                break;
            case "sensitiveDetection":
                settings.SensitiveDetection = value.GetBoolean();
                break;
            case "fileMaxMb":
                settings.FileMaxMb = value.GetInt32();
                break;
            case "cacheMaxGb":
                settings.CacheMaxGb = value.GetInt32();
                break;
            case "apiBaseUrl":
                settings.ApiBaseUrl = value.GetString() ?? settings.ApiBaseUrl;
                break;
            case "apiKey":
                settings.ApiKey = value.GetString() ?? "";
                break;
            case "modelName":
                settings.ModelName = value.GetString() ?? settings.ModelName;
                break;
            case "defaultTargetLanguage":
                settings.DefaultTargetLanguage = value.GetString() ?? settings.DefaultTargetLanguage;
                break;
            case "quickShowClipboard":
                settings.QuickShowClipboard = value.GetBoolean();
                break;
            case "quickShowPhrases":
                settings.QuickShowPhrases = value.GetBoolean();
                break;
            case "quickHotkey":
                settings.QuickHotkey = value.GetString() ?? "";
                var hotkeyResult = RegisterQuickHotkey();
                _ = PostToWeb("hotkeyStatus", new { text = hotkeyResult });
                break;
        }
    }

    private void SaveApiSettings(string? apiBaseUrl, string? apiKey, string? modelName)
    {
        var settings = _store.Data.Settings;
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            settings.ApiBaseUrl = apiBaseUrl.Trim();
        }

        settings.ApiKey = apiKey?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            settings.ModelName = modelName.Trim();
        }
    }

    /// <summary>
    /// 根据用户录入的快捷键注册全局热键；支持单键、组合键和鼠标侧键，空值表示不注册。
    /// </summary>
    private string RegisterQuickHotkey()
    {
        NativeMethods.UnregisterHotKey(Handle, QuickOverlayHotkeyId);
        UnregisterQuickMouseHook();
        var hotkey = _store.Data.Settings.QuickHotkey;
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return "快捷键已清除。";
        }

        if (TryParseMouseHotkey(hotkey, out var mouseButton))
        {
            _quickMouseButton = mouseButton;
            _mouseHook = NativeMethods.SetWindowsHookEx(WhMouseLl, _mouseProc, IntPtr.Zero, 0);
            if (_mouseHook == IntPtr.Zero)
            {
                _store.Log("quick_mouse_hook_register_failed");
                return "鼠标快捷键注册失败：系统没有允许捕获这个鼠标键。";
            }

            return $"快捷键已设置：{hotkey}";
        }

        if (!TryParseKeyboardHotkey(hotkey, out var modifiers, out var key))
        {
            return "快捷键无效：请按一个系统能识别的键，或鼠标侧键。";
        }

        if (!NativeMethods.RegisterHotKey(Handle, QuickOverlayHotkeyId, modifiers, key))
        {
            _store.Log("quick_hotkey_register_failed");
            return "快捷键注册失败：可能已经被其他软件占用。";
        }

        return $"快捷键已设置：{hotkey}";
    }

    private void UnregisterQuickMouseHook()
    {
        _quickMouseButton = 0;
        if (_mouseHook == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(_mouseHook);
        _mouseHook = IntPtr.Zero;
    }

    private IntPtr HandleGlobalMouse(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && wParam.ToInt32() == WmXbuttondown && _quickMouseButton != 0)
        {
            var info = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.Msllhookstruct>(lParam);
            var button = (int)((info.MouseData >> 16) & 0xffff);
            if (button == _quickMouseButton)
            {
                BeginInvoke(ShowQuickOverlay);
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private static bool TryParseMouseHotkey(string? hotkey, out int button)
    {
        button = 0;
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return false;
        }

        if (hotkey.Equals("MouseBack", StringComparison.OrdinalIgnoreCase)
            || hotkey.Equals("XButton1", StringComparison.OrdinalIgnoreCase))
        {
            button = MouseXbutton1;
            return true;
        }

        if (hotkey.Equals("MouseForward", StringComparison.OrdinalIgnoreCase)
            || hotkey.Equals("XButton2", StringComparison.OrdinalIgnoreCase))
        {
            button = MouseXbutton2;
            return true;
        }

        return false;
    }

    private static bool TryParseKeyboardHotkey(string? hotkey, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return false;
        }

        foreach (var part in hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModControl;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModAlt;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModShift;
            }
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModWin;
            }
            else if (Enum.TryParse<Keys>(part, true, out var parsed))
            {
                key = (uint)parsed;
            }
        }

        return key != 0;
    }

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
            _ = PushState();
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
            return files.Count == 0 ? null : CreateFileRecord(files);
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
            if ((isLink && !settings.RecordLinks) || (!isLink && !settings.RecordText))
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
        var existing = _store.Data.ClipboardItems.FirstOrDefault(item => item.Kind == record.Kind && item.ContentHash == record.ContentHash);
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

    private bool CopyClipboardItem(Guid id)
    {
        var record = _store.Data.ClipboardItems.FirstOrDefault(item => item.Id == id);
        if (record is null)
        {
            return false;
        }

        if (record.Kind == ClipboardKind.Image && record.CachedPath is not null && File.Exists(record.CachedPath))
        {
            using var image = Image.FromFile(record.CachedPath);
            Clipboard.SetImage(new Bitmap(image));
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
                return false;
            }

            Clipboard.SetFileDropList(paths);
        }
        else
        {
            Clipboard.SetText(record.Content);
        }

        return true;
    }

    private void TogglePin(Guid id)
    {
        var record = _store.Data.ClipboardItems.FirstOrDefault(item => item.Id == id);
        if (record is null) return;
        record.Pinned = !record.Pinned;
        _store.Save();
    }

    private void DeleteClipboard(Guid id)
    {
        var record = _store.Data.ClipboardItems.FirstOrDefault(item => item.Id == id);
        if (record is null) return;
        TryDelete(record.PreviewPath);
        TryDelete(record.CachedPath);
        _store.Data.ClipboardItems.Remove(record);
        _store.Save();
    }

    private bool CopyPhrase(Guid id)
    {
        var phrase = _store.Data.Phrases.FirstOrDefault(item => item.Id == id);
        if (phrase is not null)
        {
            Clipboard.SetText(phrase.Content);
            return true;
        }

        return false;
    }

    private void DeletePhrase(Guid id)
    {
        var phrase = _store.Data.Phrases.FirstOrDefault(item => item.Id == id);
        if (phrase is null)
        {
            return;
        }

        _store.Data.Phrases.Remove(phrase);
        _store.Save();
    }

    /// <summary>
    /// 添加用户自定义快捷短语，标题为空时使用内容首行，内容为空时不写入。
    /// </summary>
    private Phrase? AddPhrase(Guid groupId, string title, string content)
    {
        var normalizedContent = content.Trim();
        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            return null;
        }

        var targetGroup = _store.Data.PhraseGroups.FirstOrDefault(group => group.Id == groupId)
            ?? _store.Data.PhraseGroups.OrderBy(group => group.SortOrder).FirstOrDefault();
        if (targetGroup is null)
        {
            targetGroup = new PhraseGroup { Name = "自定义", SortOrder = 1 };
            _store.Data.PhraseGroups.Add(targetGroup);
        }

        var phrase = new Phrase
        {
            GroupId = targetGroup.Id,
            SortOrder = _store.Data.Phrases
                .Where(item => item.GroupId == targetGroup.Id)
                .Select(item => item.SortOrder)
                .DefaultIfEmpty()
                .Max() + 1,
            Title = string.IsNullOrWhiteSpace(title) ? FirstLine(normalizedContent, 32) : title.Trim(),
            Content = normalizedContent,
            Description = string.IsNullOrWhiteSpace(title) ? "自定义短语" : title.Trim(),
            UpdatedAt = DateTime.Now
        };
        _store.Data.Phrases.Add(phrase);
        _store.Save();
        return phrase;
    }

    /// <summary>
    /// 新增短语分类，名称为空时不写入，排序追加到当前分类末尾。
    /// </summary>
    private PhraseGroup? AddPhraseGroup(string name)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var group = new PhraseGroup
        {
            Name = normalizedName,
            SortOrder = _store.Data.PhraseGroups
                .Select(item => item.SortOrder)
                .DefaultIfEmpty()
                .Max() + 1
        };
        _store.Data.PhraseGroups.Add(group);
        _store.Save();
        return group;
    }

    /// <summary>
    /// 重命名短语分类，保持分类 ID 和排序不变，名称为空时拒绝修改。
    /// </summary>
    private PhraseGroup? RenamePhraseGroup(Guid id, string name)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var group = _store.Data.PhraseGroups.FirstOrDefault(item => item.Id == id);
        if (group is null)
        {
            return null;
        }

        group.Name = normalizedName;
        _store.Save();
        return group;
    }

    /// <summary>
    /// 保存当前短语卡片拖拽后的顺序，只调整传入 ID 对应的短语排序。
    /// </summary>
    private void ReorderPhrases(IReadOnlyList<Guid> orderedIds)
    {
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var phrase = _store.Data.Phrases.FirstOrDefault(item => item.Id == orderedIds[index]);
            if (phrase is not null)
            {
                phrase.SortOrder = index + 1;
                phrase.UpdatedAt = DateTime.Now;
            }
        }

        _store.Save();
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
            var endpoint = BuildChatCompletionsEndpoint(settings.ApiBaseUrl);
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

    /// <summary>
    /// 使用当前翻译配置发送最小 OpenAI 兼容请求，返回清晰的连接测试结果。
    /// </summary>
    private async Task<string> TestApiAsync(string? apiBaseUrl = null, string? apiKey = null, string? modelName = null)
    {
        var settings = _store.Data.Settings;
        var effectiveApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? settings.ApiBaseUrl : apiBaseUrl;
        var effectiveApiKey = string.IsNullOrWhiteSpace(apiKey) ? settings.ApiKey : apiKey;
        var effectiveModelName = string.IsNullOrWhiteSpace(modelName) ? settings.ModelName : modelName;
        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            return "测试失败：请先填写 API Key。";
        }

        try
        {
            var endpoint = BuildChatCompletionsEndpoint(effectiveApiBaseUrl!);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", effectiveApiKey);
            var body = new
            {
                model = effectiveModelName,
                messages = new[]
                {
                    new { role = "system", content = "Reply with OK only." },
                    new { role = "user", content = "ping" }
                },
                max_tokens = 4
            };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode && LooksLikeOpenAiResponse(responseBody))
            {
                return "测试通过：API 地址、Key 和模型可以连接。";
            }

            if (response.IsSuccessStatusCode)
            {
                _store.Log("api_test_failed reason=unexpected_response");
                return "测试失败：接口已连接，但返回格式不是 OpenAI 兼容格式。";
            }

            var reason = ExtractApiError(responseBody);
            _store.Log($"api_test_failed status={(int)response.StatusCode}");
            return string.IsNullOrWhiteSpace(reason)
                ? $"测试失败：接口返回 {(int)response.StatusCode}，请检查 Key、模型或额度。"
                : $"测试失败：接口返回 {(int)response.StatusCode}，{reason}";
        }
        catch (TaskCanceledException ex)
        {
            _store.Log($"api_test_timeout type={ex.GetType().Name}");
            return "测试失败：连接超时，请检查网络或 API 地址。";
        }
        catch (Exception ex)
        {
            _store.Log($"api_test_error type={ex.GetType().Name} message={ex.Message}");
            return "测试失败：无法连接，请检查 API 地址、网络或代理。";
        }
    }

    private void ApplyStartOnBoot(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null) return;
            if (enabled)
            {
                key.SetValue("CopyCreator", $"\"{Application.ExecutablePath}\"");
            }
            else
            {
                key.DeleteValue("CopyCreator", false);
            }
        }
        catch (Exception ex)
        {
            _store.Log($"startup_error type={ex.GetType().Name} message={ex.Message}");
        }
    }

    private void SaveSettingsSilently() => _store.Save();

    private static string BuildChatCompletionsEndpoint(string apiBaseUrl)
    {
        var baseUrl = apiBaseUrl.Trim().TrimEnd('/');
        if (baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return baseUrl;
        }

        return baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/v1/chat/completions";
    }

    private static bool LooksLikeOpenAiResponse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ExtractApiError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var message))
                {
                    return FirstLine(message.GetString() ?? "", 90);
                }

                return FirstLine(error.ToString(), 90);
            }

            if (doc.RootElement.TryGetProperty("message", out var rootMessage))
            {
                return FirstLine(rootMessage.GetString() ?? "", 90);
            }
        }
        catch (JsonException)
        {
            return FirstLine(body, 90);
        }

        return "";
    }

    private static bool LooksSensitive(string text)
    {
        return Regex.IsMatch(text, "(api[_-]?key|secret|token|password|sk-[a-zA-Z0-9])", RegexOptions.IgnoreCase)
            || text.Length is > 24 and < 240 && Regex.IsMatch(text, "^[A-Za-z0-9_\\-\\.]{24,}$");
    }

    private static void TryDelete(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string FirstLine(string value, int maxLength)
    {
        var line = value.ReplaceLineEndings(" ").Trim();
        return line.Length > maxLength ? line[..maxLength] + "..." : line;
    }

    private static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private string BuildHtml()
    {
        return AppShellHtml.Value;
    }
}

internal static class NativeMethods
{
    internal delegate IntPtr LowLevelMouseProc(int code, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct Msllhookstruct
    {
        public Point Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr SendMessage(IntPtr hwnd, int msg, int wparam, int lparam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int hookType, LowLevelMouseProc callback, IntPtr module, uint threadId);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hook);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hwnd);
}
