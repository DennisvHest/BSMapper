# Project context

Before making changes to BSMapper, read [docs/overview.md](docs/overview.md) for the application's purpose, user flow, architecture, data model, and current boundaries.

## Project guidelines

- Editor interaction controls such as beat subdivision buttons should be placed in the 3D environment rather than screen-space DebugUI.
- For BSMapper's 3D song spectrogram, favor a high-resolution linear frequency layout with minimal visible pixel blockiness while preserving transient detail and beat/timeline alignment, similar to the supplied ChroMapper reference.
