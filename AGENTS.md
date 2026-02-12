# Project Agent Instructions (Unity)

## Defaults
- This is a Unity project using asmdef-based Clean-ish layering.
- Output must be **manual-apply friendly**:
  1) List changed files first
  2) Provide full file contents (paste-ready)
  3) Explain WHY + what changed
  4) Provide Unity Inspector / wiring steps (no skipped steps)
  5) Provide verification checklist

## 11 Collaboration Rules (Project)
1) Domain must not reference UnityEngine (layer separation)
2) Hide external libs behind Ports/Adapters
3) Single Composition Root (one place to wire)
4) Prefer Tick scheduler; avoid Update-sprawl
5) Enforce Release/Dispose rules (Addressables/Reactive etc)
6) Separate Render FPS and Simulation Tick (30/60)
7) Save schema version + migrations are default
8) Base stack: Addressables + UniTask + InputSystem + Cinemachine + DOTween/Feel
9) DI: prefer VContainer when adopting DI
10) Reactive: event-based (avoid heavy Rx by default)
11) UGS: thin phased adoption (Auth → CloudSave sync → RemoteConfig small → Analytics funnel)

## Safety / Scope
- Do not edit ThirdParty / Packages / ProjectSettings unless explicitly requested.
- Never “fix” Domain by adding UnityEngine references; instead introduce interfaces/ports and adapt in Presentation/Infrastructure.

## Coding
- Follow SOLID; mention relevant patterns when proposing structure.
- Prefer small diff; no big-bang refactors.
