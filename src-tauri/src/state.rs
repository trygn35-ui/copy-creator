use sqlx::{sqlite::SqlitePoolOptions, SqlitePool};
use std::{fs, path::PathBuf};
use tauri::{AppHandle, Manager};

pub struct AppState {
    pub pool: SqlitePool,
    pub data_dir: PathBuf,
    pub cache_dir: PathBuf,
    pub log_dir: PathBuf,
}

impl AppState {
    /// 初始化程序同级数据目录、缓存目录、日志目录和 SQLite 连接池。
    ///
    /// 这里优先使用可执行文件所在目录下的 `data` 文件夹，符合绿色便携版要求。
    pub async fn new(app: AppHandle) -> Result<Self, Box<dyn std::error::Error>> {
        let exe_dir = std::env::current_exe()
            .ok()
            .and_then(|path| path.parent().map(|parent| parent.to_path_buf()))
            .unwrap_or_else(|| app.path().app_data_dir().unwrap_or_else(|_| PathBuf::from(".")));
        let data_dir = exe_dir.join("data");
        let cache_dir = data_dir.join("cache");
        let log_dir = data_dir.join("logs");
        fs::create_dir_all(cache_dir.join("images"))?;
        fs::create_dir_all(cache_dir.join("files"))?;
        fs::create_dir_all(&log_dir)?;

        let db_path = data_dir.join("copy_creator.sqlite");
        let url = format!("sqlite://{}?mode=rwc", db_path.to_string_lossy());
        let pool = SqlitePoolOptions::new().max_connections(5).connect(&url).await?;
        run_migrations(&pool).await?;
        seed_defaults(&pool).await?;

        Ok(Self {
            pool,
            data_dir,
            cache_dir,
            log_dir,
        })
    }
}

/// 执行内置 SQL 迁移，保证数据库基础表存在。
async fn run_migrations(pool: &SqlitePool) -> sqlx::Result<()> {
    let migration = include_str!("../migrations/001_initial.sql");
    for statement in migration.split(';').map(str::trim).filter(|sql| !sql.is_empty()) {
        sqlx::query(statement).execute(pool).await?;
    }
    Ok(())
}

/// 写入默认短语分组，避免首次启动时界面空白。
async fn seed_defaults(pool: &SqlitePool) -> sqlx::Result<()> {
    let count: (i64,) = sqlx::query_as("SELECT COUNT(*) FROM phrase_groups")
        .fetch_one(pool)
        .await?;
    if count.0 == 0 {
        sqlx::query("INSERT INTO phrase_groups (name, sort_order) VALUES (?, ?), (?, ?), (?, ?)")
            .bind("AI 指令")
            .bind(1)
            .bind("客服短语")
            .bind(2)
            .bind("常用链接")
            .bind(3)
            .execute(pool)
            .await?;
    }
    Ok(())
}
