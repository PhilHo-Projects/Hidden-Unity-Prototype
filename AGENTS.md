# Hidden Unity Prototype project instructions

## Scope

This archived repository preserves the original Unity prototype for Hidden.
Keep it focused on the Unity project under `Client/`. Do not add the maintained
web application, server code, credentials, machine-specific paths, or
deployment automation here.

## Structure

- `Client/` is a Unity `6000.3.8f1` project.
- The maintained web edition lives at
  `https://github.com/PhilHo-Projects/Hidden`.
- Production is hosted at `https://hidden.philippeho.dev`.

## Change discipline

- Treat this repository as historical reference unless a task explicitly asks
  to change the Unity prototype.
- Preserve Unity `.meta` files with their matching assets.
- Do not add CI/CD or production deployment configuration.
- Never commit generated Unity directories such as `Library/`, `Temp/`,
  `Build/`, `Logs/`, or `UserSettings/`.
