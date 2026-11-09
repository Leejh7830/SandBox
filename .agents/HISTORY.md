# GateHelper Project - Engineering Decision & Bug-Fix History (HISTORY.md)

이 문서는 단순한 '왕조실록식' 중간 수치 나열(과도기 밸런스 수치, 임시 좌표 변경, 반복적인 테스트 통과 자랑 문구 등 현재 코드와 충돌해 혼선을 줄 수 있는 노이즈)을 전면 배제하고, **"기능적으로 어떤 문제/불합리가 있어서 왜 이렇게 구조를 변경했는가"**와 **"모듈별 최종 확립된 핵심 아키텍처"**만 정제하여 보존한 핵심 엔지니어링 이력서입니다.
*(현재의 정확한 수치/스탯/좌표는 항상 실제 C# 소스 코드와 폴더별 `AGENTS.md`를 단일 진실 공급원(Single Source of Truth)으로 삼습니다.)*

---

## 1. WorkLog Studio — 주요 기능적 문제 해결 및 구조 변경 이력

1. **구버전 데이터(`WorkLog.json`) 하위 호환성 보장 (`WorkLogRepository.cs`)**
   - **문제**: 2025년 초기 버전의 원시 JSON 배열(`[ { ... } ]`) 포맷이나 대소문자가 혼재된 상태값(`"Open"`, `"Done"`), 절대경로 이미지 참조가 신버전 스키마 로드 시 누락될 위험.
   - **해결**: 원시 배열 감지 시 자동 래핑, 상태값 대문자(`OPEN`, `DONE`, `ING..`, `FIXED`) 자동 정규화, `Path.IsPathRooted` 동시 대응, 기존 `Tags`/`Memo` 필드 무손실 보존.
2. **좌측 목록(`OlvWorkLog`) 폰트 줌 변경 시 마우스 호버 지연 해결 (`WorkLogForm.cs`)**
   - **문제**: `Invalidate()`만 호출하면 ListView 내부 행 높이(`RowHeight`)와 서브아이템 폰트 캐시가 갱신되지 않아 마우스를 올릴 때만 글자가 커지던 현상.
   - **해결**: `UpdateFontSize` 시 `OlvWorkLog.BuildList(true)` 및 `Refresh()`를 동기 호출하고, `DrawSubItem`에서 `e.SubItem.Font` 대신 폼의 `_regularFont`를 직접 참조하도록 일원화.
3. **필터 활성 상태에서 '➕ 새 업무' 생성 시 항목 증발 착시 해결**
   - **문제**: `[진행]` 필터나 검색어가 켜진 상태에서 새 업무(`OPEN`)를 추가하면 필터에 가려져 생성되지 않은 것처럼 보이던 UX 문제.
   - **해결**: 새 업무 생성 시 퀵 필터를 `"ALL"`로 자동 전환하고 검색어를 초기화하여 신규 항목으로 즉시 포커스 이동.
4. **팝아웃 뷰어(`WorkLogImagePopupForm`) 듀얼 모니터 비트맵 충돌 방지**
   - **문제**: 팝아웃 창을 띄워둔 채 메인 창에서 다른 업무를 클릭하면 기존 `Bitmap`이 `Dispose()`되어 팝아웃 창이 크래시 나던 현상.
   - **해결**: 팝아웃 창 생성 시 `new Bitmap(img)`으로 독립 복제본을 소유하게 하고 팝아웃 종료 시 자체 해제하도록 메모리 격리.
5. **우측 저장/삭제 버튼 화면 밖 잘림 해결 및 1열(`X=1`)·2열(`X=75`) 정렬 확립**
   - **문제**: 저장/삭제 버튼이 `Anchor = Top|Right`로 우측 끝에 고정되어 작은 창(1080px)에서 잘리거나 최대화 시 동선이 멀어지던 문제.
   - **해결**: 일자 컨트롤 우측 좌측 그룹으로 저장/삭제 버튼을 묶어 배치하고, 1열 뱃지(`X=1`)와 하단 탭/뷰어 보더 수직선을 1px 오차 없이 일치시킴.

---

## 2. SandBox 7: 포켓 팜 & 다이너 (PocketFarm) — 핵심 불합리 해결 이력

1. **안전 출하(`$ 안전출하`) 시 품종개량 비축 작물 강제 매각 방지**
   - **문제**: 안전 출하 클릭 시 요리 재료(2개)만 남기고 전량 판매되어, 품종개량을 위해 모아두던 작물까지 팔려 개량 진행도가 초기화되던 불합리.
   - **해결**: 미완료 품종개량 목표 수량(`RequiredStockpile`)을 보호 재고(`reservedSafeStock`)에 자동 합산하여 안전 출하 시에도 100% 보존.
2. **완료된 작업장(`Ready`)에만 일꾼 파견 및 탭별 일꾼 격리**
   - **문제**: 성장 중(`Growing`)인 밭에 일꾼이 미리 나가 대기하거나, 농장 탭에서 파견을 눌렀는데 어부/목동까지 섞여 나가고 다른 탭 화면을 가로질러 걷던 시각적 어색함.
   - **해결**: 오직 수확/채집 완료(`TaskPlotState.Ready`) 구역에만 해당 탭 전담 일꾼(농부/어부/목동)이 출동하도록 격리하고, 현재 활성 탭의 일꾼만 렌더링.
3. **완성된 축사의 사료 낭비 차단 & 오프라인 식당 자동 영업 정산**
   - **문제**: 채집 대기 중(`Ready`)인 축사가 계속 밀 사료를 소비하던 버그와, 오프라인 복귀 시 작물만 쌓이고 식당 영업 정산은 멈춰 있던 문제.
   - **해결**: 오직 생산 중(`Growing`)일 때만 사료를 소비하도록 수정하고, `RestaurantOpen` 상태면 오프라인 경과 시간 동안 식당 서빙 골드/명성도 `O(1)` 델타 정산에 반영.
4. **신규 일꾼 고용 시 기존 손수레 업그레이드 스펙 누락 해결**
   - **문제**: 손수레를 먼저 업그레이드한 뒤 새 일꾼을 고용하면 새 일꾼은 기본 적재량/이속으로 생성되던 불일치.
   - **해결**: 고용 시점의 `CartCapacityUpgradeLevel` 수치를 신규 일꾼에게 즉시 동기화.

---

## 3. SandBox 6: 데이터 플립 (Core Front) — 아키텍처 및 버그 해결 이력

1. **초기 진입 시 `NullReferenceException` 크래시 해결 (`DataFlipControl.cs`)**
   - **문제**: `InitializeComponent()` 내부 `this.Size` 설정이 `OnResize -> UpdateLayoutBounds()`를 즉시 호출하는데, `_game` 생성 코드가 그 뒤에 있어 `_game.Cols` 참조 시 크래시 발생.
   - **해결**: `_game` 생성 및 맵 초기화를 `InitializeComponent()` 이전으로 이동하고 `if (_game == null) return;` 가드 추가.
2. **시네마틱 리플레이 뷰어 미기록 및 즉시 닫힘 결함 해결**
   - **문제**: 초기/종료 2회만 스냅샷이 찍히고, 리플레이 중 아무 곳이나 클릭하면 리플레이가 곧바로 닫히며 키보드 단축키가 `_game.IsGameOver` 조건에 막혀 먹통이 되던 문제.
   - **해결**: 기지 설립·출격·교전 완료마다 턴별 스냅샷을 누적 기록하고, 7대 미디어 컨트롤러/스크러버 바 탑재 및 `OnKeyDown`에서 리플레이 입력 처리를 최우선으로 배치.
3. **발전소 한도 도달 시 이전 선택 유닛이 잘못 소환되던 버그 해결**
   - **문제**: 발전소 한도 초과 상태에서 1번 슬롯을 누를 때 이전에 선택했던 유닛 상태가 해제되지 않아 맵 클릭 시 엉뚱한 유닛이 소환되던 현상.
   - **해결**: 한도 도달 슬롯 클릭 및 탭 전환 시 `_selectedUnit = UnitType.None; _selectedBuilding = BuildingType.None;` 강제 초기화.
4. **포탑 사각지대(인접 1칸) & 원거리 유닛 Hold & Fire 메커니즘 확립**
   - **문제**: 포탑이 전방위 무결점 요격을 해 근접 돌파가 불가능했고, 원거리 유닛(콜로서스 등)이 사거리 내에 적이 있어도 굳이 앞으로 걸어가 근접전에 노출되던 문제.
   - **해결**: 포탑 상하좌우 1칸 인접 타일을 사각지대로 설정하여 근접 파고들기 전략을 열고, 사거리 2 이상 유닛은 유효 타깃 포착 시 제자리에서 정지 포격(Hold & Fire)하도록 개선.
5. **대용량 파일 역할별 `partial class` 분할 및 무언 정지 방지 워치독**
   - **구조**: `DataFlipEntities.cs`, `DataFlipModel.Cards.cs`, `DataFlipModel.Ai.cs`, `DataFlipControl.Render.cs`, `DataFlipControl.Modals.cs`, `DataFlipControl.Inspector.cs`로 책임 분리.
   - **안전망**: 턴 애니메이션이 3.5초 이상 지연되면 강제로 `EndTurnSequence()`를 호출하는 워치독 타이머와 GDI+ 알파값(0~255) 클램핑 적용.

---

## 4. SandBox 1~5 (BitFlip, SignalLink, ServerTycoon, AiTrend) — 핵심 문제 해결 이력

1. **비트 플립(BitFlip) AI 힌트 클릭 시 폼 '빨간 엑스(Red X)' 크래시 해결**
   - **문제**: `DrawQuantumLinkBeams`에서 `Math.Sin(_globalPulse * 2f)`가 음수가 될 때 알파값이 음수(`-4`)로 계산되어 `Color.FromArgb` 예외로 `OnPaint`가 무너지던 현상.
   - **해결**: 펄스식을 `0.5f + 0.5f * Sin(...)`으로 정규화하고 `Math.Max(0, Math.Min(255, alpha))` 클램프를 전역 적용.
2. **시그널 링크(SignalLink) 10×10 Expert 가로줄 허무 맵 생성 및 고속 드래그 선 끊김 해결**
   - **문제**: 기존 랜덤 스네이크 생성기가 10×10에서 74% 확률로 실패해 단순 가로줄 폴백 맵을 내보내고, 마우스를 빠르게 움직이면 선이 끊기던 현상.
   - **해결**: **Warnsdorff 휴리스틱 + D4 대칭 미로 폴백 엔진**으로 허무 맵을 0건으로 차단하고, **Bresenham 직선 보간**으로 고속 드래그 시 선 끊김을 근절.
3. **서버 타이쿤(ServerTycoon) 환생 복사 차단 & 바이트코인(BYT) 평단가/수익률 직관화**
   - **문제**: 코인 거래 시 매수 평단가와 실시간 평가 손익을 알기 어려웠고, 채굴 코인과 매수 원금이 뒤섞이던 문제.
   - **해결**: 업비트/토스 스타일 4대 지표(총 평가금액, 손익/수익률, 매수 평단가, 원금/보유량)로 개편하고 무료 채굴 시 원금 증가 없이 수량만 늘어 평단가가 낮아지는 물타기 구조 확립.
4. **AI 트렌드(AiTrend) API 할당량/네트워크 차단 방어 및 RSS 본문 누락 해결 (`AiTrendService.cs`)**
   - **문제**: 사내망 프록시나 Gemini API 호출 한도(429) 초과 시 멈추거나, 구글 뉴스 RSS가 제목만 제공해 본문 요약이 부실하던 현상.
   - **해결**: `실시간 수집 ➔ MD5 로컬 캐시 ➔ 다중 Gemini 모델 폴백 ➔ 오프라인 스타터 팩` 4단계 결함 허용 구조 및 3단 심층 브리핑(`DetailStory`), 카테고리 급전환 레이스 컨디션 방어(`_currentLoadRequestId`) 구축.

---

## 5. MainUI · 자동 로그인(GateOne OTP) · 백그라운드 모니터 이력

1. **자동 로그인(`PerformGateOneAutoLogin`) UI 응답 없음 프리징 및 타이머 포커스 강탈 해결 (`MainUI.cs`, `Util_Connect.cs`)**
   - **문제**: OTP 메일 대기(15초) 동안 메인 UI 스레드가 멈추고, 백그라운드 상태 확인 타이머가 로그인 키보드 입력(`SendKeys`) 도중 포커스를 빼앗아 입력이 씹히거나, 6단계 마지막 키보드 입력 실패 시에도 성공으로 넘어가는 논리 버그 존재.
   - **해결**: 전 구간을 `Task.Run()` 비동기화 및 `IsAuthInProgress` 인터락으로 보호하고, `AutoConnect_1 ~ 6` 전 단계가 `out string errorMessage`를 반환하도록 세분화하여 `ToastNotification` 우측 하단 페이드인 알림으로 연동 (`BtnStart1` 광클 중복 실행 차단 포함).
2. **백그라운드 모니터 분리 및 타 사이트 팝업 포커스 강탈 버그 픽스 (`Util_BackgroundMonitor.cs`)**
   - **문제**: 공지사항이나 구글 등 일반 팝업이 떴을 때 5초 주기 타이머가 브라우저 포커스를 강제로 가져가고, MGMT 설정 URL이 빈 문자열(`""`)일 때 모든 팝업을 MGMT 탭으로 오인하던 버그.
   - **해결**: 타이머 로직을 `Util_BackgroundMonitor`로 독립 분리하고, 비관리 탭은 `_ignoredHandles`에 등록해 순회에서 영구 무시하며 `!string.IsNullOrEmpty` 가드 추가.
3. **윈도우 배율 변경 시 UI 강제 원복(`🔄`) 및 MaterialSkin 타이틀 바 흰색 상자 버그 해결 (`MainUI.cs`)**
   - **문제**: 디스플레이 배율 변경 시 창/내부 컨트롤이 깨지거나, `ThemeManager` 테마 전환 시 `MaterialSkinManager`가 타이틀 바 라벨들의 `BackColor`를 흰색/회색 바탕색으로 덮어쓰던 현상.
   - **해결**: 크롬 드라이버 재연결 없이 레이아웃을 복구하는 우측 상단 `🔄` 리셋 버튼을 추가하고, `ThemeChanged` 이벤트에서 타이틀 바 라벨 배경색을 `DarkPrimaryColor`로 즉시 복원하도록 방어.
4. **사용자 옵션 분리 저장 (`_meta/user_options.json`, `Util_Option.cs`)**
   - **문제**: `Auto Screen Unlock` 등 UI 체크박스 옵션을 기존 `settings.config`에 함께 쓰면 설정 파일이 손상될 위험.
   - **해결**: `_meta/user_options.json`으로 독립 저장/복원하도록 분리.

---

## 6. 시스템 대시보드(Dashboard) · Excel Analyzer · 공통 패키지 이력

1. **대시보드 출근 시 어제/오늘 접속 기록 왜곡 해결 및 파일 동시성 방어 (`DashboardForm.cs`, `DashboardControls.cs`)**
   - **문제**: 평면 24시간 배열만 사용해 아침 출근 시 전날 오후 접속 기록이 오늘 오후처럼 보이던 왜곡 현상 및 `serverData.json` 조회 시 타 스레드 저장과의 파일 잠금 충돌 위험.
   - **해결**: 일자별 히스토리(`DailyHourlyAccessCounts`, 60일 자동 정리)를 도입해 `[최근 7일 주간] ⮀ [오늘 24시간]` 듀얼 토글 위젯으로 분리하고, `FileShare.ReadWrite` 스트림 조회 및 최소화 창 재클릭 시 `Normal` 자동 복원 적용.
2. **Excel Analyzer 체크박스 무한루프·예외 항목 가시성·창 위로 솟구침(Creeping Upwards) 해결 (`ExcelAnalyzerForm.cs`)**
   - **문제**: 고유/공통 체크박스 코드 변경 시 `_isUpdatingUI` 락으로 인해 상호 배타 해제나 우클릭 무시 색상 갱신이 씹히고, 모델리스(`Show`) 상태에서 패널 크기 변경 시 창이 화면 상단 밖으로 솟구치던 버그.
   - **해결**: `CellClick` 내 직접 덮어쓰기 및 `UpdateRowColor` 강제 갱신 함수 신설, 무시된 항목에 인디안 레드 `X` 마크 커스텀 페인팅(`CellPainting`) 적용, `ShowDialog(this)` + `CenterParent` 모달 고정으로 창 이탈 영구 해결.
3. **`.NET 9.0` 프리뷰 NuGet 어셈블리 충돌(`FUSION_E_REF_DEF_MISMATCH`) 및 대표 아이콘 찌그러짐 해결 (`GateHelper.csproj`)**
   - **문제**: 상위 프리뷰 `System.Memory` 패키지가 `.NET Framework 4.8` / `ClosedXML`과 충돌하여 엑셀 드롭 시 런타임 에러를 유발하고, 정사각형 아이콘 변환으로 로고가 세로로 찌그러지던 현상.
   - **해결**: 안정 버전 패키지로 고정 후 `AutoGenerateBindingRedirects`로 바인딩 리다이렉트를 일원화하고, 상하 투명 여백을 덧댄 원본 비율 `ico_gateone_fixed.ico` 적용.

