use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ClipboardItem {
    pub id: i64,
    pub kind: String,
    pub title: String,
    pub content: String,
    pub preview_path: Option<String>,
    pub original_path: Option<String>,
    pub cached_path: Option<String>,
    pub cached: bool,
    pub pinned: bool,
    pub created_at: String,
    pub updated_at: String,
    pub size_bytes: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PhraseGroup {
    pub id: i64,
    pub name: String,
    pub sort_order: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Phrase {
    pub id: i64,
    pub group_id: i64,
    pub title: String,
    pub content: String,
    pub updated_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PhraseInput {
    pub id: Option<i64>,
    pub group_id: i64,
    pub title: String,
    pub content: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PhraseGroupInput {
    pub id: Option<i64>,
    pub name: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AppSettings {
    pub language: String,
    pub theme: String,
    pub density: String,
    pub start_on_boot: bool,
    pub hide_on_close: bool,
    pub save_days: i64,
    pub max_items: i64,
    pub record_text: bool,
    pub record_links: bool,
    pub record_images: bool,
    pub record_files: bool,
    pub sensitive_detection: bool,
    pub image_cache_policy: String,
    pub file_max_mb: i64,
    pub cache_max_gb: i64,
    pub cache_cleanup: String,
    pub quick_hotkey: String,
    pub quick_show_clipboard: bool,
    pub quick_show_phrases: bool,
    pub api_base_url: String,
    pub api_key_configured: bool,
    pub model_name: String,
    pub default_target_language: String,
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            language: "zh".to_string(),
            theme: "light".to_string(),
            density: "normal".to_string(),
            start_on_boot: false,
            hide_on_close: true,
            save_days: 30,
            max_items: 1000,
            record_text: true,
            record_links: true,
            record_images: true,
            record_files: true,
            sensitive_detection: false,
            image_cache_policy: "follow_history".to_string(),
            file_max_mb: 200,
            cache_max_gb: 5,
            cache_cleanup: "follow_history".to_string(),
            quick_hotkey: String::new(),
            quick_show_clipboard: true,
            quick_show_phrases: true,
            api_base_url: "https://api.deepseek.com".to_string(),
            api_key_configured: false,
            model_name: "deepseek-chat".to_string(),
            default_target_language: "English".to_string(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TranslateInput {
    pub text: String,
    pub target_language: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TranslateResult {
    pub text: String,
    pub engine: String,
    pub elapsed_ms: i64,
}
