import { invoke } from "@tauri-apps/api/core";
import { initialData } from "./mock";
import type {
  AppData,
  AppSettings,
  ClipboardFilter,
  ClipboardItem,
  Phrase,
  PhraseGroup,
  TranslateInput,
  TranslateResult,
} from "./types";

const STORE_KEY = "copy-creator.mock-data";

function isTauriRuntime() {
  return "__TAURI_INTERNALS__" in window;
}

function readStore(): AppData {
  const stored = window.localStorage.getItem(STORE_KEY);
  if (!stored) {
    window.localStorage.setItem(STORE_KEY, JSON.stringify(initialData));
    return structuredClone(initialData);
  }
  return JSON.parse(stored) as AppData;
}

function writeStore(data: AppData) {
  window.localStorage.setItem(STORE_KEY, JSON.stringify(data));
}

function nextId<T extends { id: number }>(items: T[]) {
  return items.length === 0 ? 1 : Math.max(...items.map((item) => item.id)) + 1;
}

async function copyText(text: string) {
  await navigator.clipboard.writeText(text);
}

export async function getSettings(): Promise<AppSettings> {
  if (isTauriRuntime()) {
    return invoke<AppSettings>("get_settings");
  }
  return readStore().settings;
}

export async function saveSettings(settings: AppSettings): Promise<AppSettings> {
  if (isTauriRuntime()) {
    return invoke<AppSettings>("save_settings", { settings });
  }
  const data = readStore();
  data.settings = settings;
  writeStore(data);
  return settings;
}

export async function listClipboardItems(filter: ClipboardFilter, keyword: string): Promise<ClipboardItem[]> {
  if (isTauriRuntime()) {
    return invoke<ClipboardItem[]>("list_clipboard_items", { filter, keyword });
  }
  const normalizedKeyword = keyword.trim().toLowerCase();
  return readStore()
    .clipboardItems.filter((item) => filter === "all" || item.kind === filter)
    .filter((item) => {
      if (!normalizedKeyword) return true;
      return `${item.title} ${item.content} ${item.originalPath ?? ""}`.toLowerCase().includes(normalizedKeyword);
    })
    .sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.updatedAt.localeCompare(a.updatedAt));
}

export async function copyClipboardItem(id: number): Promise<void> {
  if (isTauriRuntime()) {
    return invoke<void>("copy_clipboard_item", { id });
  }
  const item = readStore().clipboardItems.find((entry) => entry.id === id);
  if (item) {
    await copyText(item.content);
  }
}

export async function togglePinClipboardItem(id: number): Promise<void> {
  if (isTauriRuntime()) {
    return invoke<void>("toggle_pin_clipboard_item", { id });
  }
  const data = readStore();
  data.clipboardItems = data.clipboardItems.map((item) =>
    item.id === id ? { ...item, pinned: !item.pinned } : item,
  );
  writeStore(data);
}

export async function deleteClipboardItem(id: number): Promise<void> {
  if (isTauriRuntime()) {
    return invoke<void>("delete_clipboard_item", { id });
  }
  const data = readStore();
  data.clipboardItems = data.clipboardItems.filter((item) => item.id !== id);
  writeStore(data);
}

export async function listPhraseGroups(): Promise<PhraseGroup[]> {
  if (isTauriRuntime()) {
    return invoke<PhraseGroup[]>("list_phrase_groups");
  }
  return readStore().phraseGroups.sort((a, b) => a.sortOrder - b.sortOrder);
}

export async function listPhrases(keyword: string): Promise<Phrase[]> {
  if (isTauriRuntime()) {
    return invoke<Phrase[]>("list_phrases", { keyword });
  }
  const normalizedKeyword = keyword.trim().toLowerCase();
  return readStore().phrases.filter((phrase) => {
    if (!normalizedKeyword) return true;
    return `${phrase.title} ${phrase.content}`.toLowerCase().includes(normalizedKeyword);
  });
}

export async function savePhrase(input: Omit<Phrase, "id" | "updatedAt"> & { id?: number }): Promise<Phrase> {
  if (isTauriRuntime()) {
    return invoke<Phrase>("save_phrase", { input });
  }
  const data = readStore();
  const now = new Date().toLocaleString("sv-SE");
  const phrase: Phrase = {
    id: input.id ?? nextId(data.phrases),
    groupId: input.groupId,
    title: input.title,
    content: input.content,
    updatedAt: now,
  };
  data.phrases = input.id ? data.phrases.map((item) => (item.id === input.id ? phrase : item)) : [...data.phrases, phrase];
  writeStore(data);
  return phrase;
}

export async function deletePhrase(id: number): Promise<void> {
  if (isTauriRuntime()) {
    return invoke<void>("delete_phrase", { id });
  }
  const data = readStore();
  data.phrases = data.phrases.filter((phrase) => phrase.id !== id);
  writeStore(data);
}

export async function savePhraseGroup(input: { id?: number; name: string }): Promise<PhraseGroup> {
  if (isTauriRuntime()) {
    return invoke<PhraseGroup>("save_phrase_group", { input });
  }
  const data = readStore();
  const group: PhraseGroup = {
    id: input.id ?? nextId(data.phraseGroups),
    name: input.name,
    sortOrder: input.id ? data.phraseGroups.find((item) => item.id === input.id)?.sortOrder ?? 1 : data.phraseGroups.length + 1,
  };
  data.phraseGroups = input.id
    ? data.phraseGroups.map((item) => (item.id === input.id ? group : item))
    : [...data.phraseGroups, group];
  writeStore(data);
  return group;
}

export async function deletePhraseGroup(id: number): Promise<void> {
  if (isTauriRuntime()) {
    return invoke<void>("delete_phrase_group", { id });
  }
  const data = readStore();
  data.phraseGroups = data.phraseGroups.filter((group) => group.id !== id);
  data.phrases = data.phrases.filter((phrase) => phrase.groupId !== id);
  writeStore(data);
}

export async function copyPhrase(id: number): Promise<void> {
  if (isTauriRuntime()) {
    return invoke<void>("copy_phrase", { id });
  }
  const phrase = readStore().phrases.find((item) => item.id === id);
  if (phrase) {
    await copyText(phrase.content);
  }
}

export async function translateText(input: TranslateInput): Promise<TranslateResult> {
  if (isTauriRuntime()) {
    return invoke<TranslateResult>("translate_text", { input });
  }
  const settings = readStore().settings;
  if (!settings.apiKeyConfigured) {
    throw new Error("API_KEY_MISSING");
  }
  return {
    text: `[${input.targetLanguage}] ${input.text}`,
    engine: settings.modelName,
    elapsedMs: 148,
  };
}

export async function utilityCommand(name: string): Promise<string> {
  if (isTauriRuntime()) {
    return invoke<string>("utility_command", { name });
  }
  return `${name}: OK`;
}
