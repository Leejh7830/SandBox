---
name: ui-mockup-preview
description: >-
  Interactive pre-build UI mockup and requirement refinement workflow for
  GateHelper. Activate when the user requests a new screen, UI layout overhaul,
  or visual feature so they can preview and interact with a lightweight
  HTML/Tailwind prototype inline in chat (<agent-embed>) and confirm edge cases
  via ask_question before writing heavy C# WinForms code.
---

# GateHelper Pre-Build UI Mockup & Interview Skill

Use this skill whenever the user proposes a new UI layout, new game/module screen, or major visual reorganization in GateHelper.

---

## Step 1: Edge-Case Clarification (`ask_question`)

If the user's idea has open design choices or edge cases they may not have considered:
1. Do **not** guess silently and jump straight into C# coding.
2. Call `ask_question` with 1–3 multiple-choice questions, placing your best recommendation first prefixed with `(Recommended)`.

---

## Step 2: Interactive Chat Mini-Preview (`<agent-embed>`)

Before modifying WinForms `.Designer.cs` or GDI+ `OnPaint` code for a major layout change:
1. Generate a compact, interactive HTML/Tailwind mockup (`< 420px` height) in `<appDataDir>\brain\<conversation-id>\ui_preview.html` with `UserFacing: true`.
2. Style the mockup to match GateHelper's dark cyber/glassmorphism aesthetic (neon cyan `#00E5FF` / emerald `#00E676` borders, clean badge alignment) while respecting `<body class="bg-transparent ...">` for the outer frame.
3. Include clickable tabs, toggle buttons, or layout options (e.g., `[안 A: 상단 배치]` vs `[안 B: 우측 배치]`) so the user can test the feel directly inside the chat window.
4. Once the user approves the preview, translate the exact coordinates and colors 1:1 into C# WinForms / GDI+ code and verify with MSBuild (`0 Warnings, 0 Errors`).

