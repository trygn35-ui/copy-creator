export type ClipboardKind = "text" | "link" | "image" | "file";
export type ClipboardFilter = "all" | ClipboardKind;
export type ThemeMode = "system" | "light" | "dark";
export type Density = "compact" | "normal" | "relaxed";
export type Language = "zh" | "en";
export type NavKey = "clipboard" | "phrases" | "translate" | "settings";

export interface ClipboardItem {
  id: number;
  kind: ClipboardKind;
  title: string;
  content: string;
  previewPath?: string;
  originalPath?: string;
  cachedPath?: string;
  cached: boolean;
  pinned: boolean;
  createdAt: string;
  updatedAt: string;
  sizeBytes: number;
}

export interface PhraseGroup {
  id: number;
  name: string;
  sortOrder: number;
}

export interface Phrase {
  id: number;
  groupId: number;
  title: string;
  content: string;
  updatedAt: string;
}

export interface AppSettings {
  language: Language;
  theme: ThemeMode;
  density: Density;
  startOnBoot: boolean;
  hideOnClose: boolean;
  saveDays: number;
  maxItems: number;
  recordText: boolean;
  recordLinks: boolean;
  recordImages: boolean;
  recordFiles: boolean;
  sensitiveDetection: boolean;
  imageCachePolicy: "follow_history";
  fileMaxMb: number;
  cacheMaxGb: number;
  cacheCleanup: "follow_history" | "manual";
  quickHotkey: string;
  quickShowClipboard: boolean;
  quickShowPhrases: boolean;
  apiBaseUrl: string;
  apiKeyConfigured: boolean;
  modelName: string;
  defaultTargetLanguage: string;
}

export interface TranslateInput {
  text: string;
  targetLanguage: string;
}

export interface TranslateResult {
  text: string;
  engine: string;
  elapsedMs: number;
}

export interface AppData {
  clipboardItems: ClipboardItem[];
  phraseGroups: PhraseGroup[];
  phrases: Phrase[];
  settings: AppSettings;
}
