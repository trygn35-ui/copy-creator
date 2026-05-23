import {
  Clipboard,
  Copy,
  FileText,
  Languages,
  LayoutList,
  Link2,
  Moon,
  MoreHorizontal,
  Pin,
  PinOff,
  Plus,
  Search,
  Settings,
  Sun,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  copyClipboardItem,
  copyPhrase,
  deleteClipboardItem,
  deletePhrase,
  deletePhraseGroup,
  getSettings,
  listClipboardItems,
  listPhraseGroups,
  listPhrases,
  savePhrase,
  savePhraseGroup,
  saveSettings,
  togglePinClipboardItem,
  translateText,
  utilityCommand,
} from "./api";
import { useCopy } from "./i18n";
import type {
  AppSettings,
  ClipboardFilter,
  ClipboardItem,
  NavKey,
  Phrase,
  PhraseGroup,
} from "./types";

const languages = ["Chinese", "English", "Japanese", "Korean", "French", "German", "Spanish"];
const filters: ClipboardFilter[] = ["all", "text", "image", "link", "file"];

const fallbackSettings: AppSettings = {
  language: "zh",
  theme: "light",
  density: "normal",
  startOnBoot: false,
  hideOnClose: true,
  saveDays: 30,
  maxItems: 1000,
  recordText: true,
  recordLinks: true,
  recordImages: true,
  recordFiles: true,
  sensitiveDetection: false,
  imageCachePolicy: "follow_history",
  fileMaxMb: 200,
  cacheMaxGb: 5,
  cacheCleanup: "follow_history",
  quickHotkey: "",
  quickShowClipboard: true,
  quickShowPhrases: true,
  apiBaseUrl: "https://api.deepseek.com",
  apiKeyConfigured: false,
  modelName: "deepseek-chat",
  defaultTargetLanguage: "English",
};

function bytesToLabel(size: number) {
  if (size > 1024 * 1024) return `${(size / 1024 / 1024).toFixed(1)} MB`;
  if (size > 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${size} B`;
}

function kindIcon(kind: ClipboardItem["kind"]) {
  if (kind === "link") return <Link2 size={15} />;
  if (kind === "file") return <FileText size={15} />;
  if (kind === "image") return <LayoutList size={15} />;
  return <Clipboard size={15} />;
}

export function App() {
  const [settings, setSettings] = useState<AppSettings>(fallbackSettings);
  const t = useCopy(settings.language);
  const [nav, setNav] = useState<NavKey>("clipboard");
  const [clipboardItems, setClipboardItems] = useState<ClipboardItem[]>([]);
  const [phraseGroups, setPhraseGroups] = useState<PhraseGroup[]>([]);
  const [phrases, setPhrases] = useState<Phrase[]>([]);
  const [clipboardFilter, setClipboardFilter] = useState<ClipboardFilter>("all");
  const [clipboardKeyword, setClipboardKeyword] = useState("");
  const [phraseKeyword, setPhraseKeyword] = useState("");
  const [activeGroupId, setActiveGroupId] = useState<number | "all">("all");
  const [editingPhrase, setEditingPhrase] = useState<Phrase | null>(null);
  const [editingGroup, setEditingGroup] = useState<PhraseGroup | null>(null);
  const [quickOpen, setQuickOpen] = useState(false);
  const [toast, setToast] = useState("");
  const [translateSource, setTranslateSource] = useState("");
  const [targetLanguage, setTargetLanguage] = useState("English");
  const [translateResult, setTranslateResult] = useState("");
  const [translateError, setTranslateError] = useState("");
  const [loadingTranslate, setLoadingTranslate] = useState(false);

  const themeClass = settings.theme === "system" ? "light" : settings.theme;

  useEffect(() => {
    void hydrate();
  }, []);

  useEffect(() => {
    void refreshClipboard();
  }, [clipboardFilter, clipboardKeyword]);

  useEffect(() => {
    void refreshPhrases();
  }, [phraseKeyword]);

  useEffect(() => {
    document.documentElement.dataset.theme = themeClass;
    document.documentElement.dataset.density = settings.density;
  }, [settings.density, themeClass]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setQuickOpen(false);
      if (event.ctrlKey && event.altKey && event.key.toLowerCase() === "q") setQuickOpen((open) => !open);
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  async function hydrate() {
    const nextSettings = await getSettings();
    setSettings(nextSettings);
    setTargetLanguage(nextSettings.defaultTargetLanguage);
    await Promise.all([refreshClipboard(), refreshPhrases(), refreshGroups()]);
  }

  async function refreshClipboard() {
    setClipboardItems(await listClipboardItems(clipboardFilter, clipboardKeyword));
  }

  async function refreshPhrases() {
    setPhrases(await listPhrases(phraseKeyword));
  }

  async function refreshGroups() {
    setPhraseGroups(await listPhraseGroups());
  }

  async function updateSettings(patch: Partial<AppSettings>) {
    const next = { ...settings, ...patch };
    setSettings(next);
    await saveSettings(next);
  }

  async function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(""), 1800);
  }

  const filteredPhrases = useMemo(() => {
    return phrases.filter((phrase) => activeGroupId === "all" || phrase.groupId === activeGroupId);
  }, [activeGroupId, phrases]);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-mark">CC</div>
        <button className={nav === "clipboard" ? "nav-item active" : "nav-item"} onClick={() => setNav("clipboard")} title={t.clipboard}>
          <Clipboard size={19} />
        </button>
        <button className={nav === "phrases" ? "nav-item active" : "nav-item"} onClick={() => setNav("phrases")} title={t.phrases}>
          <LayoutList size={19} />
        </button>
        <button className={nav === "translate" ? "nav-item active" : "nav-item"} onClick={() => setNav("translate")} title={t.translate}>
          <Languages size={19} />
        </button>
        <div className="sidebar-spacer" />
        <button className="nav-item" onClick={() => setQuickOpen(true)} title={t.quickPanel}>
          <MoreHorizontal size={19} />
        </button>
        <button className={nav === "settings" ? "nav-item active" : "nav-item"} onClick={() => setNav("settings")} title={t.settings}>
          <Settings size={19} />
        </button>
      </aside>

      <main className="workspace">
        <header className="topbar">
          <div>
            <h1>{t[nav]}</h1>
            <p>{t.appName} · {settings.theme === "dark" ? t.dark : t.light} · {t[settings.density]}</p>
          </div>
          <button className="icon-button" onClick={() => updateSettings({ theme: settings.theme === "dark" ? "light" : "dark" })}>
            {settings.theme === "dark" ? <Sun size={18} /> : <Moon size={18} />}
          </button>
        </header>

        {nav === "clipboard" && (
          <ClipboardView
            items={clipboardItems}
            keyword={clipboardKeyword}
            filter={clipboardFilter}
            t={t}
            onKeyword={setClipboardKeyword}
            onFilter={setClipboardFilter}
            onCopy={async (id) => {
              await copyClipboardItem(id);
              await showToast(t.copy);
            }}
            onPin={async (id) => {
              await togglePinClipboardItem(id);
              await refreshClipboard();
            }}
            onDelete={async (id) => {
              await deleteClipboardItem(id);
              await refreshClipboard();
            }}
          />
        )}

        {nav === "phrases" && (
          <PhrasesView
            groups={phraseGroups}
            phrases={filteredPhrases}
            activeGroupId={activeGroupId}
            keyword={phraseKeyword}
            t={t}
            onActiveGroup={setActiveGroupId}
            onKeyword={setPhraseKeyword}
            onEditPhrase={setEditingPhrase}
            onEditGroup={setEditingGroup}
            onCopy={async (id) => {
              await copyPhrase(id);
              await showToast(t.copy);
            }}
            onDeletePhrase={async (id) => {
              await deletePhrase(id);
              await refreshPhrases();
            }}
            onDeleteGroup={async (id) => {
              await deletePhraseGroup(id);
              await refreshGroups();
              await refreshPhrases();
              setActiveGroupId("all");
            }}
          />
        )}

        {nav === "translate" && (
          <TranslateView
            t={t}
            source={translateSource}
            targetLanguage={targetLanguage}
            result={translateResult}
            error={translateError}
            loading={loadingTranslate}
            onSource={setTranslateSource}
            onTargetLanguage={setTargetLanguage}
            onCopyResult={async () => {
              await navigator.clipboard.writeText(translateResult);
              await showToast(t.copyResult);
            }}
            onTranslate={async () => {
              setTranslateError("");
              setLoadingTranslate(true);
              try {
                const result = await translateText({ text: translateSource, targetLanguage });
                setTranslateResult(result.text);
              } catch (error) {
                setTranslateError(error instanceof Error && error.message === "API_KEY_MISSING" ? t.apiMissing : "翻译失败，请检查网络或接口配置。");
              } finally {
                setLoadingTranslate(false);
              }
            }}
          />
        )}

        {nav === "settings" && <SettingsView settings={settings} t={t} onChange={updateSettings} onCommand={utilityCommand} onToast={showToast} />}
      </main>

      {editingPhrase !== null && (
        <PhraseDialog
          phrase={editingPhrase}
          groups={phraseGroups}
          t={t}
          onClose={() => setEditingPhrase(null)}
          onSave={async (input) => {
            await savePhrase(input);
            setEditingPhrase(null);
            await refreshPhrases();
          }}
        />
      )}

      {editingGroup !== null && (
        <GroupDialog
          group={editingGroup}
          t={t}
          onClose={() => setEditingGroup(null)}
          onSave={async (input) => {
            await savePhraseGroup(input);
            setEditingGroup(null);
            await refreshGroups();
          }}
        />
      )}

      {quickOpen && (
        <QuickPanel
          t={t}
          items={clipboardItems.slice(0, 12)}
          groups={phraseGroups}
          phrases={phrases.slice(0, 12)}
          settings={settings}
          onClose={() => setQuickOpen(false)}
          onCopyClipboard={async (id) => {
            await copyClipboardItem(id);
            setQuickOpen(false);
          }}
          onCopyPhrase={async (id) => {
            await copyPhrase(id);
            setQuickOpen(false);
          }}
        />
      )}

      {toast && <div className="toast">{toast}</div>}
    </div>
  );
}

function ClipboardView(props: {
  items: ClipboardItem[];
  keyword: string;
  filter: ClipboardFilter;
  t: ReturnType<typeof useCopy>;
  onKeyword(value: string): void;
  onFilter(value: ClipboardFilter): void;
  onCopy(id: number): void;
  onPin(id: number): void;
  onDelete(id: number): void;
}) {
  const { t } = props;
  return (
    <section className="panel">
      <div className="toolbar">
        <div className="searchbox">
          <Search size={17} />
          <input value={props.keyword} onChange={(event) => props.onKeyword(event.target.value)} placeholder={t.searchClipboard} />
        </div>
        <div className="segmented">
          {filters.map((filter) => (
            <button key={filter} className={props.filter === filter ? "active" : ""} onClick={() => props.onFilter(filter)}>
              {t[filter]}
            </button>
          ))}
        </div>
      </div>
      <div className="list">
        {props.items.length === 0 ? (
          <EmptyState text={props.keyword ? t.emptySearch : t.emptyClipboard} />
        ) : (
          props.items.map((item) => (
            <article className={`record record-${item.kind}`} key={item.id}>
              <div className="record-kind">{kindIcon(item.kind)} {t[item.kind]}</div>
              <div className="record-body">
                {item.kind === "image" && <div className="image-thumb" />}
                <div>
                  <strong>{item.title}</strong>
                  <p>{item.content}</p>
                  <span>{item.updatedAt} · {bytesToLabel(item.sizeBytes)} · {item.cached ? t.cached : t.pathOnly}</span>
                </div>
              </div>
              <div className="record-actions">
                <button onClick={() => props.onCopy(item.id)}><Copy size={15} /></button>
                <button onClick={() => props.onPin(item.id)}>{item.pinned ? <PinOff size={15} /> : <Pin size={15} />}</button>
                <button onClick={() => props.onDelete(item.id)}><Trash2 size={15} /></button>
              </div>
            </article>
          ))
        )}
      </div>
    </section>
  );
}

function PhrasesView(props: {
  groups: PhraseGroup[];
  phrases: Phrase[];
  activeGroupId: number | "all";
  keyword: string;
  t: ReturnType<typeof useCopy>;
  onActiveGroup(id: number | "all"): void;
  onKeyword(value: string): void;
  onEditPhrase(phrase: Phrase): void;
  onEditGroup(group: PhraseGroup): void;
  onCopy(id: number): void;
  onDeletePhrase(id: number): void;
  onDeleteGroup(id: number): void;
}) {
  const { t } = props;
  const defaultGroupId = props.groups[0]?.id ?? 1;
  return (
    <section className="panel split-panel">
      <aside className="group-rail">
        <button className={props.activeGroupId === "all" ? "active" : ""} onClick={() => props.onActiveGroup("all")}>{t.all}</button>
        {props.groups.map((group) => (
          <button key={group.id} className={props.activeGroupId === group.id ? "active" : ""} onClick={() => props.onActiveGroup(group.id)}>
            {group.name}
          </button>
        ))}
        <button className="line-button" onClick={() => props.onEditGroup({ id: 0, name: "", sortOrder: props.groups.length + 1 })}>
          <Plus size={15} /> {t.newGroup}
        </button>
      </aside>
      <div className="content-column">
        <div className="toolbar">
          <div className="searchbox">
            <Search size={17} />
            <input value={props.keyword} onChange={(event) => props.onKeyword(event.target.value)} placeholder={t.searchPhrases} />
          </div>
          <button className="primary-button" onClick={() => props.onEditPhrase({ id: 0, groupId: defaultGroupId, title: "", content: "", updatedAt: "" })}>
            <Plus size={16} /> {t.newPhrase}
          </button>
        </div>
        <div className="list">
          {props.phrases.map((phrase) => (
            <article className="phrase-row" key={phrase.id}>
              <button className="phrase-main" onClick={() => props.onCopy(phrase.id)}>
                <strong>{phrase.title}</strong>
                <span>{phrase.content}</span>
              </button>
              <button onClick={() => props.onEditPhrase(phrase)}>{t.save}</button>
              <button onClick={() => props.onDeletePhrase(phrase.id)}><Trash2 size={15} /></button>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}

function TranslateView(props: {
  t: ReturnType<typeof useCopy>;
  source: string;
  targetLanguage: string;
  result: string;
  error: string;
  loading: boolean;
  onSource(value: string): void;
  onTargetLanguage(value: string): void;
  onTranslate(): void;
  onCopyResult(): void;
}) {
  const { t } = props;
  return (
    <section className="panel translate-grid">
      <textarea value={props.source} onChange={(event) => props.onSource(event.target.value)} placeholder={t.sourceText} />
      <div className="form-row">
        <label>{t.targetLanguage}</label>
        <select value={props.targetLanguage} onChange={(event) => props.onTargetLanguage(event.target.value)}>
          {languages.map((language) => <option key={language}>{language}</option>)}
        </select>
        <button className="primary-button" onClick={props.onTranslate} disabled={!props.source.trim() || props.loading}>
          <Languages size={16} /> {props.loading ? "..." : t.startTranslate}
        </button>
      </div>
      <div className="result-box">
        <div className="result-header">
          <span>{t.result}</span>
          <button onClick={props.onCopyResult} disabled={!props.result}><Copy size={15} /> {t.copyResult}</button>
        </div>
        {props.error ? <p className="error-text">{props.error}</p> : <p>{props.result || t.apiMissing}</p>}
      </div>
    </section>
  );
}

function SettingsView(props: {
  settings: AppSettings;
  t: ReturnType<typeof useCopy>;
  onChange(patch: Partial<AppSettings>): Promise<void>;
  onCommand(name: string): Promise<string>;
  onToast(message: string): Promise<void>;
}) {
  const { settings, t } = props;
  const run = async (name: string) => props.onToast(await props.onCommand(name));
  return (
    <section className="panel settings-grid">
      <SettingsSection title={t.general}>
        <Select label={t.language} value={settings.language} onChange={(value) => props.onChange({ language: value as AppSettings["language"] })} options={[["zh", "中文"], ["en", "English"]]} />
        <Select label={t.theme} value={settings.theme} onChange={(value) => props.onChange({ theme: value as AppSettings["theme"] })} options={[["light", t.light], ["dark", t.dark], ["system", t.system]]} />
        <Select label={t.density} value={settings.density} onChange={(value) => props.onChange({ density: value as AppSettings["density"] })} options={[["compact", t.compact], ["normal", t.normal], ["relaxed", t.relaxed]]} />
        <Toggle label={t.boot} checked={settings.startOnBoot} onChange={(value) => props.onChange({ startOnBoot: value })} />
        <Toggle label={t.hideOnClose} checked={settings.hideOnClose} onChange={(value) => props.onChange({ hideOnClose: value })} />
      </SettingsSection>
      <SettingsSection title={t.clipboardSettings}>
        <NumberInput label={t.saveDays} value={settings.saveDays} onChange={(value) => props.onChange({ saveDays: value })} />
        <NumberInput label={t.maxItems} value={settings.maxItems} onChange={(value) => props.onChange({ maxItems: value })} />
        <Toggle label={t.text} checked={settings.recordText} onChange={(value) => props.onChange({ recordText: value })} />
        <Toggle label={t.link} checked={settings.recordLinks} onChange={(value) => props.onChange({ recordLinks: value })} />
        <Toggle label={t.image} checked={settings.recordImages} onChange={(value) => props.onChange({ recordImages: value })} />
        <Toggle label={t.file} checked={settings.recordFiles} onChange={(value) => props.onChange({ recordFiles: value })} />
        <Toggle label={t.sensitiveDetection} checked={settings.sensitiveDetection} onChange={(value) => props.onChange({ sensitiveDetection: value })} />
      </SettingsSection>
      <SettingsSection title={t.cache}>
        <NumberInput label={t.fileMax} value={settings.fileMaxMb} onChange={(value) => props.onChange({ fileMaxMb: value })} />
        <NumberInput label={t.cacheMax} value={settings.cacheMaxGb} onChange={(value) => props.onChange({ cacheMaxGb: value })} />
      </SettingsSection>
      <SettingsSection title={t.hotkey}>
        <TextInput label={t.quickHotkey} value={settings.quickHotkey} placeholder="未设置" onChange={(value) => props.onChange({ quickHotkey: value })} />
        <Toggle label={t.clipboard} checked={settings.quickShowClipboard} onChange={(value) => props.onChange({ quickShowClipboard: value })} />
        <Toggle label={t.phrases} checked={settings.quickShowPhrases} onChange={(value) => props.onChange({ quickShowPhrases: value })} />
      </SettingsSection>
      <SettingsSection title={t.translation}>
        <TextInput label={t.apiBaseUrl} value={settings.apiBaseUrl} onChange={(value) => props.onChange({ apiBaseUrl: value })} />
        <TextInput label={t.apiKey} value={settings.apiKeyConfigured ? "********" : ""} onChange={() => props.onChange({ apiKeyConfigured: true })} />
        <TextInput label={t.modelName} value={settings.modelName} onChange={(value) => props.onChange({ modelName: value })} />
        <button onClick={() => run("test_connection")}>{t.testConnection}</button>
      </SettingsSection>
      <SettingsSection title={t.advanced}>
        <button onClick={() => run("open_data_dir")}>{t.openDataDir}</button>
        <button onClick={() => run("open_log_dir")}>{t.openLogDir}</button>
        <button onClick={() => run("check_updates")}>{t.checkUpdates}</button>
        <button onClick={() => run("db_health")}>{t.dbHealth}</button>
        <button onClick={() => run("cleanup_cache")}>{t.cleanupCache}</button>
      </SettingsSection>
    </section>
  );
}

function PhraseDialog(props: {
  phrase: Phrase;
  groups: PhraseGroup[];
  t: ReturnType<typeof useCopy>;
  onClose(): void;
  onSave(input: { id?: number; groupId: number; title: string; content: string }): void;
}) {
  const [draft, setDraft] = useState(props.phrase);
  return (
    <div className="modal-backdrop">
      <form className="dialog" onSubmit={(event) => {
        event.preventDefault();
        props.onSave({ id: draft.id || undefined, groupId: draft.groupId, title: draft.title, content: draft.content });
      }}>
        <div className="dialog-title"><span>{props.t.newPhrase}</span><button type="button" onClick={props.onClose}><X size={17} /></button></div>
        <TextInput label={props.t.title} value={draft.title} onChange={(value) => setDraft({ ...draft, title: value })} />
        <label className="field"><span>{props.t.group}</span><select value={draft.groupId} onChange={(event) => setDraft({ ...draft, groupId: Number(event.target.value) })}>{props.groups.map((group) => <option value={group.id} key={group.id}>{group.name}</option>)}</select></label>
        <label className="field"><span>{props.t.content}</span><textarea value={draft.content} onChange={(event) => setDraft({ ...draft, content: event.target.value })} /></label>
        <button className="primary-button" type="submit">{props.t.save}</button>
      </form>
    </div>
  );
}

function GroupDialog(props: { group: PhraseGroup; t: ReturnType<typeof useCopy>; onClose(): void; onSave(input: { id?: number; name: string }): void }) {
  const [name, setName] = useState(props.group.name);
  return (
    <div className="modal-backdrop">
      <form className="dialog compact-dialog" onSubmit={(event) => {
        event.preventDefault();
        props.onSave({ id: props.group.id || undefined, name });
      }}>
        <div className="dialog-title"><span>{props.t.newGroup}</span><button type="button" onClick={props.onClose}><X size={17} /></button></div>
        <TextInput label={props.t.title} value={name} onChange={setName} />
        <button className="primary-button" type="submit">{props.t.save}</button>
      </form>
    </div>
  );
}

function QuickPanel(props: {
  t: ReturnType<typeof useCopy>;
  settings: AppSettings;
  items: ClipboardItem[];
  groups: PhraseGroup[];
  phrases: Phrase[];
  onClose(): void;
  onCopyClipboard(id: number): void;
  onCopyPhrase(id: number): void;
}) {
  const [tab, setTab] = useState<"clipboard" | "phrases">(props.settings.quickShowClipboard ? "clipboard" : "phrases");
  return (
    <div className="quick-layer" onMouseDown={props.onClose}>
      <section className="quick-panel" onMouseDown={(event) => event.stopPropagation()}>
        <aside>
          {props.settings.quickShowClipboard && <button className={tab === "clipboard" ? "active" : ""} onMouseEnter={() => setTab("clipboard")}>{props.t.clipboard}</button>}
          {props.settings.quickShowPhrases && <button className={tab === "phrases" ? "active" : ""} onMouseEnter={() => setTab("phrases")}>{props.t.phrases}</button>}
        </aside>
        <div className="quick-list">
          {tab === "clipboard" ? props.items.map((item) => (
            <button key={item.id} onClick={() => props.onCopyClipboard(item.id)}><span>{item.title}</span><small>{item.updatedAt}</small></button>
          )) : props.phrases.map((phrase) => (
            <button key={phrase.id} onClick={() => props.onCopyPhrase(phrase.id)}><span>{phrase.title}</span><small>{phrase.content}</small></button>
          ))}
        </div>
      </section>
    </div>
  );
}

function SettingsSection(props: { title: string; children: React.ReactNode }) {
  return <fieldset className="settings-section"><legend>{props.title}</legend>{props.children}</fieldset>;
}

function TextInput(props: { label: string; value: string; placeholder?: string; onChange(value: string): void }) {
  return <label className="field"><span>{props.label}</span><input value={props.value} placeholder={props.placeholder} onChange={(event) => props.onChange(event.target.value)} /></label>;
}

function NumberInput(props: { label: string; value: number; onChange(value: number): void }) {
  return <label className="field"><span>{props.label}</span><input type="number" value={props.value} onChange={(event) => props.onChange(Number(event.target.value))} /></label>;
}

function Select(props: { label: string; value: string; options: [string, string][]; onChange(value: string): void }) {
  return <label className="field"><span>{props.label}</span><select value={props.value} onChange={(event) => props.onChange(event.target.value)}>{props.options.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>;
}

function Toggle(props: { label: string; checked: boolean; onChange(value: boolean): void }) {
  return <label className="toggle-row"><span>{props.label}</span><input type="checkbox" checked={props.checked} onChange={(event) => props.onChange(event.target.checked)} /></label>;
}

function EmptyState(props: { text: string }) {
  return <div className="empty-state">{props.text}</div>;
}
