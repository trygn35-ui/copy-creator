namespace CopyCreator;

internal static class AppShellHtml
{
    /// <summary>
    /// WebView2 主界面。所有可见控件都由 HTML/CSS 绘制，避免露出 Windows 原生控件样式。
    /// </summary>
    public const string Value = """
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root{
  --ink:#f4f0e6;--text:#dfded2;--muted:#9aa197;--dim:#687167;--line:#343b33;--line2:#626b60;
  --canvas:#141811;--body:#0d100c;--rail:#181d15;--main:#20261d;--bar:#11160f;--panel:#1d231b;--panel2:#273024;--field:#11170f;--slab:#303a2e;--slab2:#3c4739;
  --accent:#f0cf63;--accent-strong:#f5dc83;--active-fg:#11160f;--cyan:#6bd8c5;--cyan-dark:#183f39;--red:#eb6b5f;
  --blue:#67a9ff;--yellow:#f0bd44;--green:#75d58e;--violet:#b58dff;
  --danger-bg:#44201e;--danger-text:#ffd8d2;--thumb-bg:#171b17;--soft-line:#253025;--placeholder:#657166;--footer:#151a14;--scroll-thumb:#70796d;--button-primary:#f4db78;--button-primary-text:#11160f;
  --row:96px;--font:'Aptos','Bahnschrift','Microsoft YaHei UI',sans-serif;--display:'Bahnschrift SemiCondensed','Aptos Display','Microsoft YaHei UI',sans-serif;--mono:'Cascadia Mono','Cascadia Code','Consolas','Microsoft YaHei UI',monospace;
}
*{box-sizing:border-box}
html,body,#app{width:100%;height:100%;margin:0;overflow:hidden}
body{font-family:var(--font);background:var(--body);color:var(--text);font-size:14px;letter-spacing:0}
button,input,textarea,select{font:inherit;color:inherit}
button{cursor:pointer}
svg{display:block}
.theme-white{
  --ink:#11160f;--text:#232820;--muted:#697066;--dim:#9a9585;--line:#c9c1ad;--line2:#8f8774;
  --canvas:#f1eee3;--body:#e9e5d7;--rail:#e3ddca;--main:#f8f5eb;--bar:#ded8c5;--panel:#fffaf0;--panel2:#ede7d7;--field:#fffdf7;--slab:#ded6c2;--slab2:#d0c7b0;
  --accent:#141811;--accent-strong:#141811;--active-fg:#fffaf0;--cyan:#008f7e;--cyan-dark:#d7eee8;--red:#b84c43;
  --danger-bg:#ffe2dd;--danger-text:#7d1f18;--thumb-bg:#ede7d7;--soft-line:#ded6c2;--placeholder:#8c887a;--footer:#e2dccb;--scroll-thumb:#7f7867;--button-primary:#141811;--button-primary-text:#fffaf0;
}
.shell{height:100vh;display:grid;grid-template-columns:132px minmax(0,1fr);background:var(--canvas);border:1px solid var(--line);overflow:hidden}
.rail{position:relative;display:grid;grid-template-rows:102px 1fr auto;background:var(--rail);border-right:1px solid var(--line)}
.rail:before{content:"";position:absolute;left:12px;top:102px;bottom:72px;width:1px;background:color-mix(in srgb,var(--line) 42%,transparent)}
.rail:after{content:"";position:absolute;right:0;top:0;bottom:0;width:1px;background:var(--line)}
.brand{position:relative;padding:26px 10px 0;border-bottom:1px solid color-mix(in srgb,var(--line) 48%,transparent);text-align:center}
.brand:after{content:"";position:absolute;left:12px;right:12px;bottom:-1px;height:1px;background:var(--line)}
.brand-title{display:block;width:100%;color:var(--ink);font-family:var(--display);font-size:31px;font-weight:300;line-height:.76;letter-spacing:-.08em;text-align:center;transform:scaleY(1.18);transform-origin:center center}
.brand-version{margin-top:12px;color:var(--muted);font-family:var(--mono);font-size:10px;font-weight:800;letter-spacing:.02em}
.nav{position:relative;padding:18px 8px;display:flex;flex-direction:column;gap:7px}
.nav button{position:relative;height:33px;display:grid;grid-template-columns:22px 1fr;align-items:center;gap:8px;border:1px solid transparent;background:transparent;text-align:left;padding:0 8px;color:var(--muted);font-size:12px;font-weight:800;border-radius:0}
.nav button{transition:background .28s ease,border-color .28s ease,color .28s ease,transform .28s ease}
.nav button:hover{background:color-mix(in srgb,var(--accent) 8%,var(--panel));color:var(--ink);border-color:var(--line2);transform:translateX(2px)}
.nav button.active{background:color-mix(in srgb,var(--accent) 18%,var(--panel));color:var(--ink);border-color:var(--accent)}
.nav button.active:after{content:"";position:absolute;right:-1px;top:-1px;bottom:-1px;width:2px;background:var(--accent);animation:activeRail 1.7s ease-in-out infinite}
.nav-icon{width:15px;height:15px;color:currentColor}
.rail-foot{position:relative;padding:0 10px 18px;display:grid;gap:14px}
.rail-foot:before{content:"";height:1px;background:var(--soft-line);width:100%;display:block}
.log-btn{display:flex;align-items:center;gap:9px;border:0;background:transparent;color:var(--muted);font-size:11px;font-weight:800;padding:0;text-align:left}
.log-btn:hover{color:var(--ink)}
.main{min-width:0;min-height:0;height:100%;display:grid;grid-template-rows:42px minmax(0,1fr);background:var(--main);overflow:hidden}
.titlebar{display:grid;grid-template-columns:1fr auto 42px;align-items:center;background:var(--bar);border-bottom:1px solid var(--line)}
.drag{height:42px;display:flex;align-items:center;gap:22px;padding:0 14px;user-select:none;font-family:var(--mono);font-weight:1000}
.module-name{position:relative;font-size:13px;color:var(--ink);letter-spacing:.06em;padding-right:18px}
.module-name:after{content:"";position:absolute;right:0;top:4px;bottom:4px;width:1px;background:var(--line2)}
.node-label{font-size:11px;color:var(--muted);letter-spacing:.03em}
.engine-status{display:flex;align-items:center;gap:8px;color:var(--text);font-size:11px;font-weight:900;padding-right:18px}
.status-dot{width:7px;height:7px;background:var(--cyan);display:inline-block;animation:statusPulse 1.8s ease-in-out infinite}
.close{height:42px;width:42px;border:0;border-left:1px solid var(--line);background:var(--bar);color:var(--muted);font-size:22px;font-weight:900;line-height:1;border-radius:0}
.close:hover{background:var(--danger-bg);color:var(--danger-text)}
.content{position:relative;min-width:0;min-height:0;height:calc(100vh - 44px);display:block;overflow:hidden;background:var(--panel)}
.hero,.hero-copy,.eyebrow,.hero-side{display:none}
.page{position:relative;inset:auto;width:100%;min-height:0;height:100%;display:grid;grid-template-rows:auto minmax(0,1fr);background:var(--panel);overflow:hidden}
.page:before{content:"";position:absolute;left:14px;right:14px;top:0;height:1px;background:var(--soft-line);pointer-events:none}
.toolbar{display:grid;grid-template-columns:minmax(280px,380px) 1fr;gap:24px;padding:13px 14px;border-bottom:1px solid var(--line);background:var(--main);align-items:center}
.settings-toolbar{grid-template-columns:1fr}
.search,input,textarea{border:1px solid var(--line);background:var(--field);outline:none;padding:8px 10px;border-radius:0;color:var(--text)}
.search:focus,input:focus,textarea:focus{background:var(--field);border-color:var(--accent);box-shadow:none}
.chips{display:flex;gap:8px;align-items:stretch;flex-wrap:wrap}
.chip,.action,.link-btn{border:1px solid var(--line2);background:var(--panel2);padding:0 12px;min-height:28px;font-weight:1000;border-radius:0;color:var(--text);font-family:var(--mono);font-size:11px}
.chip,.action,.link-btn,.icon-btn,.tool-btn,.repo-new,.log-btn{transition:background .26s ease,border-color .26s ease,color .26s ease,transform .26s ease}
.chip:hover,.action:hover,.link-btn:hover{background:color-mix(in srgb,var(--accent) 10%,var(--panel));color:var(--ink);border-color:var(--accent);transform:translateY(-1px)}
.chip.text{color:var(--blue);border-color:color-mix(in srgb,var(--blue) 52%,var(--line))}
.chip.link{color:var(--yellow);border-color:color-mix(in srgb,var(--yellow) 52%,var(--line))}
.chip.image{color:var(--green);border-color:color-mix(in srgb,var(--green) 52%,var(--line))}
.chip.file{color:var(--violet);border-color:color-mix(in srgb,var(--violet) 52%,var(--line))}
.chip.active{background:var(--accent-strong);color:var(--active-fg);border-color:var(--accent)}
.chip.text.active{background:var(--blue);color:#050507;border-color:var(--blue)}
.chip.link.active{background:var(--yellow);color:#050507;border-color:var(--yellow)}
.chip.image.active{background:var(--green);color:#050507;border-color:var(--green)}
.chip.file.active{background:var(--violet);color:#050507;border-color:var(--violet)}
.list{margin:0;background:var(--panel);overflow-y:auto;overflow-x:hidden;scroll-behavior:smooth;scrollbar-gutter:stable;padding:0}
.list::-webkit-scrollbar{width:7px}
.list::-webkit-scrollbar-track{background:var(--canvas);border-left:1px solid var(--line)}
.list::-webkit-scrollbar-thumb{background:var(--line2);border-left:1px solid var(--canvas)}
.list::-webkit-scrollbar-thumb:hover{background:var(--scroll-thumb)}
.hide-scroll{overflow:auto;scroll-behavior:smooth}.hide-scroll::-webkit-scrollbar{display:none}
.row{display:grid;grid-template-columns:2px 1fr auto;gap:28px;min-height:var(--row);border:0;border-bottom:1px solid var(--line);background:var(--panel);margin:0;overflow:hidden}
.row.has-thumb{grid-template-columns:2px 1fr auto;min-height:128px}
.row:last-child{border-bottom:1px solid var(--line)}.row:hover{background:color-mix(in srgb,var(--accent) 5%,var(--panel))}
.stripe{width:2px;margin:0;background:#777}.stripe.text{background:var(--blue)}.stripe.link{background:var(--yellow)}.stripe.image{background:var(--green)}.stripe.file{background:var(--violet)}.row:not(:hover) .stripe{opacity:.9}
.thumb{width:86px;height:86px;margin:18px 0 0;border:1px solid var(--line);background:var(--thumb-bg);object-fit:cover;border-radius:0;display:block}
.thumb-fail{display:flex;align-items:center;justify-content:center;color:var(--muted);font-weight:900;font-size:11px}
.row-main{min-width:0;padding:22px 0;cursor:pointer}.image-main{padding:22px 0 18px}
.meta{display:flex;gap:12px;align-items:center;flex-wrap:wrap;font-size:12px;font-weight:800;color:var(--muted);font-family:var(--mono);text-transform:uppercase}
.type-pill{border:0;padding:0;background:transparent;color:var(--ink);font-size:13px;letter-spacing:.02em}
.type-pill.text{color:var(--blue)}.type-pill.link{color:var(--yellow)}.type-pill.image{color:var(--green)}.type-pill.file{color:var(--violet)}
.title-text{margin-top:10px;font-weight:1000;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-size:22px;font-family:var(--mono);color:var(--ink)}
.preview{margin-top:8px;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-family:var(--mono);font-size:16px}
.row-actions{display:flex;align-items:center;gap:8px;padding-right:12px}
.icon-btn{width:28px;height:28px;border:1px solid var(--line2);background:var(--panel2);font-weight:1000;border-radius:0;color:var(--muted)}
.icon-btn:hover{background:var(--slab);color:var(--ink)}.icon-btn.danger:hover{background:var(--danger-bg);color:var(--danger-text)}
.empty{height:100%;display:flex;align-items:center;justify-content:center;text-align:center;font-weight:900;color:var(--muted);background:var(--panel);border:0;font-family:var(--mono)}
.repo-page{grid-template-rows:auto auto minmax(0,1fr)}
.repo-toolbar{position:relative;display:grid;grid-template-columns:1fr auto;gap:14px;padding:16px 14px 12px;border-bottom:1px solid var(--line);background:var(--main)}
.repo-toolbar:after{content:"";position:absolute;left:14px;right:14px;bottom:0;height:1px;background:var(--soft-line)}
.sys-kicker{font-family:var(--mono);font-size:11px;font-weight:900;color:var(--ink);letter-spacing:.08em;margin-bottom:8px}
.repo-titleline{display:flex;align-items:end;gap:12px;margin-bottom:8px}
.repo-titleline strong{font-size:28px;line-height:.88;color:var(--ink);letter-spacing:-.05em}
.repo-titleline span{font-family:var(--mono);font-size:10px;color:var(--muted);font-weight:900;letter-spacing:.04em}
.repo-filters{display:flex;gap:5px;flex-wrap:wrap}
.repo-filter{height:22px;border:1px solid var(--line2);background:var(--panel2);color:var(--text);padding:0 9px;font-family:var(--mono);font-size:10px;font-weight:1000;border-radius:0}
.repo-filter.active{background:var(--accent-strong);color:var(--active-fg);border-color:var(--accent)}
.repo-actions{display:grid;grid-template-columns:220px 98px;gap:12px;align-items:start}
.repo-search{height:30px;width:100%;background:var(--field);border:1px solid var(--line);color:var(--text);font-family:var(--mono);font-size:11px}
.repo-new{height:30px;border:1px solid var(--button-primary);background:var(--button-primary);color:var(--button-primary-text);font-size:11px;font-weight:900;border-radius:0}
.repo-new:hover{background:color-mix(in srgb,var(--button-primary) 78%,var(--panel));border-color:var(--accent)}
.phrase-composer{display:grid;grid-template-columns:minmax(140px,.8fr) minmax(220px,1fr) 82px;gap:8px;padding:10px 14px;border-bottom:1px solid var(--line);background:var(--canvas)}
.phrase-composer input{min-width:0;height:30px;background:var(--field);font-family:var(--mono);font-size:11px}
.repo-grid{min-height:0;padding:12px 14px;display:grid;grid-template-columns:repeat(2,minmax(220px,1fr));grid-auto-rows:minmax(110px,auto);gap:12px;background:var(--panel)}
.repo-card{position:relative;min-width:0;border:1px solid var(--line);background:var(--panel2);display:grid;grid-template-rows:28px 1fr;cursor:pointer;border-radius:0;transition:background .18s ease,border-color .18s ease,transform .18s ease,opacity .18s ease}
.repo-card:before{content:"";position:absolute;left:-1px;top:-1px;bottom:-1px;width:2px;background:var(--line2);opacity:.85}
.repo-card:hover{border-color:var(--accent);background:color-mix(in srgb,var(--accent) 7%,var(--panel))}
.repo-card.dragging{opacity:.58;border-color:var(--accent);background:color-mix(in srgb,var(--accent) 14%,var(--panel))}
.repo-card.drag-target:after{content:"";position:absolute;left:0;right:0;top:-3px;height:3px;background:var(--accent)}
.repo-card-head{display:grid;grid-template-columns:1fr auto;align-items:center;background:var(--slab);border-bottom:1px solid var(--line);padding:0 8px;color:var(--muted);font-family:var(--mono);font-size:10px;font-weight:1000;letter-spacing:.04em}
.repo-card-delete{width:22px;height:22px;border:0;background:transparent;color:var(--muted);font-size:16px;font-weight:1000;border-radius:0}
.repo-card-delete:hover{background:var(--danger-bg);color:var(--danger-text)}
.repo-card-body{padding:12px 12px 13px;min-width:0}
.repo-card-title{color:var(--ink);font-size:20px;font-weight:1000;line-height:1.05;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.repo-card-copy{margin-top:9px;color:var(--muted);font-family:var(--mono);font-size:12px;font-weight:800;line-height:1.4;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}
.repo-add-card{border:1px dashed var(--line2);background:var(--panel2);display:flex;align-items:center;justify-content:center;min-height:104px;color:var(--muted);font-weight:1000;cursor:pointer}
.repo-add-card:hover{border-color:var(--accent);color:var(--ink);background:color-mix(in srgb,var(--accent) 8%,var(--panel))}
.repo-plus{font-size:28px;line-height:1;text-align:center}.repo-plus-label{margin-top:8px;font-size:12px}
.translate-page{grid-template-rows:42px minmax(0,1fr)}
.translate-controlbar{position:relative;display:grid;grid-template-columns:auto auto minmax(150px,210px) 1fr auto;align-items:center;gap:14px;border-bottom:1px solid var(--line);background:var(--main);padding:0 14px}
.translate-controlbar:after{content:"";position:absolute;left:50%;top:0;bottom:0;width:1px;background:var(--soft-line)}
.language-cluster{display:flex;align-items:center;gap:12px;font-family:var(--mono);font-size:11px;font-weight:900;color:var(--text)}
.language-cluster>span{color:var(--muted);font-size:10px;letter-spacing:.05em}
.lang-button{height:24px;border:0;background:transparent;color:var(--ink);font-weight:900;padding:0;font-family:var(--mono)}
.lang-button:hover{color:var(--accent)}
.language-drawer{position:relative;z-index:4}
.drawer-button{height:28px;width:100%;border:1px solid var(--line2);background:var(--panel2);color:var(--ink);font-family:var(--mono);font-size:11px;font-weight:1000;text-align:left;padding:0 10px;border-radius:0}
.drawer-button:hover{border-color:var(--accent);background:color-mix(in srgb,var(--accent) 8%,var(--panel))}
.drawer-panel{position:absolute;left:0;right:0;top:34px;border:1px solid var(--line2);background:var(--panel);display:grid;grid-template-columns:1fr;z-index:10;animation:drawerDrop .28s ease-out both}
.drawer-panel .chip{height:30px;border:0;border-bottom:1px solid var(--line);text-align:left;background:transparent;color:var(--text)}
.drawer-panel .chip:last-child{border-bottom:0}
.drawer-panel .chip.active{background:var(--accent-strong);color:var(--active-fg)}
.auto-toggle{height:22px;border:1px solid var(--line);background:var(--slab);color:var(--muted);display:flex;align-items:center;gap:8px;padding:0 9px;font-family:var(--mono);font-size:10px;font-weight:1000;border-radius:0}
.auto-led{width:7px;height:7px;background:var(--cyan);display:inline-block;animation:statusPulse 1.8s ease-in-out infinite}
.translate-actions{display:flex;gap:8px;align-items:center;justify-content:flex-end}
.tool-btn{width:auto;height:28px;border:1px solid transparent;background:transparent;color:var(--muted);display:grid;place-items:center;border-radius:0;padding:0 9px;font-family:var(--mono);font-size:11px;font-weight:900}
.tool-btn:hover{border-color:var(--accent);color:var(--ink);background:color-mix(in srgb,var(--accent) 8%,var(--panel))}
.translate-primary{height:28px;border:1px solid var(--button-primary);background:var(--button-primary);color:var(--button-primary-text);padding:0 12px;font-size:11px;font-weight:900;border-radius:0}
.translate-primary:hover{background:color-mix(in srgb,var(--button-primary) 78%,var(--panel));border-color:var(--accent)}
.translate-primary:disabled{cursor:wait;opacity:.86}
.translate-primary.loading{position:relative;overflow:hidden;padding-right:28px}
.translate-primary.loading:after{content:"";position:absolute;right:10px;top:11px;width:5px;height:5px;background:currentColor;animation:loadingTick 1s steps(4,end) infinite}
.translation-workbench{min-height:0;display:grid;grid-template-columns:1fr 1fr;background:var(--panel)}
.translate-pane{min-width:0;min-height:0;display:grid;grid-template-rows:minmax(0,1fr) auto;position:relative}
.translate-pane.source{border-right:1px dashed var(--line2)}
.translate-pane.source:after{content:"";position:absolute;right:-1px;top:0;bottom:0;width:1px;background:linear-gradient(to bottom,transparent 0,var(--cyan) 42%,var(--cyan) 58%,transparent 100%);background-size:1px 210px;background-repeat:no-repeat;animation:scanBounce 3.4s ease-in-out infinite}
.translate-pane textarea{width:100%;height:100%;resize:none;border:0;background:var(--panel);color:var(--text);box-shadow:none;padding:26px 28px;line-height:1.6;font-family:var(--mono);font-size:14px}
.translate-pane textarea::placeholder{color:var(--placeholder);font-size:16px;font-weight:900}
.translate-pane textarea:focus{box-shadow:none;background:var(--panel)}
.pane-counter{height:32px;display:flex;align-items:center;justify-content:flex-end;padding:0 18px;color:var(--muted);font-family:var(--mono);font-size:10px;font-weight:900}
.result-metrics{margin:0 14px 14px;border:1px solid var(--line);background:var(--panel2);display:grid;grid-template-columns:repeat(3,1fr)}
.metric{padding:6px 8px;border-right:1px solid var(--line);font-family:var(--mono);font-size:10px;font-weight:1000;color:var(--muted)}
.metric:last-child{border-right:0}.metric strong{display:block;margin-top:5px;color:var(--ink);font-size:11px}
.meter{display:inline-block;width:58px;height:4px;background:var(--slab2);margin:0 16px 0 6px;vertical-align:middle}.meter span{display:block;width:12%;height:100%;background:var(--cyan)}
.settings-page{height:100%;min-height:0;grid-template-rows:auto minmax(0,1fr);overflow:hidden}
.settings-scroll{height:100%;min-height:0;overflow-y:auto;overflow-x:hidden;scroll-behavior:smooth}
.settings-scroll::-webkit-scrollbar{display:none}
.settings{min-height:min-content;margin:0;padding-bottom:80px;display:grid;grid-template-columns:repeat(2,minmax(260px,1fr));align-content:start;gap:0;border-top:1px solid var(--line);background:linear-gradient(90deg,transparent calc(50% - .5px),var(--line) calc(50% - .5px),var(--line) calc(50% + .5px),transparent calc(50% + .5px))}
.section{border:0;border-right:1px solid var(--line);border-bottom:1px solid var(--line);background:var(--panel);min-width:0;border-radius:0;overflow:hidden;box-shadow:none}
.section h3{margin:0;padding:12px 14px;border-bottom:1px solid var(--line);background:var(--panel2);font-size:13px;font-family:var(--mono);text-transform:uppercase}
.section:nth-child(2n) h3,.section:nth-child(3n) h3{background:var(--slab)}
.field{display:grid;grid-template-columns:132px 1fr;gap:10px;align-items:center;padding:10px 12px;border-bottom:1px solid var(--line)}
.field:last-child{border-bottom:0}.field label{font-weight:1000;color:var(--ink);font-family:var(--mono);font-size:11px;text-transform:uppercase}
.toggle{display:flex;align-items:center;justify-content:space-between;padding:12px;border-bottom:1px solid var(--line);font-weight:1000;cursor:pointer;color:var(--ink);font-family:var(--mono);font-size:11px}
.toggle:last-child{border-bottom:0}.switch{width:42px;height:18px;border:1px solid var(--line2);background:var(--field);position:relative;border-radius:0}.switch:after{content:"";position:absolute;width:12px;height:12px;left:2px;top:2px;background:var(--muted);border-radius:0;transition:left .14s ease-out,background .14s ease-out}.toggle.on .switch{background:var(--cyan-dark);border-color:var(--accent)}.toggle.on .switch:after{left:26px;background:var(--accent-strong)}
.inline-editor{position:absolute;left:164px;top:54px;width:280px;border:1px solid var(--line2);background:var(--panel);z-index:30;display:grid;grid-template-columns:1fr auto auto;gap:6px;padding:8px;animation:drawerDrop .22s ease-out both}
.inline-editor input{height:30px;font-family:var(--mono);font-size:11px}
.toast{position:absolute;right:18px;bottom:18px;border:1px solid var(--accent-strong);background:var(--accent-strong);color:var(--active-fg);padding:10px 14px;font-weight:1000;display:none;box-shadow:none;z-index:20;border-radius:0;font-family:var(--mono)}
.toast.show{display:block;animation:pop .32s ease-out}
@keyframes pop{0%{transform:scale(.98);opacity:.2}100%{transform:scale(1);opacity:1}}
@keyframes statusPulse{0%,100%{opacity:.45}50%{opacity:1}}
@keyframes activeRail{0%,100%{opacity:.55}50%{opacity:1}}
@keyframes scanBounce{0%,100%{background-position:0 0;opacity:.35}50%{background-position:0 100%;opacity:1}}
@keyframes drawerDrop{from{opacity:0;clip-path:inset(0 0 100% 0)}to{opacity:1;clip-path:inset(0)}}
@keyframes rowPulse{0%{background:color-mix(in srgb,var(--accent) 18%,var(--panel))}100%{background:var(--panel)}}
@keyframes loadingTick{0%{box-shadow:0 0 0 currentColor}33%{box-shadow:7px 0 0 currentColor}66%{box-shadow:7px 0 0 currentColor,14px 0 0 currentColor}100%{box-shadow:0 0 0 currentColor}}
.compact{--gap:8px;--pad:10px;--row:66px}.relaxed{--gap:16px;--pad:18px;--row:96px}
@media(max-width:820px){.shell{grid-template-columns:132px minmax(0,1fr)}.repo-grid{grid-template-columns:1fr}.toolbar,.repo-toolbar,.translate-controlbar{grid-template-columns:1fr}.settings,.translation-workbench{grid-template-columns:1fr}.translate-pane.source{border-right:0;border-bottom:1px dashed var(--line2)}.repo-actions{grid-template-columns:1fr}.brand-title{font-size:18px}}
</style>
</head>
<body>
<div id="app"></div>
<script>
const send=(message)=>chrome.webview.postMessage(message);
const get=(obj,...keys)=>{for(const key of keys){if(obj&&Object.prototype.hasOwnProperty.call(obj,key))return obj[key]}return undefined};
let state={clipboard:[],groups:[],phrases:[],settings:{}};
let tab='clipboard',filter='all',group='all',query='',translation='',recordingHotkey=false,isTranslating=false;
let scrollMemory={};
let phraseQuery='';
let languageDrawerOpen=false;
let draggedPhraseId='';
let phraseGroupEditor=null;
const typeNames={all:'全部',text:'文本',link:'链接',image:'图片',file:'文件'};
const typeNamesEn={all:'All',text:'Text',link:'Link',image:'Image',file:'File'};
const kindMap={0:'text',1:'link',2:'image',3:'file',text:'text',link:'link',image:'image',file:'file',Text:'text',Link:'link',Image:'image',File:'file'};
const languageOptions=[['Chinese','中文'],['English','英文'],['Japanese','日文'],['Korean','韩文'],['French','法文'],['German','德文'],['Spanish','西班牙文']];
const i18n={
  zh:{clipboard:'剪贴板',phrases:'快捷短语',translate:'翻译',settings:'设置',portable:'便携版',search:'搜索内容、链接或文件名',emptyClipboard:'还没有记录。复制一段文字、图片或文件后这里会自动出现。',copied:'已复制',copyPhrase:'短语已复制',paste:'粘贴',copyResult:'复制结果',startTranslate:'开始翻译',translateTo:'翻译成',input:'输入',result:'结果',inputPlaceholder:'在这里输入或粘贴要翻译的内容',resultPlaceholder:'翻译结果会显示在这里',clickPhrase:'点击短语即复制',addCustom:'添加自定义短语',allPhrases:'全部短语',phraseHint:'中文提示，例如 查看上下文',phraseContent:'复制内容，例如 /context',add:'添加',emptyPhrase:'这个分组里还没有短语。',dataDir:'数据目录',logs:'日志目录',general:'常规',language:'语言',startup:'开机启动',hideClose:'关闭按钮隐藏到托盘',clipboardSection:'剪贴板',saveDays:'保存天数',maxItems:'最大记录条数',recordText:'记录文本',recordLinks:'记录链接',recordImages:'记录图片',recordFiles:'记录文件',sensitive:'疑似密钥检测',cache:'缓存',fileMax:'单个文件上限 MB',cacheMax:'文件缓存总量 GB',quickClipboard:'浮层显示剪贴板',quickPhrases:'浮层显示短语',quickHotkey:'快捷浮窗快捷键',recordHotkey:'录制快捷键',clearHotkey:'清除快捷键',recordingHotkey:'请按下要用的键或鼠标侧键',notSet:'未设置',api:'API 地址',apiKey:'API Key',model:'模型名称',test:'连接测试',testApi:'测试 API',testingApi:'正在测试 API',text:'文本',link:'链接',image:'图片',file:'文件',all:'全部',pinned:'已置顶',cached:'已缓存',pathOnly:'仅路径',invalid:'失效',customPhrase:'自定义短语',noText:'剪贴板里没有文本',pasted:'已粘贴到输入区',fillTranslate:'先输入要翻译的内容',noResult:'没有可复制的翻译结果',translated:'翻译完成',done:'完成'},
  en:{clipboard:'Clipboard',phrases:'Phrases',translate:'Translate',settings:'Settings',portable:'Portable',search:'Search content, links, or filenames',emptyClipboard:'No records yet. Copy text, images, or files to see them here.',copied:'Copied',copyPhrase:'Phrase copied',paste:'Paste',copyResult:'Copy result',startTranslate:'Translate',translateTo:'Translate to',input:'Input',result:'Result',inputPlaceholder:'Type or paste text to translate',resultPlaceholder:'Translation result appears here',clickPhrase:'Click a phrase to copy',addCustom:'Add custom phrase',allPhrases:'All phrases',phraseHint:'Hint, e.g. Review code',phraseContent:'Copied content, e.g. /review',add:'Add',emptyPhrase:'No phrases in this group.',dataDir:'Data folder',logs:'Logs',general:'General',language:'Language',startup:'Start on boot',hideClose:'Close hides to tray',clipboardSection:'Clipboard',saveDays:'Save days',maxItems:'Max items',recordText:'Record text',recordLinks:'Record links',recordImages:'Record images',recordFiles:'Record files',sensitive:'Sensitive detection',cache:'Cache',fileMax:'Single file limit MB',cacheMax:'File cache total GB',quickClipboard:'Overlay shows clipboard',quickPhrases:'Overlay shows phrases',quickHotkey:'Overlay hotkey',recordHotkey:'Record hotkey',clearHotkey:'Clear hotkey',recordingHotkey:'Press a key or mouse side button',notSet:'Not set',api:'API URL',apiKey:'API Key',model:'Model name',test:'Connection test',testApi:'Test API',testingApi:'Testing API',text:'Text',link:'Link',image:'Image',file:'File',all:'All',pinned:'Pinned',cached:'Cached',pathOnly:'Path only',invalid:'missing',customPhrase:'Custom phrase',noText:'Clipboard has no text',pasted:'Pasted into input',fillTranslate:'Enter text first',noResult:'No result to copy',translated:'Translated',done:'Done'}
};
function esc(value){return String(value??'').replace(/[&<>"]/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[m]))}
function setting(name,fallback){const value=get(state.settings,name,name[0].toUpperCase()+name.slice(1))??fallback;return name==='theme'&&value==='light'?'dark':value}
function lang(){return setting('language','zh')==='en'?'en':'zh'}
function t(key){return i18n[lang()][key]||i18n.zh[key]||key}
function kindLabel(kind){return lang()==='en'?(typeNamesEn[kind]||kind):(typeNames[kind]||kind)}
function themeClass(){return setting('theme','dark')==='white'?'theme-white':'theme-dark'}
const navMeta={
  clipboard:{zh:'剪贴板',en:'Clipboard',icon:'<path d="M8 4h8v3H8z"/><path d="M7 6h10v14H7z"/>'},
  phrases:{zh:'常用短语',en:'Phrases',icon:'<path d="M4 6h16v10H8l-4 4z"/><path d="M8 10h8M8 13h5"/>'},
  translate:{zh:'智能翻译',en:'Translate',icon:'<path d="M4 6h8M8 4v2m3 0c-.7 3.7-2.7 6.5-6 8"/><path d="M6 10c1.1 1.8 2.8 3.1 5 4"/><path d="M14 20l3.5-8L21 20M15.2 17h4.6"/>'},
  settings:{zh:'系统设置',en:'Settings',icon:'<path d="M12 8a4 4 0 100 8 4 4 0 000-8z"/><path d="M3 12h3m12 0h3M12 3v3m0 12v3M5.6 5.6l2.1 2.1m8.6 8.6l2.1 2.1M18.4 5.6l-2.1 2.1m-8.6 8.6l-2.1 2.1"/>'}
};
function navLabel(id){const meta=navMeta[id];return lang()==='en'?meta.en:meta.zh}
function iconSvg(id){return `<svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="square" stroke-linejoin="miter">${navMeta[id].icon}</svg>`}
function moduleCode(){if(tab==='clipboard')return 'CLIP_SYNC';if(tab==='phrases')return 'LOCAL_LIBRARY';if(tab==='translate')return 'NEURAL_TRANSLATE';return 'SYSTEM_CONFIG'}
function render(){
  rememberScroll();
  const previousTab=tab;
  document.getElementById('app').innerHTML=`<div class="shell ${themeClass()}"><aside class="rail"><div class="brand"><div class="brand-title">COPY_OS</div><div class="brand-version">V1.0.4_STABLE</div></div><nav class="nav">${nav('clipboard')}${nav('phrases')}${nav('translate')}${nav('settings')}</nav><div class="rail-foot"><button class="log-btn" onclick="send({type:'openLogs'})">${iconSvg('settings')}<span>${t('logs')}</span></button></div></aside><main class="main"><div class="titlebar"><div class="drag" onmousedown="send({type:'drag'})"><span class="module-name">${moduleCode()}</span><span class="node-label">NODE: ${lang()==='en'?'TR-42_EN':'TR-42_ZH'}</span></div><div class="engine-status"><span class="status-dot"></span>${lang()==='en'?'ENGINE ONLINE':'引擎在线'}</div><button class="close" title="Close" onclick="send({type:'close'})">×</button></div><div class="content">${page()}</div></main><div id="groupEditorHost">${phraseGroupEditorHtml()}</div><div id="toast" class="toast"></div></div>`;
  restoreScroll(previousTab);
}
function nav(id){return `<button class="${tab===id?'active':''}" onclick="rememberScroll();tab='${id}';render()">${iconSvg(id)}<span>${navLabel(id)}</span></button>`}
function phraseGroupEditorHtml(){
  if(!phraseGroupEditor)return '';
  return `<div class="inline-editor"><input id="groupNameInput" value="${esc(phraseGroupEditor.name||'')}" placeholder="${lang()==='en'?'Group name':'分类名称'}"><button class="action" onclick="submitPhraseGroupEditor()">${t('done')}</button><button class="action" onclick="phraseGroupEditor=null;renderPhraseGroupEditor()">×</button></div>`;
}
function countLabel(){if(tab==='clipboard')return `${state.clipboard.length}<br>ITEMS`;if(tab==='phrases')return `${state.phrases.length}<br>PHRASES`;if(tab==='translate')return 'MANUAL<br>ONLY';return 'LIVE<br>SETTINGS'}
function page(){if(tab==='phrases')return phrases();if(tab==='translate')return translate();if(tab==='settings')return settings();return clipboard()}
function clipboard(){
  const items=currentClipboardItems();
  return `<section class="page"><div class="toolbar"><input class="search" value="${esc(query)}" placeholder="${t('search')}" oninput="query=this.value;refreshClipboardListWithoutJump()"><div class="chips" id="clipboardFilters">${clipboardFilterHtml()}</div></div><div class="list" data-scroll-key="clipboard-list">${clipboardListHtml(items)}</div></section>`;
}
function clipboardFilterHtml(){return ['all','text','link','image','file'].map(f=>`<button class="chip ${f} ${filter===f?'active':''}" onclick="setClipboardFilter('${f}')">${kindLabel(f)}</button>`).join('')}
function setClipboardFilter(value){filter=value;const filters=document.getElementById('clipboardFilters');if(filters)filters.innerHTML=clipboardFilterHtml();refreshClipboardListWithoutJump()}
function clipboardListHtml(items){return items.length?items.map(row).join(''):`<div class="empty">${t('emptyClipboard')}</div>`}
function row(item){
  const id=get(item,'Id','id'),kind=normalizeKind(get(item,'Kind','kind')),title=get(item,'Title','title')||typeNames[kind]||kind;
  const content=get(item,'Content','content')||'',pinned=!!get(item,'Pinned','pinned'),cached=!!get(item,'Cached','cached'),time=new Date(get(item,'UpdatedAt','updatedAt')||Date.now()).toLocaleString();
  const previewUri=get(item,'PreviewUri','previewUri')||'';
  const thumb=kind==='image'?(previewUri?`<img class="thumb" src="${esc(previewUri)}" onerror="this.replaceWith(Object.assign(document.createElement('div'),{className:'thumb thumb-fail'}))">`:`<div class="thumb thumb-fail"></div>`):'';
  if(kind==='image') return `<div class="row has-thumb" data-id="${id}"><div class="stripe image"></div><div class="row-main image-main" onclick="send({type:'copyClipboard',id:'${id}'})"><div class="meta"><span class="type-pill image">${kindLabel(kind)}</span><span>${pinned?t('pinned')+' · ':''}${esc(time)}</span></div>${thumb}</div><div class="row-actions"><button class="icon-btn" title="${pinned?'Unpin':'Pin'}" onclick="pinClipboardNow('${id}')">${pinned?'◆':'◇'}</button><button class="icon-btn danger" title="Delete" onclick="deleteClipboardNow('${id}')">×</button></div></div>`;
  const detail=kind==='file'?(cached?t('cached'):t('pathOnly')):content;
  return `<div class="row" data-id="${id}"><div class="stripe ${kind}"></div><div class="row-main" onclick="send({type:'copyClipboard',id:'${id}'})"><div class="meta"><span class="type-pill ${kind}">${kindLabel(kind)}</span><span>${pinned?t('pinned')+' · ':''}${esc(time)}</span><span>${cached?t('cached'):t('pathOnly')}</span></div><div class="title-text">${esc(title)}</div><div class="preview">${esc(detail)}</div></div><div class="row-actions"><button class="icon-btn" title="${pinned?'Unpin':'Pin'}" onclick="pinClipboardNow('${id}')">${pinned?'◆':'◇'}</button><button class="icon-btn danger" title="Delete" onclick="deleteClipboardNow('${id}')">×</button></div></div>`;
}
function normalizeKind(value){return kindMap[value]||kindMap[String(value)]||String(value||'text').toLowerCase()}
function phrases(){
  const selected=String(group);
  const raw=state.phrases.filter(p=>selected==='all'||String(get(p,'GroupId','groupId'))===selected);
  const needle=phraseQuery.trim().toLowerCase();
  const phrases=raw.filter(p=>{
    const text=`${get(p,'Title','title')||''} ${get(p,'Content','content')||''} ${get(p,'Description','description')||''}`.toLowerCase();
    return !needle||text.includes(needle);
  });
  const groupChips=phraseGroupChipsHtml(selected);
  return `<section class="page repo-page"><div class="repo-toolbar"><div><div class="sys-kicker">LOCAL_REPOSITORIES</div><div class="repo-titleline"><strong>${String(state.phrases.length).padStart(2,'0')}</strong><span>${lang()==='en'?'SAVED PHRASES':'条本地短语'}</span></div><div class="repo-filters">${groupChips}</div></div><div class="repo-actions"><input class="repo-search" value="${esc(phraseQuery)}" placeholder="${lang()==='en'?'Search phrases...':'搜索短语库...'}" oninput="phraseQuery=this.value;refreshPhraseGridWithoutJump()"><button class="repo-new" onclick="focusPhraseForm()">⊕ ${t('add')}</button></div></div><div class="phrase-composer"><input id="phraseTitle" placeholder="${t('phraseHint')}"><input id="phraseContent" placeholder="${t('phraseContent')}"><button class="action" onclick="addPhrase()">${t('add')}</button></div><div class="repo-grid hide-scroll" data-scroll-key="phrase-list" ondragover="handlePhraseDragOver(event)" ondragleave="clearPhraseDragTarget(event)" ondrop="dropPhrase(event)">${phrases.length?phrases.map(phraseRow).join(''):`<div class="empty">${t('emptyPhrase')}</div>`}</div></section>`;
}
function phraseGroupChipsHtml(selected=String(group)){return `<button class="repo-filter ${selected==='all'?'active':''}" onclick="setPhraseGroup('all')">${lang()==='en'?'ALL':'全部'}: ${state.phrases.length}</button>${state.groups.map(g=>{const id=String(get(g,'Id','id'));const total=state.phrases.filter(p=>String(get(p,'GroupId','groupId'))===id).length;return `<button class="repo-filter ${selected===id?'active':''}" onclick="setPhraseGroup('${id}')" ondblclick="renamePhraseGroupNow('${id}')">${esc(get(g,'Name','name'))}: ${String(total).padStart(2,'0')}</button>`}).join('')}<button class="repo-filter" onclick="addPhraseGroupNow()">＋ ${lang()==='en'?'GROUP':'分类'}</button>`}
function setPhraseGroup(value){group=value;const filters=document.querySelector('.repo-filters');if(filters)filters.innerHTML=phraseGroupChipsHtml();refreshPhraseGridWithoutJump()}
function phraseRow(p){
  const id=get(p,'Id','id'),content=get(p,'Content','content')||get(p,'Title','title')||'',note=get(p,'Description','description')||get(p,'Title','title')||t('copyPhrase');
  const groupName=groupNameFor(get(p,'GroupId','groupId'));
  return `<div class="repo-card" draggable="true" data-id="${id}" ondragstart="dragPhrase(event,'${id}')" ondragend="endPhraseDrag()" onclick="send({type:'copyPhrase',id:'${id}'})"><div class="repo-card-head"><span>${esc(groupName)}</span><button class="repo-card-delete" title="Delete" onclick="deletePhraseNow(event,'${id}')">×</button></div><div class="repo-card-body"><div class="repo-card-title">${esc(note)}</div><div class="repo-card-copy">${esc(content)}</div></div></div>`;
}
function groupNameFor(id){const groupItem=state.groups.find(item=>String(get(item,'Id','id'))===String(id));return get(groupItem||{},'Name','name')||t('customPhrase')}
function selectedPhraseGroupId(){return group==='all'?(get(state.groups[0]||{},'Id','id')||''):group}
function focusPhraseForm(){document.getElementById('phraseTitle')?.focus()}
function addPhrase(){const title=document.getElementById('phraseTitle')?.value||'',content=document.getElementById('phraseContent')?.value||'';if(!content.trim()){toast(t('phraseContent'));return}const groupId=selectedPhraseGroupId();if(!groupId){toast(t('emptyPhrase'));return}send({type:'addPhrase',groupId,title,content})}
function deletePhraseNow(event,id){event.stopPropagation();state.phrases=state.phrases.filter(item=>String(get(item,'Id','id'))!==String(id));document.querySelector(`.repo-card[data-id="${id}"]`)?.remove();send({type:'deletePhrase',id});toast(t('done'))}
function renderPhraseGroupEditor(){const host=document.getElementById('groupEditorHost');if(host)host.innerHTML=phraseGroupEditorHtml()}
function addPhraseGroupNow(){phraseGroupEditor={mode:'add',name:''};renderPhraseGroupEditor();setTimeout(()=>document.getElementById('groupNameInput')?.focus(),0)}
function renamePhraseGroupNow(id){const item=state.groups.find(group=>String(get(group,'Id','id'))===String(id));phraseGroupEditor={mode:'rename',id,name:get(item||{},'Name','name')||''};renderPhraseGroupEditor();setTimeout(()=>document.getElementById('groupNameInput')?.focus(),0)}
function submitPhraseGroupEditor(){const name=document.getElementById('groupNameInput')?.value.trim()||'';if(!phraseGroupEditor||!name){toast(lang()==='en'?'Enter group name':'请输入分类名称');return}if(phraseGroupEditor.mode==='add')send({type:'addPhraseGroup',name});else send({type:'renamePhraseGroup',id:phraseGroupEditor.id,name});phraseGroupEditor=null;renderPhraseGroupEditor()}
function dragPhrase(event,id){draggedPhraseId=id;event.dataTransfer.effectAllowed='move';event.dataTransfer.setData('text/plain',id);event.currentTarget.classList.add('dragging')}
function phraseDropTarget(event){return event.target.closest?.('.repo-card')}
function handlePhraseDragOver(event){event.preventDefault();document.querySelectorAll('.repo-card.drag-target').forEach(card=>card.classList.remove('drag-target'));const target=phraseDropTarget(event);if(target&&target.dataset.id!==draggedPhraseId)target.classList.add('drag-target')}
function clearPhraseDragTarget(event){if(!event.currentTarget.contains(event.relatedTarget))document.querySelectorAll('.repo-card.drag-target').forEach(card=>card.classList.remove('drag-target'))}
function endPhraseDrag(){document.querySelectorAll('.repo-card.dragging,.repo-card.drag-target').forEach(card=>card.classList.remove('dragging','drag-target'));draggedPhraseId=''}
function dropPhrase(event){
  event.preventDefault();
  document.querySelectorAll('.repo-card.drag-target').forEach(card=>card.classList.remove('drag-target'));
  const grid=document.querySelector('[data-scroll-key="phrase-list"]');
  if(!grid||!draggedPhraseId)return;
  const cards=[...grid.querySelectorAll('.repo-card')];
  const dragged=cards.find(card=>card.dataset.id===draggedPhraseId);
  const target=event.target.closest?.('.repo-card');
  if(!dragged||!target||dragged===target)return;
  const rect=target.getBoundingClientRect();
  const before=event.clientY<rect.top+rect.height/2;
  grid.insertBefore(dragged,before?target:target.nextSibling);
  const ids=[...grid.querySelectorAll('.repo-card')].map(card=>card.dataset.id);
  const movedIds=new Set(ids.map(String));
  const moved=ids.map(id=>state.phrases.find(item=>String(get(item,'Id','id'))===String(id))).filter(Boolean);
  const rest=state.phrases.filter(item=>!movedIds.has(String(get(item,'Id','id'))));
  state.phrases=[...moved,...rest];
  draggedPhraseId='';
  requestAnimationFrame(()=>dragged?.classList.add('dragging'));
  setTimeout(()=>dragged?.classList.remove('dragging'),160);
  send({type:'reorderPhrases',ids});
  toast(t('done'));
}
function translate(){
  const target=setting('defaultTargetLanguage','English');
  return `<section class="page translate-page"><div class="translate-controlbar"><div class="language-cluster"><span>${lang()==='en'?'SOURCE':'源语言'}</span><button class="lang-button" onclick="toast('${lang()==='en'?'Auto detect enabled':'已启用自动检测'}')">${lang()==='en'?'AUTO':'自动检测'} ▾</button></div><button class="auto-toggle" onclick="toast('${lang()==='en'?'Auto detection is on':'自动检测已开启'}')">${lang()==='en'?'AUTO':'自动检测'} <span class="auto-led"></span></button><div class="language-drawer" id="languageDrawer">${languageDrawerHtml()}</div><div></div><div class="translate-actions"><button class="tool-btn" title="${t('paste')}" onclick="pasteToTranslate()">${t('paste')}</button><button class="tool-btn" title="${t('copyResult')}" onclick="copyTranslation()">${t('copyResult')}</button><button id="translateAction" class="translate-primary ${isTranslating?'loading':''}" ${isTranslating?'disabled':''} onclick="doTranslate()">${translateActionText(target)}</button></div></div><div class="translation-workbench"><div class="translate-pane source"><textarea id="src" maxlength="2000" placeholder="${t('inputPlaceholder')}" oninput="updateSourceCounter()"></textarea><div class="pane-counter" id="sourceCounter">字符计数: 0/2000</div></div><div class="translate-pane result"><textarea id="result" readonly placeholder="${t('resultPlaceholder')}">${esc(translation)}</textarea><div class="result-metrics"><div class="metric">${lang()==='en'?'STATUS':'状态'}<strong id="translateStatus">${isTranslating?(lang()==='en'?'RUNNING':'翻译中'):(translation?(lang()==='en'?'DONE':'完成'):'--')}</strong></div><div class="metric">${lang()==='en'?'TARGET':'目标'}<strong id="translateTarget">${esc(languageLabel(target))}</strong></div><div class="metric">${lang()==='en'?'ENGINE':'引擎'}<strong>${esc(setting('modelName','NEURAL_V4')||'NEURAL_V4')}</strong></div></div></div></div></section>`;
}
function translateActionText(target=setting('defaultTargetLanguage','English')){return isTranslating?(lang()==='en'?'Translating...':'翻译中...'):`${t('startTranslate')} · ${languageLabel(target)}`}
function languageDrawerHtml(){const target=setting('defaultTargetLanguage','English');return `<button class="drawer-button" onclick="toggleLanguageDrawer()">${lang()==='en'?'TARGET':'目标语言'} · ${esc(languageLabel(target))} ▾</button>${languageDrawerOpen?`<div class="drawer-panel">${languageOptions.map(languageChip).join('')}</div>`:''}`}
function languageChip([value,label]){return `<button data-choice="defaultTargetLanguage" data-value="${value}" class="chip ${setting('defaultTargetLanguage','English')===value?'active':''}" onclick="setTranslateLanguage('${value}')">${label}</button>`}
function toggleLanguageDrawer(){languageDrawerOpen=!languageDrawerOpen;const drawer=document.getElementById('languageDrawer');if(drawer)drawer.innerHTML=languageDrawerHtml()}
function settings(){
  return `<section class="page settings-page"><div class="toolbar settings-toolbar"><div class="chips"><button class="chip" onclick="send({type:'openData'})">${t('dataDir')}</button><button class="chip" onclick="send({type:'openLogs'})">${t('logs')}</button></div></div><div class="settings-scroll" data-scroll-key="settings"><div class="settings">${section(t('general'),choice('language',t('language'),[['zh','中文'],['en','English']])+choice('theme',lang()==='en'?'Theme':'主题',[['dark',lang()==='en'?'Dark':'黑'],['white',lang()==='en'?'White':'白']])+toggle('startOnBoot',t('startup'))+toggle('hideOnClose',t('hideClose')))}${section(t('clipboardSection'),num('saveDays',t('saveDays'))+num('maxItems',t('maxItems'))+toggle('recordText',t('recordText'))+toggle('recordLinks',t('recordLinks'))+toggle('recordImages',t('recordImages'))+toggle('recordFiles',t('recordFiles'))+toggle('sensitiveDetection',t('sensitive')))}${section(t('cache'),num('fileMaxMb',t('fileMax'))+num('cacheMaxGb',t('cacheMax'))+toggle('quickShowClipboard',t('quickClipboard'))+toggle('quickShowPhrases',t('quickPhrases'))+hotkeyField())}${section(t('translate'),input('apiBaseUrl',t('api'))+input('apiKey',t('apiKey'),'password')+input('modelName',t('model'))+`<div class="field"><label>${t('test')}</label><button class="action" onclick="testApi()">${t('testApi')}</button></div>`)}</div></div></section>`;
}
function section(title,body){return `<div class="section"><h3>${title}</h3>${body}</div>`}
function input(key,label,type='text'){return `<div class="field"><label>${label}</label><input data-setting="${key}" type="${type}" value="${esc(setting(key,''))}" onchange="save('${key}',this.value)"></div>`}
function num(key,label){return `<div class="field"><label>${label}</label><input data-setting="${key}" type="number" min="1" value="${esc(setting(key,0))}" onchange="save('${key}',Number(this.value||0))"></div>`}
function toggle(key,label){const on=!!setting(key,false);return `<div data-setting="${key}" class="toggle ${on?'on':''}" onclick="save('${key}',!setting('${key}',false))"><span>${label}</span><span class="switch"></span></div>`}
function choice(key,label,items){return `<div class="field"><label>${label}</label><div class="chips">${items.map(([value,text])=>`<button data-choice="${key}" data-value="${value}" class="chip ${setting(key,'')===value?'active':''}" onclick="save('${key}','${value}')">${text}</button>`).join('')}</div></div>`}
function hotkeyField(){const value=setting('quickHotkey','');return `<div class="field"><label>${t('quickHotkey')}</label><div class="chips"><button class="chip active" data-setting="quickHotkey">${esc(recordingHotkey?t('recordingHotkey'):(value||t('notSet')))}</button><button class="chip" onclick="startHotkeyRecord()">${t('recordHotkey')}</button><button class="chip" onclick="clearHotkey()">${t('clearHotkey')}</button></div></div>`}
function startHotkeyRecord(){recordingHotkey=true;updateHotkeyDisplay();toast(t('recordingHotkey'))}
function clearHotkey(){recordingHotkey=false;save('quickHotkey','');toast(t('notSet'));updateHotkeyDisplay()}
function languageLabel(value){return languageOptions.find(([id])=>id===value)?.[1]||value}
function setTranslateLanguage(value){languageDrawerOpen=false;save('defaultTargetLanguage',value);const drawer=document.getElementById('languageDrawer');if(drawer)drawer.innerHTML=languageDrawerHtml();updateTranslateLanguage();toast(lang()==='en'?'Target language updated':'目标语言已切换')}
function save(key,value){
  rememberScroll();
  state.settings={...state.settings,[key]:value,[key[0].toUpperCase()+key.slice(1)]:value};
  send({type:'saveSetting',key,value});
  updateSettingControl(key,value);
}
function updateSettingControl(key,value){
  document.querySelectorAll(`[data-setting="${key}"]`).forEach(el=>{
    if(el.classList.contains('toggle')) el.classList.toggle('on',!!value);
    if(el.tagName==='INPUT') el.value=value;
    if(key==='quickHotkey') el.textContent=value||t('notSet');
  });
  document.querySelectorAll(`[data-choice="${key}"]`).forEach(el=>el.classList.toggle('active',el.dataset.value===String(value)));
  if(key==='theme'){
    const shell=document.querySelector('.shell');
    shell?.classList.toggle('theme-white',value==='white');
    shell?.classList.toggle('theme-dark',value!=='white');
  }
  if(key==='language') render();
}
function updateHotkeyDisplay(){document.querySelectorAll('[data-setting="quickHotkey"]').forEach(el=>{el.textContent=recordingHotkey?t('recordingHotkey'):(setting('quickHotkey','')||t('notSet'))})}
function updateTranslateLanguage(){document.querySelectorAll('[data-choice="defaultTargetLanguage"]').forEach(el=>el.classList.toggle('active',el.dataset.value===String(setting('defaultTargetLanguage','English'))));const action=document.getElementById('translateAction');if(action)action.textContent=translateActionText();const target=document.getElementById('translateTarget');if(target)target.textContent=languageLabel(setting('defaultTargetLanguage','English'))}
function testApi(){rememberScroll();const config=readVisibleApiConfig();Object.entries(config).forEach(([key,value])=>{state.settings={...state.settings,[key]:value,[key[0].toUpperCase()+key.slice(1)]:value}});toast(t('testingApi'));send({type:'testApi',...config})}
function readVisibleApiConfig(){const read=key=>document.querySelector(`[data-setting="${key}"]`)?.value||setting(key,'');return {apiBaseUrl:read('apiBaseUrl'),apiKey:read('apiKey'),modelName:read('modelName')}}
function rememberScroll(){document.querySelectorAll('.list,.hide-scroll,.settings-scroll').forEach((el,index)=>{const key=el.dataset.scrollKey||`${tab}-${index}`;scrollMemory[key]=el.scrollTop})}
function restoreScroll(renderedTab=tab){requestAnimationFrame(()=>document.querySelectorAll('.list,.hide-scroll,.settings-scroll').forEach((el,index)=>{const key=el.dataset.scrollKey||`${renderedTab}-${index}`;if(scrollMemory[key]!==undefined)el.scrollTop=scrollMemory[key]}))}
function currentClipboardItems(){
  return state.clipboard.filter(item=>{
    const kind=normalizeKind(get(item,'Kind','kind'));
    const hay=`${get(item,'Title','title')||''} ${get(item,'Content','content')||''} ${get(item,'OriginalPath','originalPath')||''}`.toLowerCase();
    return (filter==='all'||kind===filter)&&(!query||hay.includes(query.toLowerCase()));
  });
}
function refreshClipboardListWithoutJump(){
  const list=document.querySelector('[data-scroll-key="clipboard-list"]');
  if(!list) return;
  const top=list.scrollTop;
  list.innerHTML=clipboardListHtml(currentClipboardItems());
  list.scrollTop=top;
  updateHeroSide();
}
function refreshPhraseGridWithoutJump(){
  const grid=document.querySelector('[data-scroll-key="phrase-list"]');
  if(!grid) return;
  const top=grid.scrollTop;
  const selected=String(group);
  const needle=phraseQuery.trim().toLowerCase();
  const phrases=state.phrases.filter(p=>{
    const inGroup=selected==='all'||String(get(p,'GroupId','groupId'))===selected;
    const text=`${get(p,'Title','title')||''} ${get(p,'Content','content')||''} ${get(p,'Description','description')||''}`.toLowerCase();
    return inGroup&&(!needle||text.includes(needle));
  });
  grid.innerHTML=phrases.length?phrases.map(phraseRow).join(''):`<div class="empty">${t('emptyPhrase')}</div>`;
  grid.scrollTop=top;
}
function refreshPhraseFilters(){const filters=document.querySelector('.repo-filters');if(filters)filters.innerHTML=phraseGroupChipsHtml()}
function patchClipboard(id, updater){
  state.clipboard=state.clipboard.map(item=>String(get(item,'Id','id'))===String(id)?updater(item):item);
  refreshClipboardListWithoutJump();
}
function pinClipboardNow(id){patchClipboard(id,item=>({...item,Pinned:!get(item,'Pinned','pinned')}));send({type:'pinClipboard',id})}
function deleteClipboardNow(id){removeClipboard(id);send({type:'deleteClipboard',id})}
function removeClipboard(id){
  state.clipboard=state.clipboard.filter(item=>String(get(item,'Id','id'))!==String(id));
  refreshClipboardListWithoutJump();
}
function updateSourceCounter(){const src=document.getElementById('src');const counter=document.getElementById('sourceCounter');if(counter&&src)counter.textContent=(lang()==='en'?'Characters: ':'字符计数: ')+src.value.length+'/2000'}
function setTranslateLoading(loading){isTranslating=loading;const action=document.getElementById('translateAction');if(action){action.disabled=loading;action.classList.toggle('loading',loading);action.textContent=translateActionText()}const status=document.getElementById('translateStatus');if(status)status.textContent=loading?(lang()==='en'?'RUNNING':'翻译中'):(translation?(lang()==='en'?'DONE':'完成'):'--')}
function doTranslate(){if(isTranslating){toast(lang()==='en'?'Already translating':'正在翻译，请稍等');return}const source=document.getElementById('src').value.trim();if(!source){toast(t('fillTranslate'));return}setTranslateLoading(true);send({type:'translate',text:source,target:setting('defaultTargetLanguage','English')})}
function pasteToTranslate(){send({type:'readClipboardText'})}
function copyTranslation(){const value=document.getElementById('result')?.value||translation;if(!value){toast(t('noResult'));return}send({type:'copyText',text:value})}
function toast(text){const el=document.getElementById('toast');if(!el)return;el.textContent=text;el.classList.add('show');clearTimeout(window.__toastTimer);window.__toastTimer=setTimeout(()=>el.classList.remove('show'),1300)}
window.copyCreatorReceive=(message)=>{
  if(message.type==='state'){
    state=message.payload;
    if(message.payload.toast)toast(message.payload.toast);
    if(tab==='clipboard'){
      refreshClipboardListWithoutJump();
    }else{
      updateHeroSide();
    }
    return;
  }
  if(message.type==='settingsSaved'){return}
  if(message.type==='hotkeyStatus'){toast(message.payload.text||t('done'));return}
  if(message.type==='openTab'){rememberScroll();tab=message.payload.tab||'clipboard';render();return}
  if(message.type==='toast'){toast(message.payload.text||t('done'));return}
  if(message.type==='clipboardPinned'){return}
  if(message.type==='clipboardDeleted'){return}
  if(message.type==='phraseDeleted'){return}
  if(message.type==='phraseAdded'){
    if(message.payload.phrase){
      state.phrases=[message.payload.phrase,...state.phrases];
      if(tab==='phrases'){
        refreshPhraseFilters();
        refreshPhraseGridWithoutJump();
        const title=document.getElementById('phraseTitle');
        const content=document.getElementById('phraseContent');
        if(title)title.value='';
        if(content)content.value='';
      }
    }
    toast(message.payload.toast||'短语已添加');
    return;
  }
  if(message.type==='phraseGroupSaved'){
    if(message.payload.group){
      const saved=message.payload.group;
      const id=String(get(saved,'Id','id'));
      const index=state.groups.findIndex(item=>String(get(item,'Id','id'))===id);
      if(index>=0)state.groups[index]=saved;else state.groups=[...state.groups,saved];
      if(tab==='phrases'){
        if(index<0)group=id;
        refreshPhraseFilters();
        refreshPhraseGridWithoutJump();
      }
    }
    toast(message.payload.toast||t('done'));
    return;
  }
  if(message.type==='phrasesReordered'){return}
  if(message.type==='clipboardText'){const src=document.getElementById('src');if(src&&message.payload.text){src.value=message.payload.text;toast(t('pasted'))}else{toast(t('noText'))}return}
  if(message.type==='translation'){translation=message.payload.result||'';const result=document.getElementById('result');if(result)result.value=translation;setTranslateLoading(false);toast(t('translated'))}
};
window.addEventListener('keydown',event=>{
  if(!recordingHotkey) return;
  event.preventDefault();
  const key=event.key.length===1?event.key.toUpperCase():event.key;
  if(['Control','Shift','Alt','Meta'].includes(key)) return;
  const parts=[];
  if(event.ctrlKey) parts.push('Ctrl');
  if(event.altKey) parts.push('Alt');
  if(event.shiftKey) parts.push('Shift');
  if(event.metaKey) parts.push('Win');
  parts.push(key.replace('Arrow',''));
  recordingHotkey=false;
  save('quickHotkey',parts.join('+'));
  updateHotkeyDisplay();
});
window.addEventListener('mouseup',event=>{
  if(!recordingHotkey) return;
  const name=event.button===3?'MouseBack':event.button===4?'MouseForward':'';
  if(!name) return;
  event.preventDefault();
  recordingHotkey=false;
  save('quickHotkey',name);
  updateHotkeyDisplay();
});
function updateHeroSide(){const side=document.querySelector('.hero-side');if(side)side.innerHTML=countLabel()}
render();send({type:'ready'});
</script>
</body>
</html>
""";
}
