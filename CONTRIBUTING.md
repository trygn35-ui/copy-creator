# Contributing

Thank you for taking an interest in Copy Creator.

This project is still in an early stage. Before opening a large pull request, please create an issue or discussion first so the scope can be agreed on.

## Development Setup

```powershell
npm install
npm run build
dotnet build .\desktop\CopyCreator.WinForms.csproj -c Release
```

## Pull Request Checklist

- Keep changes focused and avoid unrelated refactors.
- Do not commit local runtime data, logs, caches, API keys, or release output.
- Update documentation when user-facing behavior changes.
- Run the relevant build or validation command before submitting.
- For privacy-related changes, check `PRIVACY.md` and `SECURITY.md`.

## Coding Notes

- The current runnable Windows app lives in `desktop/`.
- The React/Vite app in `src/` is useful for browser preview and UI iteration.
- `src-tauri/` is experimental and should not be treated as the production desktop runtime yet.

