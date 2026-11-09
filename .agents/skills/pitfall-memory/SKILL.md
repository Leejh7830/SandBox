---
name: pitfall-memory
description: >-
  Self-evolving technical pitfall memory and recurring bug prevention system for
  GateHelper. Activate before modifying WinForms layouts, MaterialSkin forms,
  ObjectListView rendering, or GDI+ custom controls, AND activate whenever a
  tricky bug or root cause is resolved so the lesson is permanently appended to
  references/pitfalls.md.
---

# GateHelper Self-Evolving Pitfall Memory (`pitfall-memory`)

This skill prevents the agent from ever repeating past mistakes across new conversation windows.

---

## 1. Pre-Coding Check (Read Before Modifying UI / GDI+ Code)
Before modifying `MaterialForm`, `ObjectListView`, `SplitContainer`, or custom `OnPaint` controls, review the known pitfalls catalog:
- 👉 [references/pitfalls.md](./references/pitfalls.md)

---

## 2. Self-Evolution Rule (Append After Solving New Bugs)
Whenever you diagnose and fix a non-trivial bug, unexpected WinForms behavior, or subtle regression:
1. Open [references/pitfalls.md](./references/pitfalls.md).
2. Append a concise entry under a new numbered heading with:
   - **증상 (Symptom)**: What went wrong visibly or at runtime.
   - **원인 (Root Cause)**: Why WinForms / .NET Framework 4.8 / GDI+ behaved that way.
   - **해결 공식 (Permanent Fix)**: The exact code pattern to use every time.

