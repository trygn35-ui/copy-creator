use crate::{
    models::{
        AppSettings, ClipboardItem, Phrase, PhraseGroup, PhraseGroupInput, PhraseInput,
        TranslateInput, TranslateResult,
    },
    state::AppState,
};
use chrono::Local;
use sqlx::Row;
use tauri::{AppHandle, Manager, State};

type CommandResult<T> = Result<T, String>;

/// 读取全部设置；如果数据库还没有设置，返回默认值。
#[tauri::command]
pub async fn get_settings(state: State<'_, AppState>) -> CommandResult<AppSettings> {
    let rows = sqlx::query("SELECT key, value FROM settings")
        .fetch_all(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    if rows.is_empty() {
        return Ok(AppSettings::default());
    }
    let mut settings = AppSettings::default();
    for row in rows {
        let key: String = row.get("key");
        let value: String = row.get("value");
        apply_setting(&mut settings, &key, &value);
    }
    Ok(settings)
}

/// 保存设置；API Key 的真实加密存储在后续密钥模块中接入，这里只保存是否已配置等普通项。
#[tauri::command]
pub async fn save_settings(settings: AppSettings, state: State<'_, AppState>) -> CommandResult<AppSettings> {
    let pairs = settings_to_pairs(&settings);
    let mut tx = state.pool.begin().await.map_err(|error| error.to_string())?;
    for (key, value) in pairs {
        sqlx::query("INSERT INTO settings (key, value) VALUES (?, ?) ON CONFLICT(key) DO UPDATE SET value = excluded.value")
            .bind(key)
            .bind(value)
            .execute(&mut *tx)
            .await
            .map_err(|error| error.to_string())?;
    }
    tx.commit().await.map_err(|error| error.to_string())?;
    Ok(settings)
}

/// 查询剪贴板记录，支持类型筛选和关键字搜索。
#[tauri::command]
pub async fn list_clipboard_items(filter: String, keyword: String, state: State<'_, AppState>) -> CommandResult<Vec<ClipboardItem>> {
    let pattern = format!("%{}%", keyword.trim());
    let rows = if filter == "all" {
        sqlx::query(
            "SELECT * FROM clipboard_items WHERE title LIKE ? OR content LIKE ? OR original_path LIKE ? ORDER BY pinned DESC, updated_at DESC",
        )
        .bind(&pattern)
        .bind(&pattern)
        .bind(&pattern)
        .fetch_all(&state.pool)
        .await
    } else {
        sqlx::query(
            "SELECT * FROM clipboard_items WHERE kind = ? AND (title LIKE ? OR content LIKE ? OR original_path LIKE ?) ORDER BY pinned DESC, updated_at DESC",
        )
        .bind(filter)
        .bind(&pattern)
        .bind(&pattern)
        .bind(&pattern)
        .fetch_all(&state.pool)
        .await
    }
    .map_err(|error| error.to_string())?;

    Ok(rows.into_iter().map(row_to_clipboard_item).collect())
}

/// 将指定历史记录重新写入剪贴板；图片和文件恢复会在剪贴板监听模块完善后接入。
#[tauri::command]
pub async fn copy_clipboard_item(id: i64, app: AppHandle, state: State<'_, AppState>) -> CommandResult<()> {
    let row = sqlx::query("SELECT content FROM clipboard_items WHERE id = ?")
        .bind(id)
        .fetch_optional(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    let Some(row) = row else {
        return Err("记录不存在".to_string());
    };
    let content: String = row.get("content");
    app.clipboard().write_text(content).map_err(|error| error.to_string())
}

/// 切换剪贴板记录置顶状态。
#[tauri::command]
pub async fn toggle_pin_clipboard_item(id: i64, state: State<'_, AppState>) -> CommandResult<()> {
    sqlx::query("UPDATE clipboard_items SET pinned = CASE pinned WHEN 1 THEN 0 ELSE 1 END WHERE id = ?")
        .bind(id)
        .execute(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    Ok(())
}

/// 删除剪贴板记录；数据库外的缓存文件清理在缓存模块中集中处理。
#[tauri::command]
pub async fn delete_clipboard_item(id: i64, state: State<'_, AppState>) -> CommandResult<()> {
    sqlx::query("DELETE FROM clipboard_items WHERE id = ?")
        .bind(id)
        .execute(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    Ok(())
}

/// 查询快捷短语分组。
#[tauri::command]
pub async fn list_phrase_groups(state: State<'_, AppState>) -> CommandResult<Vec<PhraseGroup>> {
    let rows = sqlx::query("SELECT id, name, sort_order FROM phrase_groups ORDER BY sort_order ASC, id ASC")
        .fetch_all(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    Ok(rows
        .into_iter()
        .map(|row| PhraseGroup {
            id: row.get("id"),
            name: row.get("name"),
            sort_order: row.get("sort_order"),
        })
        .collect())
}

/// 查询快捷短语，支持标题和内容关键字搜索。
#[tauri::command]
pub async fn list_phrases(keyword: String, state: State<'_, AppState>) -> CommandResult<Vec<Phrase>> {
    let pattern = format!("%{}%", keyword.trim());
    let rows = sqlx::query("SELECT id, group_id, title, content, updated_at FROM phrases WHERE title LIKE ? OR content LIKE ? ORDER BY updated_at DESC, id DESC")
        .bind(&pattern)
        .bind(&pattern)
        .fetch_all(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    Ok(rows.into_iter().map(row_to_phrase).collect())
}

/// 新增或更新快捷短语。
#[tauri::command]
pub async fn save_phrase(input: PhraseInput, state: State<'_, AppState>) -> CommandResult<Phrase> {
    let now = Local::now().format("%Y-%m-%d %H:%M:%S").to_string();
    if let Some(id) = input.id.filter(|id| *id > 0) {
        sqlx::query("UPDATE phrases SET group_id = ?, title = ?, content = ?, updated_at = ? WHERE id = ?")
            .bind(input.group_id)
            .bind(&input.title)
            .bind(&input.content)
            .bind(&now)
            .bind(id)
            .execute(&state.pool)
            .await
            .map_err(|error| error.to_string())?;
        Ok(Phrase { id, group_id: input.group_id, title: input.title, content: input.content, updated_at: now })
    } else {
        let result = sqlx::query("INSERT INTO phrases (group_id, title, content, updated_at) VALUES (?, ?, ?, ?)")
            .bind(input.group_id)
            .bind(&input.title)
            .bind(&input.content)
            .bind(&now)
            .execute(&state.pool)
            .await
            .map_err(|error| error.to_string())?;
        Ok(Phrase { id: result.last_insert_rowid(), group_id: input.group_id, title: input.title, content: input.content, updated_at: now })
    }
}

/// 删除快捷短语。
#[tauri::command]
pub async fn delete_phrase(id: i64, state: State<'_, AppState>) -> CommandResult<()> {
    sqlx::query("DELETE FROM phrases WHERE id = ?")
        .bind(id)
        .execute(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    Ok(())
}

/// 新增或更新快捷短语分组。
#[tauri::command]
pub async fn save_phrase_group(input: PhraseGroupInput, state: State<'_, AppState>) -> CommandResult<PhraseGroup> {
    if let Some(id) = input.id.filter(|id| *id > 0) {
        sqlx::query("UPDATE phrase_groups SET name = ? WHERE id = ?")
            .bind(&input.name)
            .bind(id)
            .execute(&state.pool)
            .await
            .map_err(|error| error.to_string())?;
        let sort_order = sqlx::query("SELECT sort_order FROM phrase_groups WHERE id = ?")
            .bind(id)
            .fetch_one(&state.pool)
            .await
            .map_err(|error| error.to_string())?
            .get("sort_order");
        Ok(PhraseGroup { id, name: input.name, sort_order })
    } else {
        let sort_order: i64 = sqlx::query("SELECT COALESCE(MAX(sort_order), 0) + 1 AS next_order FROM phrase_groups")
            .fetch_one(&state.pool)
            .await
            .map_err(|error| error.to_string())?
            .get("next_order");
        let result = sqlx::query("INSERT INTO phrase_groups (name, sort_order) VALUES (?, ?)")
            .bind(&input.name)
            .bind(sort_order)
            .execute(&state.pool)
            .await
            .map_err(|error| error.to_string())?;
        Ok(PhraseGroup { id: result.last_insert_rowid(), name: input.name, sort_order })
    }
}

/// 删除快捷短语分组，并通过外键级联删除该分组下短语。
#[tauri::command]
pub async fn delete_phrase_group(id: i64, state: State<'_, AppState>) -> CommandResult<()> {
    sqlx::query("DELETE FROM phrase_groups WHERE id = ?")
        .bind(id)
        .execute(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    Ok(())
}

/// 将快捷短语内容复制到剪贴板，不主动模拟粘贴。
#[tauri::command]
pub async fn copy_phrase(id: i64, app: AppHandle, state: State<'_, AppState>) -> CommandResult<()> {
    let row = sqlx::query("SELECT content FROM phrases WHERE id = ?")
        .bind(id)
        .fetch_optional(&state.pool)
        .await
        .map_err(|error| error.to_string())?;
    let Some(row) = row else {
        return Err("短语不存在".to_string());
    };
    let content: String = row.get("content");
    app.clipboard().write_text(content).map_err(|error| error.to_string())
}

/// 翻译文本；首版骨架只校验配置，真实 OpenAI 兼容请求由翻译模块接入。
#[tauri::command]
pub async fn translate_text(input: TranslateInput, state: State<'_, AppState>) -> CommandResult<TranslateResult> {
    let settings = get_settings(state).await?;
    if !settings.api_key_configured {
        return Err("API_KEY_MISSING".to_string());
    }
    Ok(TranslateResult {
        text: format!("[{}] {}", input.target_language, input.text),
        engine: settings.model_name,
        elapsed_ms: 0,
    })
}

/// 执行设置页高级调试命令。
#[tauri::command]
pub async fn utility_command(name: String, app: AppHandle, state: State<'_, AppState>) -> CommandResult<String> {
    match name.as_str() {
        "open_data_dir" => {
            app.opener().open_path(state.data_dir.to_string_lossy(), None::<&str>).map_err(|error| error.to_string())?;
            Ok("数据目录已打开".to_string())
        }
        "open_log_dir" => {
            app.opener().open_path(state.log_dir.to_string_lossy(), None::<&str>).map_err(|error| error.to_string())?;
            Ok("日志目录已打开".to_string())
        }
        "db_health" => {
            sqlx::query("SELECT 1").execute(&state.pool).await.map_err(|error| error.to_string())?;
            Ok("数据库连接正常".to_string())
        }
        "cleanup_cache" => Ok("缓存清理已排入后续实现".to_string()),
        "check_updates" => Ok("当前版本为 0.1.0，自动检查将在配置 GitHub 仓库后启用".to_string()),
        "test_connection" => Ok("接口测试将在密钥模块接入后启用".to_string()),
        _ => Err("未知命令".to_string()),
    }
}

fn row_to_clipboard_item(row: sqlx::sqlite::SqliteRow) -> ClipboardItem {
    ClipboardItem {
        id: row.get("id"),
        kind: row.get("kind"),
        title: row.get("title"),
        content: row.get("content"),
        preview_path: row.get("preview_path"),
        original_path: row.get("original_path"),
        cached_path: row.get("cached_path"),
        cached: row.get::<i64, _>("cached") == 1,
        pinned: row.get::<i64, _>("pinned") == 1,
        created_at: row.get("created_at"),
        updated_at: row.get("updated_at"),
        size_bytes: row.get("size_bytes"),
    }
}

fn row_to_phrase(row: sqlx::sqlite::SqliteRow) -> Phrase {
    Phrase {
        id: row.get("id"),
        group_id: row.get("group_id"),
        title: row.get("title"),
        content: row.get("content"),
        updated_at: row.get("updated_at"),
    }
}

fn apply_setting(settings: &mut AppSettings, key: &str, value: &str) {
    match key {
        "language" => settings.language = value.to_string(),
        "theme" => settings.theme = value.to_string(),
        "density" => settings.density = value.to_string(),
        "startOnBoot" => settings.start_on_boot = value == "true",
        "hideOnClose" => settings.hide_on_close = value == "true",
        "saveDays" => settings.save_days = value.parse().unwrap_or(settings.save_days),
        "maxItems" => settings.max_items = value.parse().unwrap_or(settings.max_items),
        "recordText" => settings.record_text = value == "true",
        "recordLinks" => settings.record_links = value == "true",
        "recordImages" => settings.record_images = value == "true",
        "recordFiles" => settings.record_files = value == "true",
        "sensitiveDetection" => settings.sensitive_detection = value == "true",
        "fileMaxMb" => settings.file_max_mb = value.parse().unwrap_or(settings.file_max_mb),
        "cacheMaxGb" => settings.cache_max_gb = value.parse().unwrap_or(settings.cache_max_gb),
        "quickHotkey" => settings.quick_hotkey = value.to_string(),
        "quickShowClipboard" => settings.quick_show_clipboard = value == "true",
        "quickShowPhrases" => settings.quick_show_phrases = value == "true",
        "apiBaseUrl" => settings.api_base_url = value.to_string(),
        "apiKeyConfigured" => settings.api_key_configured = value == "true",
        "modelName" => settings.model_name = value.to_string(),
        "defaultTargetLanguage" => settings.default_target_language = value.to_string(),
        _ => {}
    }
}

fn settings_to_pairs(settings: &AppSettings) -> Vec<(&'static str, String)> {
    vec![
        ("language", settings.language.clone()),
        ("theme", settings.theme.clone()),
        ("density", settings.density.clone()),
        ("startOnBoot", settings.start_on_boot.to_string()),
        ("hideOnClose", settings.hide_on_close.to_string()),
        ("saveDays", settings.save_days.to_string()),
        ("maxItems", settings.max_items.to_string()),
        ("recordText", settings.record_text.to_string()),
        ("recordLinks", settings.record_links.to_string()),
        ("recordImages", settings.record_images.to_string()),
        ("recordFiles", settings.record_files.to_string()),
        ("sensitiveDetection", settings.sensitive_detection.to_string()),
        ("imageCachePolicy", settings.image_cache_policy.clone()),
        ("fileMaxMb", settings.file_max_mb.to_string()),
        ("cacheMaxGb", settings.cache_max_gb.to_string()),
        ("cacheCleanup", settings.cache_cleanup.clone()),
        ("quickHotkey", settings.quick_hotkey.clone()),
        ("quickShowClipboard", settings.quick_show_clipboard.to_string()),
        ("quickShowPhrases", settings.quick_show_phrases.to_string()),
        ("apiBaseUrl", settings.api_base_url.clone()),
        ("apiKeyConfigured", settings.api_key_configured.to_string()),
        ("modelName", settings.model_name.clone()),
        ("defaultTargetLanguage", settings.default_target_language.clone()),
    ]
}
