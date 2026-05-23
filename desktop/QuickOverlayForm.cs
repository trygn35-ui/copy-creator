using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CopyCreator;

internal sealed class QuickOverlayForm : Form
{
    private readonly AppData _data;
    private readonly Action<Guid> _copyClipboard;
    private readonly Action<Guid> _copyPhrase;
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };

    public QuickOverlayForm(AppData data, Action<Guid> copyClipboard, Action<Guid> copyPhrase)
    {
        _data = data;
        _copyClipboard = copyClipboard;
        _copyPhrase = copyPhrase;
        Text = "Copy OS Quick Panel";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(560, 420);
        BackColor = data.Settings.Theme == "white"
            ? Color.FromArgb(241, 238, 227)
            : Color.FromArgb(20, 24, 17);
        KeyPreview = true;
        Controls.Add(_webView);
        Deactivate += (_, _) => Close();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
        InitializeWebView();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        MoveToCursorLowerLeft();
    }

    /// <summary>
    /// 初始化快捷浮窗 WebView，保持与主界面一致的自绘视觉并提供点击复制后关闭的行为。
    /// </summary>
    private async void InitializeWebView()
    {
        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.WebMessageReceived += (_, args) => HandleMessage(args.WebMessageAsJson);
        _webView.NavigateToString(BuildHtml());
    }

    /// <summary>
    /// 将快捷浮窗放到当前鼠标指针左下方，并限制在当前屏幕工作区内。
    /// </summary>
    private void MoveToCursorLowerLeft()
    {
        var cursor = Cursor.Position;
        var area = Screen.FromPoint(cursor).WorkingArea;
        var left = Math.Clamp(cursor.X - Width, area.Left, area.Right - Width);
        var top = Math.Clamp(cursor.Y + 12, area.Top, area.Bottom - Height);
        Location = new Point(left, top);
    }

    private void HandleMessage(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var type = root.GetProperty("type").GetString() ?? "";
        if (type == "close")
        {
            Close();
            return;
        }

        if (type == "drag")
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
            return;
        }

        var id = Guid.Parse(root.GetProperty("id").GetString() ?? "");
        if (type == "copyClipboard")
        {
            _copyClipboard(id);
            Close();
        }
        else if (type == "copyPhrase")
        {
            _copyPhrase(id);
            Close();
        }
    }

    private string BuildHtml()
    {
        var payload = JsonSerializer.Serialize(new
        {
            clipboard = _data.Settings.QuickShowClipboard
                ? _data.ClipboardItems.OrderByDescending(item => item.Pinned).ThenByDescending(item => item.UpdatedAt).Take(20).Select(item => (object)new
                {
                    item.Id,
                    Kind = item.Kind.ToString(),
                    item.Title,
                    item.Content,
                    PreviewUri = ToImageDataUri(item.PreviewPath),
                    item.Pinned
                })
                : Enumerable.Empty<object>(),
            phrases = _data.Settings.QuickShowPhrases
                ? _data.Phrases
                    .OrderBy(item => item.SortOrder == 0 ? int.MaxValue : item.SortOrder)
                    .ThenByDescending(item => item.UpdatedAt)
                    .Take(20)
                    .Select(item => (object)new
                {
                    item.Id,
                    item.Title,
                    item.Content,
                    item.Description
                })
                : Enumerable.Empty<object>(),
            language = _data.Settings.Language,
            theme = _data.Settings.Theme == "white" ? "white" : "dark"
        });

        return $$"""
<!doctype html>
<html><head><meta charset="utf-8"><style>
*{box-sizing:border-box}html,body,#app{width:100%;height:100%;margin:0;overflow:hidden}
:root{--ink:#f4f0e6;--text:#dfded2;--muted:#9aa197;--line:#343b33;--line2:#626b60;--rail:#181d15;--panel:#1d231b;--panel2:#273024;--field:#11170f;--bar:#11160f;--accent:#f0cf63;--active-fg:#11160f;--cyan:#6bd8c5;--danger-bg:#44201e;--danger-text:#ffd8d2;--thumb-bg:#171b17;--font:'Aptos','Bahnschrift','Microsoft YaHei UI',sans-serif;--display:'Bahnschrift SemiCondensed','Aptos Display','Microsoft YaHei UI',sans-serif;--mono:'Cascadia Mono','Cascadia Code',Consolas,'Microsoft YaHei UI',monospace}
.theme-white{--ink:#11160f;--text:#232820;--muted:#697066;--line:#c9c1ad;--line2:#8f8774;--rail:#e3ddca;--panel:#fffaf0;--panel2:#ede7d7;--field:#fffdf7;--bar:#ded8c5;--accent:#141811;--active-fg:#fffaf0;--cyan:#008f7e;--danger-bg:#ffe2dd;--danger-text:#7d1f18;--thumb-bg:#ede7d7}
body{font-family:var(--font);background:var(--bar);color:var(--text)}
button{font:inherit;color:inherit;cursor:pointer}
.shell{height:100%;display:grid;grid-template-rows:42px 1fr;border:1px solid var(--line);background:var(--panel);box-shadow:none}
.bar{display:grid;grid-template-columns:1fr 36px;align-items:center;border-bottom:1px solid var(--line);background:var(--bar);user-select:none}
.drag{height:100%;display:flex;align-items:center;gap:12px;padding:0 12px;color:var(--ink);font-family:var(--display);font-size:24px;font-weight:300;letter-spacing:-.08em;transform-origin:left center}
.drag span{font-family:var(--mono);font-size:10px;font-weight:900;letter-spacing:.02em;color:var(--muted)}
.close{height:38px;border:0;border-left:1px solid var(--line);background:var(--bar);font-weight:1000;font-size:18px;color:var(--muted);border-radius:0}.close:hover{background:var(--danger-bg);color:var(--danger-text)}
.body{display:grid;grid-template-columns:108px 1fr;min-height:0}
.tabs{background:var(--rail);border-right:1px solid var(--line);display:grid;grid-template-rows:auto auto 1fr}.tab{height:52px;border:0;border-bottom:1px solid var(--line);background:transparent;width:100%;font-weight:900;text-align:left;padding:0 14px;color:var(--muted);border-radius:0}.tab:hover{background:color-mix(in srgb,var(--accent) 8%,var(--panel));color:var(--ink)}.tab.active{background:var(--accent);color:var(--active-fg)}
.tab-count{display:block;margin-top:4px;font-family:var(--mono);font-size:10px;font-weight:900;opacity:.72}
.list{overflow:auto;padding:0;scroll-behavior:smooth;background:var(--panel)}.list::-webkit-scrollbar{display:none}.item{min-height:68px;border-bottom:1px solid var(--line);background:var(--panel);padding:11px 14px;cursor:pointer;display:grid;grid-template-columns:1fr;gap:8px}.item.has-thumb{grid-template-columns:58px 1fr;align-items:center}.item:hover{background:color-mix(in srgb,var(--accent) 7%,var(--panel))}
.thumb{width:46px;height:46px;border:1px solid var(--line2);background:var(--thumb-bg);object-fit:cover;border-radius:0}.thumb-fail{display:flex;align-items:center;justify-content:center;color:var(--muted);font-size:10px;font-weight:900}
.meta{font-family:var(--mono);font-size:10px;font-weight:900;color:var(--muted);text-transform:uppercase;margin-bottom:5px}
.title{font-weight:1000;font-size:13px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-family:var(--mono);color:var(--ink)}.sub{margin-top:5px;color:var(--muted);font-weight:800;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-family:var(--mono);font-size:12px}
.empty{height:100%;display:flex;align-items:center;justify-content:center;color:var(--muted);font-weight:900;font-family:var(--mono)}
</style></head><body><div id="app"></div><script>
const data={{payload}};
let tab=data.clipboard.length?'clipboard':'phrases';
const send=m=>chrome.webview.postMessage(m);
const text=data.language==='en'?{clipboard:'Clipboard',phrases:'Phrases',empty:'No available items',image:'Image',quick:'QUICK'}:{clipboard:'剪贴板',phrases:'短语',empty:'没有可用内容',image:'图片',quick:'快捷'};
function esc(v){return String(v??'').replace(/[&<>"]/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[m]))}
function render(){document.getElementById('app').innerHTML=`<div class="shell ${data.theme==='white'?'theme-white':'theme-dark'}"><div class="bar"><div class="drag" onmousedown="send({type:'drag'})">COPY_OS <span>${text.quick}</span></div><button class="close" title="Close" onclick="send({type:'close'})">×</button></div><div class="body"><div class="tabs">${tabButton('clipboard',text.clipboard,data.clipboard.length)}${tabButton('phrases',text.phrases,data.phrases.length)}</div><div class="list">${items()}</div></div></div>`}
function tabButton(id,label,count){return `<button data-tab="${id}" class="tab ${tab===id?'active':''}" onclick="setTab('${id}')">${label}<span class="tab-count">${String(count).padStart(2,'0')}</span></button>`}
function setTab(id){tab=id;document.querySelectorAll('.tab').forEach(button=>button.classList.toggle('active',button.dataset.tab===id));document.querySelector('.list').innerHTML=items()}
function items(){const arr=tab==='clipboard'?data.clipboard:data.phrases;if(!arr.length)return `<div class="empty">${text.empty}</div>`;return arr.map(item).join('')}
function item(x){const image=tab==='clipboard'&&x.Kind==='Image';const thumb=image?(x.PreviewUri?`<img class="thumb" src="${esc(x.PreviewUri)}" onerror="this.replaceWith(Object.assign(document.createElement('div'),{className:'thumb thumb-fail'}))">`:`<div class="thumb thumb-fail"></div>`):'';if(image)return `<div class="item has-thumb" onclick="send({type:'copyClipboard',id:'${x.Id}'})">${thumb}<div><div class="meta">${text.clipboard}</div><div class="title">${text.image}</div></div></div>`;return `<div class="item" onclick="send({type:'${tab==='clipboard'?'copyClipboard':'copyPhrase'}',id:'${x.Id}'})"><div><div class="meta">${tab==='clipboard'?text.clipboard:text.phrases}</div><div class="title">${esc(tab==='clipboard'?x.Title:(x.Description||x.Title||x.Content))}</div><div class="sub">${esc(tab==='clipboard'?x.Content:x.Content)}</div></div></div>`}
render();
window.addEventListener('keydown',event=>{if(event.key==='Escape')send({type:'close'})});
</script></body></html>
""";
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
}
