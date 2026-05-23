# Privacy

Copy OS is designed as a local-first desktop app.

## Local Data

Runtime data is stored locally next to the executable, usually under a `data/` directory. This may include clipboard records, phrase groups, settings, cache files, and logs.

These files are intentionally ignored by Git and should not be uploaded to GitHub.

## Clipboard Content

Clipboard content can be sensitive. Do not include real clipboard data in screenshots, bug reports, commits, release packages, or examples.

## API Keys

API keys should never be committed. Use local configuration only, and rotate keys immediately if they are accidentally published.

## Network Requests

Translation features may call a configured OpenAI-compatible API endpoint. Review the endpoint provider's privacy policy before using it with sensitive content.
