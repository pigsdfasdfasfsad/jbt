# Development Record

This section explains how the standalone offline reconstruction is being built and how each reconstruction decision is made reproducible.

The development record intentionally focuses on source evidence, conversion, engine architecture, verification, and iteration. It is suitable as the factual backbone for a later development video because each phase has a visual summary and a list of concrete artifacts that should exist when the phase is complete.

## Phase gallery

| Phase | Purpose | Diagram |
|---|---|---|
| 01 | Evidence intake | [PNG](phases/phase-01.png) |
| 02 | Map extraction | [PNG](phases/phase-02.png) |
| 03 | Asset scaffold | [PNG](phases/phase-03.png) |
| 04 | Godot scene reconstruction | [PNG](phases/phase-04.png) |
| 05 | Gameplay systems | [PNG](phases/phase-05.png) |
| 06 | Fidelity verification | [PNG](phases/phase-06.png) |
| 07 | Windows build | [PNG](phases/phase-07.png) |
| 08 | Release documentation | [PNG](phases/phase-08.png) |

## Documentation rule

Every major implementation step should leave behind at least one durable artifact: normalized data, test output, screenshot/diagram, conversion log, measured comparison, or commit. This prevents the reconstruction from becoming a collection of undocumented manual edits.
