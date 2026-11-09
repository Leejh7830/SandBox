---
name: multi-agent-governance
description: >-
  Runbook for GateHelper's 3-Tier Auto-Triage, Multi-Agent Supervisor
  Verification (Regression Guard, Zero-Resource Auditor, Chaos Tester, UX Critic),
  automatic Git savepoints before Tier 3 changes, X-Ray module health checks, and
  the mandatory inline Squad Status HUD (<agent-embed>). Activate when executing
  Tier 2/Tier 3 tasks, running an X-Ray health check, or rendering the completion
  Squad Status HUD.
---

# GateHelper Multi-Agent Governance & Squad HUD Skill

This skill provides the standard operating procedures for executing **3-Tier Auto-Triage**, **Automatic Git Savepoints**, **Specialist Subagent Audits**, and the **Inline Character Status HUD (`<agent-embed>`)**.

---

## 1. Pre-Task Gate & Savepoint Protocol (Tier 3)

1. **Ask Before Large Tasks & Enforce Conversation Topic Pinning**:
   - Never launch a **🔴 Tier 3 (Multi-Agent)** overhaul or full X-Ray scan unprompted. Confirm with the user first.
   - **1 Window = 1 Module Topic Pinning**: The user operates separate conversation windows per module (`WorkLog`, `PocketFarm`, `ServerTycoon`, `DataFlip`, `Control Tower`). If the user says *"전체 점검해라"* without naming a module, inspect **only** the primary module of the current conversation window.
   - **Ignore Aborted (Stopped) Inputs**: If a previous user turn was stopped/aborted before completion and mentioned a different module (e.g., `WorkLog` inside a `PocketFarm` conversation), treat that aborted turn as an accidental mistyped input and ignore it completely.
   - **Cross-Topic Guard**: If a new prompt asks for a module that doesn't match the current conversation window's established topic, stop and ask the user whether it was meant for another window before touching any code.
   - Ignore open IDE tabs unless the user explicitly references them in chat.
2. **Automatic Savepoint (1-Second Rollback Safety Pin)**:
   - Immediately before applying **🔴 Tier 3** multi-file code modifications, record a safe rollback state via `run_command`:
     ```powershell
     git status --porcelain
     ```
   - Create a lightweight backup or `git stash push -k -u -m "Savepoint before <task>"` / backup copy in `<appDataDir>\brain\<conversation-id>\scratch\savepoint\` so that if the user says *"아까 전으로 되돌려줘"*, you can restore 100% of modified files in 1 second.

---

## 2. Specialist Supervisor Subagent Prompts (`invoke_subagent`)

Select **only** the supervisor personas relevant to the risk profile of the current task:

* **🕵️‍♂️ 회귀 방어관 (`Impact & Regression Guard`)**:
  - Verify backward compatibility with existing `_meta/*.json` files, event handler wiring, and cross-module side effects.
* **🛡️ 제로리소스 감찰관 (`Zero-Resource & Leak Auditor`)**:
  - Verify 0 allocations (`new Font/Brush/Pen/GraphicsPath`) inside `OnPaint`/`DrawSubItem`, proper `Dispose()` of `Bitmap`/`Form`/`Timer`, `Math.Max(0, Math.Min(255, alpha))` clamping, and `0.0%` background CPU when hidden.
* **😈 블랙박스 파괴자 (`Adversarial Chaos Tester`)**:
  - Write and run a temporary C# verification harness (`csc.exe` or reflection test) simulating empty lists, rapid button clicks, locked files (`FileShare.ReadWrite`), and minimum window width (`1080px`), then delete the temporary test binary/source.
* **🧭 UX 감리관 (`Workflow & UX Critic`)**:
  - Verify 1px coordinate alignment, minimal click paths, keyboard shortcuts, and clear toast/badge feedback.
* **📐 코드 구조 점검관 (`Code Structure & Diet Architect`)**:
  - Because `read_file` reads at most **800 lines or 45 KB per call**, any `.cs` file exceeding ~800–1,000 lines slows down agent reading by 2x–4x.
  - Detect bloated `.cs` files, misplaced methods that don't belong to the file's responsibility, and unused dead methods/files.
  - Safely refactor using C# `partial class` splits (e.g., `.Render.cs`, `.Input.cs`, `.Modals.cs`) or dedicated service/helper classes, and keep `<Compile Include="..." />` in `GateHelper.csproj` 100% synchronized.

---

## 3. Mandatory Inline Squad Status HUD (`<agent-embed>`)

At the end of every task (including **🟢 Tier 1 Solo**, **🟡 Tier 2**, and **🔴 Tier 3**), you **MUST NOT** improvise custom HTML from scratch (which causes inconsistent layouts or broken CSS when external CDNs like `cdn.tailwindcss.com` are blocked by CSP).

Instead, **always** read the canonical pure-CSS template at:
- [resources/hud_template.html](./resources/hud_template.html)

Copy its exact HTML/CSS structure, modify **only** the `const HUD_DATA = { ... }` JavaScript object inside it to match the current task, save it to `<appDataDir>\brain\<conversation-id>\squad_status_hud.html` (`UserFacing: true`), and embed it inline:

```html
<agent-embed src="file:///<artifact_path>/squad_status_hud.html"></agent-embed>
```

### Required HUD Elements & Anti-Spam Rule:
0. **No Intermediate Chat Text Spam**: Never print `⚡ [80%]...` chat lines between tool calls (it breaks Antigravity's native collapsible tool grouping and floods the chat scroll). Put progress descriptions only inside `run_command`'s `toolSummary` field.
1. **Top Summary Bar (Always Visible)**: Tier badge (`🟢 Tier 1 단독`, `🟡 Tier 2 자동검증`, or `🔴 Tier 3 다중감독`), final status (`100% 완료`), MSBuild status (`0 Warnings, 0 Errors`), and a **`[▼ 작업 기록 자세히 보기]`** button.
2. **5-Character Party Row**:
   - `🧙‍♂️ 메인 코더`, `🕵️‍♂️ 회귀 방어관`, `🛡️ 제로리소스 감사`, `😈 블랙박스 파괴자`, `🧭 UX 감리관`.
   - Highlight active personas in full opacity; dim standby personas (`opacity-50`, badge: `휴식 (대기)`).
3. **Expandable Step Log & Detail Console (`[▼ 자세히 보기]` toggle)**: Clicking `[▼ 작업 기록 자세히 보기]` or a character card reveals the step-by-step execution history (`[25%] ➔ [60%] ➔ [100%]`) and detailed inspection results without cluttering the chat scroll by default.

