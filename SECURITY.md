# Security Policy

## Supported Versions

Copy Creator is currently pre-release software. Security fixes target the latest commit on the main branch.

## Reporting a Vulnerability

If this repository is private, report issues directly to the repository owner.

If the repository is public, please avoid posting sensitive details in a public issue. Use a private contact channel if one is listed in the repository profile, or open a minimal issue saying that a security report is available.

## Sensitive Data

Do not commit:

- API keys or tokens.
- Clipboard history.
- User data under `data/` or `release/data/`.
- Logs, caches, crash dumps, or local configuration.
- `.env` files.

## Local Runtime Notes

The desktop app stores data next to the executable by default. Treat any generated `data/` directory as private user data.

