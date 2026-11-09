# GateHelper Project - Agent Control Tower & Global Rules

이 문서는 Antigravity Agent가 모든 대화 세션에서 프로젝트의 핵심 규칙과 맥락을 100% 손실 없이(24KB 한도 내 초경량 유지) 파악하도록 설계된 최상위 규칙 문서입니다.
- **폴더별 세부 아키텍처 규칙**: 해당 폴더 작업 시 자동으로 추가 로드됩니다:
  - 📋 [WorkLog/AGENTS.md](file:///c:/Users/LeeJH/source/repos/Leejh7830/GateHelper/WorkLog/AGENTS.md) — WorkLog Studio UI 좌표, 데이터 호환성 및 기능 수칙
  - 🎮 [SandBox/AGENTS.md](file:///c:/Users/LeeJH/source/repos/Leejh7830/GateHelper/SandBox/AGENTS.md) — SandBox 7대 게임/시뮬레이션 GDI+ 렌더링 및 밸런스 수칙
- **과거 상세 개발 일지 아카이브**: [HISTORY.md](file:///c:/Users/LeeJH/source/repos/Leejh7830/GateHelper/.agents/HISTORY.md)에 안전하게 보존되어 있습니다.

---

## ⚡ 1. 핵심 개발 및 시스템 빌드 가이드

- **프로젝트 성격**: SandBox C# WinForms 기반 독립 보드게임/시뮬레이션 및 주식 대응 아이디어 플랫폼 (.NET Framework 4.8)
- **공식 빌드 명령어 (다중 대화창 동시 빌드 충돌 방지 인터락 포함)**: (반드시 VS2022 Community MSBuild 사용, .NET Framework x86 타깃 호환성 유지)
  ```powershell
  while (Get-Process -Name "MSBuild","csc","__temp_capture" -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 2 }
  Stop-Process -Name SandBox -Force -ErrorAction SilentlyContinue
  & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" SandBox.csproj /p:Configuration=Release /t:Rebuild
  ```
- **다중 대화창 동시 작업 수칙**: 다른 대화창에서 이미 빌드/캡처 중이면 인터락(`PreToolUse` 훅 및 `while` 대기열)이 자동으로 끝날 때까지 대기한 후 순차 실행합니다.
- **빌드 무결성 원칙**: **0 Warnings, 0 Errors** 무결점 유지 필수.

---

## 🛡️ 2. 영구 아키텍처 규칙: 제로 리소스(Zero-Resource) 원칙

모든 개발 및 유지보수는 저사양 사무용 PC에서도 부담 없이 구동되도록 다음 성능 기준을 절대 준수합니다:

1. **💾 하드디스크 (SSD) I/O 0 Byte**: 런타임 중 임시 파일, 캐시, 지속적 로그 파일 작성 원천 금지. 모든 런타임 상태는 RAM 메모리 내에서만 처리.
2. **🧠 RAM 가비지 컬렉터(GC) 압박 0화**: `OnPaint` / `DrawSubItem` / `CellPainting` 내부에서 `new Font`, `new Brush`, `new Pen`, `new GraphicsPath` 반복 생성 금지. 반드시 **정적 캐시(Static Cache)** 또는 레이아웃 변경 시 1회성 생성으로 프레임당 GC 할당량 0 유지.
3. **⚡ CPU 스마트 유휴 절전 (Smart Idle Sleep)**: 무조건적인 60 FPS `Invalidate()` 루프 금지. 애니메이션/마우스 인터랙션 시에만 갱신하여 **유휴 CPU 0.0%**, 탭 비활성화 및 창 최소화 시(`VisibleChanged`) 타이머 즉시 중지로 **백그라운드 CPU 0.0%** 보장.
4. **⚡ 스내피(Snappy) 반응성**: 모든 타일 뒤집힘, 버튼 조작 피드백은 **70~80ms 이내의 경쾌하고 즉각적인 반응**으로 처리.

---

## 🤖 3. 자율 트리아지(Auto-Triage) 및 다중 AI 감독 아키텍처 (영구 규칙)

사용자는 작업의 내부 복잡도나 숨은 부작용을 직접 판별하지 않으며, **"겉보기엔 돌아가지만 나중에 터지는 숨은 결함(시한폭탄)"**을 가장 경계합니다. 에이전트가 **스스로 위험도를 판단하여 효율과 무결성을 통제**합니다.

### 3.1 3단계 자율 트리아지 & 사전 확인 원칙
1. **🟢 Tier 1: 단독 즉각 실행 (Solo Fast-Track)**
   - **조건**: 단순 문구/색상/좌표 미세 조정, 1개 파일 내 고립된 로직 수정, 단순 질의응답.
   - **진행**: 서브에이전트 호출 없이 메인 에이전트가 즉시 처리 + MSBuild(`0 Warnings, 0 Errors`) 확인 (자원 낭비 0%).
2. **🟡 Tier 2: 구현 + 자동 검증 스크립트 (Solo + Automated Harness)**
   - **조건**: 1~2개 파일 규모의 기능 변경이지만 UI 레이아웃 정합성이나 엣지케이스 확인이 필요한 중규모 작업.
   - **진행**: 메인 에이전트 구현 후 일회성 C# 검증 코드로 수치/예외 처리 100% 확인 후 임시 파일 즉시 삭제.
3. **🔴 Tier 3: 다중 AI 감독 & 적대적 교차 검증 (Multi-Agent Governance)**
   - **조건**: 3개 이상 파일 수정, 데이터 스키마(`.json`) 변경, `OnPaint`/타이머/파일 Lock 관련 코드 수정, 신규 모듈 신설 및 리팩토링.
   - **⚠️ 사전 확인 필수 & 대화창별 전담 주제 고정(Topic Pinning) 원칙**:
     - 시간이 소요되는 **큰 작업(🔴 Tier 3)이나 전수 건강검진 스캔은 절대 임의로 먼저 실행하지 않으며, 반드시 시작 전에 사용자에게 먼저 물어보고 진행**합니다.
     - **에디터에 열려 있는 파일(Open Documents) 무시**: 사용자는 코드를 직접 수정하지 않으므로 IDE에 열려 있는 탭 목록은 현재 관심사와 무관합니다.
     - **🎯 1대화창 = 1모듈 주제 고정 & 정지(Stop)된 오입력 무시**:
       1. 사용자는 기능별(예: `WorkLog` 창, `PocketFarm` 창, `ServerTycoon` 창, `DataFlip` 창, `에이전트 컨트롤 타워` 창)로 대화창을 분리 운영합니다.
       2. 사용자가 모듈명을 생략하고 *"전체 점검해라"*, *"개선해라"*라고 지시하면 **무조건 현재 대화창이 처음부터 다루던 전담 모듈**만 작업합니다. (예: `PocketFarm` 대화창에서 전체 점검 지시 시 절대 `WorkLog`나 타 모듈을 건드리지 않음!)
       3. 사용자가 이전 턴에 실수로 다른 모듈 질문을 치고 **정지(Stop)**를 눌렀다면, 답변이 완료되지 않은 그 중단된 질문은 **취소된 오입력**으로 간주하고 완전히 무시합니다.
       4. 만약 현재 대화창의 전담 주제와 전혀 다른 모듈에 대한 요청이 들어오면, 다른 창에 칠 내용을 잘못 입력했을 가능성이 높으므로 **절대 바로 실행하지 말고 먼저 사용자에게 *"현재 대화창은 [A 모듈] 전용 창인데, [A 모듈]을 진행할까요 아니면 방금 말씀하신 [B 모듈]을 진행할까요?"*라고 되물어 확인**합니다.

### 3.2 다중 AI 감독관 인격 풀 (필요한 인격만 핀셋 투입)
- **🕵️‍♂️ 회귀 방어관 (Impact & Regression Guard)**: 구버전 JSON 하위 호환성, 기존 이벤트 끊김, 타 모듈 연쇄 영향 추적.
- **🛡️ 제로리소스 감찰관 (Zero-Resource & Leak Auditor)**: `OnPaint` 내 객체 생성, `Dispose` 누락, 백그라운드 타이머 미정지 적발.
- **😈 블랙박스 파괴자 (Adversarial Chaos Tester)**: 빈 목록 삭제/저장 연타, 파일 잠금(Lock), 최소 창 크기(1080px) 잘림 등 가혹 조건 시뮬레이션 검증.
- **🧭 UX 감리관 (Workflow & UX Critic)**: 1px 단위 픽셀 정렬, 마우스 동선 최소화, 단축키 및 상태 피드백 점검.
- **📐 코드 구조 점검관 (Code Structure & Diet Architect)**: `.cs` 파일 비대화(**800줄/45KB 초과 시 AI 1회 리딩 한도 초과로 속도 저하 발생**), 파일 목적에 맞지 않는 메서드 혼재, 미사용 죽은 코드(Dead Code)를 적발하고 `partial class`/역할별 분리 및 `GateHelper.csproj` 정리를 수행.
  - **⚡ 무부하 자동 트리거 규칙 (수동 호출 불필요 & 평소 오버헤드 0%)**: 매 작업마다 점검 여부를 고민하지 않으며, 코드 수정 시 도구(`read_file`/`edit_file`) 상단에 자동 표시되는 파일 총 줄 수가 **1,000줄 미만이면 무조건 패스(오버헤드 0%)**합니다. 단, 수정 대상 `.cs` 파일이 **1,000줄을 돌파**했거나 🔴 Tier 3 전체 점검 시에만 자동으로 개입(또는 작업 완료 끝에 분할 정리 여부를 한 줄로 제안)합니다.

### 3.3 대화창 스크롤 밀림 방지 & 접이식(Accordion) 상황판 UI (`<agent-embed>`)
- **⚠️ 작업 중 중간 텍스트 도배 절대 금지 (스크롤 밀림 원천 차단)**:
  - 채팅창 특성상 매 스텝(`read_file`, `edit_file`, `run_command`)마다 1줄 텍스트(`⚡ [83%]...`, `⚡ [84%]...`)를 출력하면 안티그래비티의 기본 도구 묶음(접기) 기능이 깨져 대화창이 끝없이 아래로 밀립니다.
  - 따라서 **도구 호출 중간에는 일반 채팅 텍스트를 절대 출력하지 않으며**, 진행 상태는 `run_command`의 `toolSummary` 속성(예: `"[70%] MSBuild Release 컴파일 중"`)으로만 표시하여 안티그래비티가 자동으로 한 줄에 접어서(클릭 시 펼쳐보기 `>`) 보여주도록 유지합니다.
- **완료 시 '표준 단일 템플릿(`hud_template.html`)' 기반 아코디언 상황판 UI (`<agent-embed>`) 필수 사용**:
  - **⚠️ 대화창별 디자인 불일치 및 CSS 깨짐 원천 차단**: AI가 대화창마다 HTML을 임의로 새로 짜거나 차단된 외부 CDN(`cdn.tailwindcss.com`)을 쓰면 스타일이 깨져 텍스트만 나열됩니다.
  - 따라서 모든 대화창에서는 **절대 HTML 구조를 임의로 새로 작성하지 말고**, 반드시 [`.agents/skills/multi-agent-governance/resources/hud_template.html`](file:///c:/Users/LeeJH/source/repos/Leejh7830/GateHelper/.agents/skills/multi-agent-governance/resources/hud_template.html) 표준 템플릿 파일을 그대로 읽어서 내부의 **`const HUD_DATA = { ... }` 데이터 값만 교체**하여 아티팩트 폴더에 저장 후 `<agent-embed>`로 출력합니다.
  - 이를 통해 어느 대화창에서든 **100% 동일한 디자인(평소엔 1줄 요약 바 + `[▼ 자세히 보기]` 클릭 시에만 6대 캐릭터 및 진행 로그 펼침)**이 깨짐 없이 출력됩니다.
- **7대 안심 보조 기능** (전용 스킬 & 훅 연동):
  1. **🩺 X-Ray 건강검진 대시보드**: 요청 시 특정 모듈의 숨은 결함을 스캔하여 신호등(`🟢/🟡/🔴`) 위젯으로 보고.
  2. **🎙️ 빈틈 보완 객관식 역질문 (`ask_question` & `/grill-me`)**: 모호하거나 새로운 기획 시 추천안이 포함된 객관식 보기로 엣지케이스 사전 확정.
  3. **🛡️ 자동 세이브포인트 (1초 롤백 안전핀)**: Tier 3 대규모 수정 직전 Git/파일 상태를 보존하여 원클릭 100% 복구 보장 (`multi-agent-governance` 스킬 참조).
  4. **🕹️ 빌드 전 채팅창 UI 미니 프리뷰**: 화면/레이아웃 개편 시 C# 본 공사 전에 채팅창 내 인터랙티브 HTML 프리뷰 위젯으로 먼저 체험·확정 (`ui-mockup-preview` 스킬 참조).
  5. **📸 WinForms 자동 화면 캡처 & 육안 검수**: UI/렌더링 수정 후 백그라운드에서 해당 폼/컨트롤을 `.png`로 자동 캡처(`DrawToBitmap`)하여 AI가 직접 눈으로 잘림·겹침을 검수하고 스크린샷을 제시 (`visual-inspector` 스킬 참조).
  6. **🔒 실제 업무/세이브 데이터 철벽 보호 훅**: `.agents/hooks.json` 및 `data-guard.ps1`을 통해 `_meta/WorkLog.json`, `_meta/WorkLog_Images`, `_meta/sandbox/` 실제 데이터를 삭제·초기화하려는 시도를 `PreToolUse` 단계에서 기계적으로 원천 차단.
  7. **🧬 자가 진화 오답노트 (실수 재발 0%)**: WinForms/GDI+의 까다로운 함정과 해결 공식을 `pitfall-memory` 스킬(`references/pitfalls.md`)에 누적 기록하여 새 대화창에서도 동일 실수를 원천 방지.

---

## 📊 4. 주력 모듈 현황 요약 (최종 업데이트: 2026-11-08)

| 구분 | 모듈명 | 상태 | 핵심 아키텍처 요약 (세부 내역은 폴더별 `AGENTS.md` 참조) |
| :---: | :--- | :---: | :--- |
| **실무 주력** | **WorkLog Studio** | **고도화 완료** | • 마스터-디테일 스튜디오(스플리터 560px), 딥 줌 캡처 뷰어(`Ctrl+V`/드래그앤드롭/팝아웃 독립 비트맵).<br>• 좌측 목록 줌 버튼 우상단 고정, 트렌디 라운드 칩 뱃지 및 포커스 글로우 타이틀 박스.<br>• 한글 가독성 강화 본문 14pt 승격, 1열 뱃지(`X=1`)·2열 컨트롤(`X=75`) 픽셀 퍼펙트 정렬, 구버전 `WorkLog.json` 100% 하위 호환. |
| **SandBox 1** | **비트 플립 (BitFlip)** | **100% 완료** | • 3×3~6×6 맵 선택, 6대 기믹, GF(2) 가우스 소거법 AI 힌트(`H`), 70ms 스내피 플립, 유휴 CPU 0%. |
| **SandBox 2** | **시그널 링크 (SignalLink)** | **100% 완료** | • Bresenham 고속 드래그 보간, 10×10 스텔스 암기 모드, Warnsdorff + D4 대칭 미로 생성기. |
| **SandBox 3~5** | **Mystery / AiTrend / ServerTycoon** | **안정 가동** | • 방탈출 퍼즐 / AI 트렌드 뷰어 / 서버실 경영 및 업비트 스타일 바이트코인(BYT) 투자 대시보드. |
| **SandBox 6** | **데이터 플립 (Core Front)** | **초대형 완성** | • 5대 전장 테마, 6대 건물/유닛, 적응형 AI 페르소나(Rush/Siege/Tech), 시네마틱 리플레이 뷰어. |
| **SandBox 7** | **포켓 팜 & 다이너 (PocketFarm)** | **100% 완료** | • 농장·낚시·목축·식당 4대 탭, 동적 기후, 10대 업적, 일일 의뢰, VIP 평론가, 5대 분야 트리형 연구소. |
