mod commands;
mod models;
mod state;

use commands::{
    copy_clipboard_item, copy_phrase, delete_clipboard_item, delete_phrase, delete_phrase_group,
    get_settings, list_clipboard_items, list_phrase_groups, list_phrases, save_phrase,
    save_phrase_group, save_settings, toggle_pin_clipboard_item, translate_text, utility_command,
};
use state::AppState;
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    Manager, WindowEvent,
};

/// 启动 Copy Creator 桌面应用，初始化托盘、窗口行为、插件和命令接口。
///
/// 关闭主窗口时默认隐藏到托盘，只有托盘“退出”动作会真正结束程序。
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_clipboard_manager::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_log::Builder::new().build())
        .plugin(tauri_plugin_opener::init())
        .setup(|app| {
            let state = tauri::async_runtime::block_on(AppState::new(app.handle().clone()))?;
            app.manage(state);
            create_tray(app)?;
            Ok(())
        })
        .on_window_event(|window, event| {
            if matches!(event, WindowEvent::CloseRequested { .. }) {
                if let WindowEvent::CloseRequested { api, .. } = event {
                    api.prevent_close();
                    let _ = window.hide();
                }
            }
        })
        .invoke_handler(tauri::generate_handler![
            get_settings,
            save_settings,
            list_clipboard_items,
            copy_clipboard_item,
            toggle_pin_clipboard_item,
            delete_clipboard_item,
            list_phrase_groups,
            list_phrases,
            save_phrase,
            delete_phrase,
            save_phrase_group,
            delete_phrase_group,
            copy_phrase,
            translate_text,
            utility_command
        ])
        .run(tauri::generate_context!())
        .expect("failed to run Copy Creator");
}

/// 创建系统托盘和最小菜单，只保留打开、设置、退出三个动作。
fn create_tray(app: &tauri::App) -> tauri::Result<()> {
    let open = MenuItem::with_id(app, "open", "打开", true, None::<&str>)?;
    let settings = MenuItem::with_id(app, "settings", "设置", true, None::<&str>)?;
    let quit = MenuItem::with_id(app, "quit", "退出", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&open, &settings, &quit])?;

    TrayIconBuilder::new()
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id.as_ref() {
            "open" => show_main_window(app, false),
            "settings" => show_main_window(app, true),
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                show_main_window(tray.app_handle(), false);
            }
        })
        .build(app)?;

    Ok(())
}

/// 显示主窗口；`open_settings` 为后续前端路由跳转设置页预留。
fn show_main_window(app: &tauri::AppHandle, _open_settings: bool) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        let _ = window.set_focus();
    }
}
