# Project context

Before making changes to BSMapper, read [docs/overview.md](docs/overview.md) for the application's purpose, user flow, architecture, data model, and current boundaries.

## Project guidelines

- Editor interaction controls such as beat subdivision buttons should be placed in the 3D environment rather than screen-space DebugUI.
- Match new UI to the existing application theme (`bs_mapper_theme.tres`) and neighboring controls. Reuse editor button styles, panel colors, rounded corners, spacing, and typography instead of introducing different styling. Explicitly apply the shared theme to standalone viewport UI when needed.
- Prefer recognizable icons from the existing FontAwesome integration and reuse `ui/icon_button.tscn` for icon buttons. Keep text labels where they help make actions clear, and maintain readable text and comfortable pointer targets in VR.
- For BSMapper's 3D song spectrogram, favor a high-resolution linear frequency layout with minimal visible pixel blockiness while preserving transient detail and beat/timeline alignment, similar to the supplied ChroMapper reference.
