using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CopyCreator;

internal static class Program
{
    /// <summary>
    /// 程序入口，启用 Windows Forms 桌面样式并启动主窗口。
    /// </summary>
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Application.ThreadException += (_, eventArgs) => WriteCrashLog(eventArgs.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                if (eventArgs.ExceptionObject is Exception exception)
                {
                    WriteCrashLog(exception);
                }
            };
            Application.Run(new MainForm());
        }
        catch (Exception exception)
        {
            WriteCrashLog(exception);
        }
    }

    /// <summary>
    /// 写入启动阶段崩溃日志，避免无窗口退出时完全没有排查线索。
    /// </summary>
    private static void WriteCrashLog(Exception exception)
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "data", "logs");
        Directory.CreateDirectory(logDir);
        File.AppendAllText(
            Path.Combine(logDir, "crash.log"),
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {exception}{Environment.NewLine}",
            Encoding.UTF8);
    }
}

internal enum ClipboardKind
{
    Text,
    Link,
    Image,
    File
}

internal sealed class ClipboardRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClipboardKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? PreviewPath { get; set; }
    public string? OriginalPath { get; set; }
    public string? CachedPath { get; set; }
    public bool Cached { get; set; }
    public bool Pinned { get; set; }
    public string ContentHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public long SizeBytes { get; set; }
}

internal sealed class PhraseGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

internal sealed class Phrase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

internal sealed class AppSettings
{
    public string Language { get; set; } = "zh";
    public string Theme { get; set; } = "dark";
    public string Density { get; set; } = "normal";
    public bool StartOnBoot { get; set; }
    public bool HideOnClose { get; set; } = true;
    public int SaveDays { get; set; } = 30;
    public int MaxItems { get; set; } = 1000;
    public bool RecordText { get; set; } = true;
    public bool RecordLinks { get; set; } = true;
    public bool RecordImages { get; set; } = true;
    public bool RecordFiles { get; set; } = true;
    public bool SensitiveDetection { get; set; }
    public int FileMaxMb { get; set; } = 200;
    public int CacheMaxGb { get; set; } = 5;
    public bool QuickShowClipboard { get; set; } = true;
    public bool QuickShowPhrases { get; set; } = true;
    public string QuickHotkey { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "https://api.deepseek.com";
    public string ApiKey { get; set; } = "";
    public string ModelName { get; set; } = "deepseek-chat";
    public string DefaultTargetLanguage { get; set; } = "English";
}

internal sealed class AppData
{
    public List<ClipboardRecord> ClipboardItems { get; set; } = [];
    public List<PhraseGroup> PhraseGroups { get; set; } = [];
    public List<Phrase> Phrases { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}

internal sealed class DataStore
{
    private readonly string _dataFile;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string DataDir { get; }
    public string ImageCacheDir { get; }
    public string FileCacheDir { get; }
    public string LogDir { get; }
    public AppData Data { get; private set; }

    public DataStore()
    {
        var baseDir = AppContext.BaseDirectory;
        DataDir = Path.Combine(baseDir, "data");
        ImageCacheDir = Path.Combine(DataDir, "cache", "images");
        FileCacheDir = Path.Combine(DataDir, "cache", "files");
        LogDir = Path.Combine(DataDir, "logs");
        Directory.CreateDirectory(ImageCacheDir);
        Directory.CreateDirectory(FileCacheDir);
        Directory.CreateDirectory(LogDir);
        _dataFile = Path.Combine(DataDir, "copy-creator.json");
        Data = Load();
    }

    /// <summary>
    /// 从程序同级 data 目录读取 UTF-8 JSON 数据，文件不存在时创建默认数据。
    /// </summary>
    public AppData Load()
    {
        if (!File.Exists(_dataFile))
        {
            var data = CreateDefaultData();
            Save(data);
            return data;
        }

        AppData loaded;
        using (var stream = File.OpenRead(_dataFile))
        {
            loaded = JsonSerializer.Deserialize<AppData>(stream, _jsonOptions) ?? CreateDefaultData();
        }

        if (EnsureDefaults(loaded))
        {
            Save(loaded);
        }

        return loaded;
    }

    /// <summary>
    /// 使用 UTF-8 保存应用数据，避免中文配置和短语出现乱码。
    /// </summary>
    public void Save()
    {
        Save(Data);
    }

    private void Save(AppData data)
    {
        var tempFile = $"{_dataFile}.tmp";
        using (var stream = File.Create(tempFile))
        {
            JsonSerializer.Serialize(stream, data, _jsonOptions);
        }

        File.Copy(tempFile, _dataFile, true);
        File.Delete(tempFile);
        Data = data;
    }

    /// <summary>
    /// 写入不含隐私正文的运行日志，只记录事件名称、错误信息和时间。
    /// </summary>
    public void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}";
        File.AppendAllText(Path.Combine(LogDir, "copy-creator.log"), line, Encoding.UTF8);
    }

    private static AppData CreateDefaultData()
    {
        var ai = new PhraseGroup { Name = "AI 指令", SortOrder = 1 };
        var service = new PhraseGroup { Name = "客服短语", SortOrder = 2 };
        var links = new PhraseGroup { Name = "常用链接", SortOrder = 3 };
        return new AppData
        {
            PhraseGroups = [ai, service, links],
            Phrases =
            [
                new Phrase { GroupId = ai.Id, SortOrder = 1, Title = "/context", Content = "/context", Description = "查看上下文" },
                new Phrase { GroupId = ai.Id, SortOrder = 2, Title = "/compact", Content = "/compact", Description = "压缩上下文" },
                new Phrase { GroupId = ai.Id, SortOrder = 3, Title = "/resume", Content = "/resume", Description = "查看历史会话" },
                new Phrase { GroupId = ai.Id, SortOrder = 4, Title = "/clear", Content = "/clear", Description = "清除上下文" }
            ]
        };
    }

    private static bool EnsureDefaults(AppData data)
    {
        var changed = NormalizeBuiltInPhrases(data);
        changed = NormalizePhraseOrdering(data) || changed;
        if (data.PhraseGroups.Count > 0)
        {
            return changed;
        }

        var defaults = CreateDefaultData();
        data.PhraseGroups = defaults.PhraseGroups;
        data.Phrases = defaults.Phrases;
        return true;
    }

    /// <summary>
    /// 为旧数据补齐短语排序值，避免新增拖拽排序后历史短语顺序不稳定。
    /// </summary>
    private static bool NormalizePhraseOrdering(AppData data)
    {
        var changed = false;
        foreach (var group in data.PhraseGroups)
        {
            var order = 1;
            foreach (var phrase in data.Phrases
                         .Where(phrase => phrase.GroupId == group.Id)
                         .OrderBy(phrase => phrase.SortOrder == 0 ? int.MaxValue : phrase.SortOrder)
                         .ThenByDescending(phrase => phrase.UpdatedAt))
            {
                if (phrase.SortOrder != order)
                {
                    phrase.SortOrder = order;
                    changed = true;
                }

                order++;
            }
        }

        return changed;
    }

    /// <summary>
    /// 纠正早期默认 AI 指令把说明文字当成复制内容的问题，确保点击后复制真实命令。
    /// </summary>
    private static bool NormalizeBuiltInPhrases(AppData data)
    {
        var commands = new Dictionary<string, string>
        {
            ["/context"] = "/context",
            ["/compact"] = "/compact",
            ["/resume"] = "/resume",
            ["/clear"] = "/clear"
        };
        var descriptions = new Dictionary<string, string>
        {
            ["/context"] = "查看上下文",
            ["/compact"] = "压缩上下文",
            ["/resume"] = "查看历史会话",
            ["/clear"] = "清除上下文"
        };
        var changed = false;
        foreach (var phrase in data.Phrases.Where(phrase => commands.ContainsKey(phrase.Title)))
        {
            var command = commands[phrase.Title];
            var description = descriptions[phrase.Title];
            if (phrase.Content == command && phrase.Description == description)
            {
                continue;
            }

            phrase.Content = command;
            phrase.Description = description;
            phrase.UpdatedAt = DateTime.Now;
            changed = true;
        }

        return changed;
    }
}
