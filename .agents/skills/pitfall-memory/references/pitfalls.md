# GateHelper Technical Pitfalls & Solutions Catalog (오답노트)

이 문서는 GateHelper 개발 과정에서 실제로 발생했던 까다로운 버그와 근본 해결 공식을 누적 기록하는 자가 진화 오답노트입니다.

---

### 1. MaterialSkinManager 자식 컨트롤 폰트 강제 초기화 현상
- **증상**: 디자이너나 생성자에서 `txtContent.Font = new Font("맑은 고딕", 12f)`를 지정해도, 실행 시 10.5pt 기본 폰트로 작게 되돌아감.
- **원인**: `MaterialSkinManager`가 폼 초기화 및 컨트롤 추가 시점에 자식 컨트롤들의 `Font`를 자체 Roboto 폰트로 덮어씀.
- **해결 공식**: 커스텀 폰트는 반드시 `Form.Load` (또는 `OnLoad` 이후) 시점에 명시적으로 재할당하고, 줌 변경 시 캐시된 폰트 변수를 직접 바인딩할 것.

---

### 2. GDI+ `Color.FromArgb` 음수/초과 알파값에 의한 '빨간 엑스(Red X)' 크래시
- **증상**: `OnPaint`에서 `Math.Sin(_pulse)` 등으로 네온 점멸 효과를 줄 때 간헐적으로 폼 전체가 하얀 바탕의 빨간 대각선 X 표시로 뻗음.
- **원인**: `Math.Sin`이 음수 구간(`-1.0 ~ 0`)으로 갈 때 알파값이 `-4`나 `260` 등으로 계산되어 `Color.FromArgb(alpha, r, g, b)`에서 `ArgumentException`이 발생하고, `OnPaint` 미처리 예외로 WinForms가 렌더링을 포기함.
- **해결 공식**: 모든 동적 알파값 연산에는 반드시 `int a = Math.Max(0, Math.Min(255, rawAlpha));` 클램프를 강제 적용할 것.

---

### 3. ObjectListView (`OlvWorkLog`) 폰트 줌 변경 시 마우스 호버 전까지 안 바뀌는 지연 현상
- **증상**: `+` / `-` 버튼으로 폰트 크기를 키웠는데 화면이 그대로이다가, 마우스 커서를 행 위에 스쳐 지나갈 때만 한 줄씩 커짐.
- **원인**: `Invalidate()`만 호출하면 WinForms `ListView`가 기존 행 높이(`RowHeight`)와 서브아이템 폰트(`e.SubItem.Font`) 메트릭 캐시를 재계산하지 않음.
- **해결 공식**: 폰트 크기 변경 시 반드시 `OlvWorkLog.BuildList(true); OlvWorkLog.Refresh();`를 연쇄 호출하고, `DrawSubItem` 내부에서는 `e.SubItem.Font` 대신 폼의 `_regularFont` 필드를 직접 참조할 것.

---

### 4. 듀얼 모니터 팝아웃 창(`Popout`) 원본 비트맵 공유 `Dispose` 크래시
- **증상**: 캡처 이미지를 팝아웃 새 창에 띄워둔 채, 메인 창에서 다른 업무를 클릭하면 팝아웃 창이 크래시(`ArgumentException: Parameter is not valid`) 발생.
- **원인**: 메인 뷰어가 새 업무 이미지를 로드하면서 이전 `Bitmap` 인스턴스를 `Dispose()`해 버려, 같은 참조를 바라보던 팝아웃 창의 GDI+ 핸들이 파괴됨.
- **해결 공식**: 팝아웃 창에는 참조를 그대로 넘기지 말고 반드시 `new Bitmap(sourceImage)`로 **독립 복제본**을 생성해 소유권을 분리하고 팝아웃 `FormClosed` 시 자체 `Dispose()`할 것.

---

### 5. Windows GDI+ `DrawString` 유니코드 서러게이트 이모지 네모(`□`, Tofu Box) 깨짐
- **증상**: `🌾`, `🎣`, `🐄`, `🔒` 등의 컬러 이모지를 GDI+ `Graphics.DrawString("맑은 고딕", ...)`으로 그리면 빈 네모 상자(`□`)로 깨져 보임.
- **원인**: .NET Framework 4.8의 GDI+ `DrawString`은 UTF-16 서러게이트 페어(U+10000 이상) 이모지 폰트 폴백을 지원하지 못하는 경우가 많음.
- **해결 공식**: 커스텀 GDI+ `DrawString` 내부에서는 서러게이트 이모지 대신 BMP 영역 표준 기호(`★`, `♥`, `⚡`, `▶`, `↻`, `✦`, `♨`) 또는 한글 텍스트 뱃지(`[농경]`, `[수산]`, `[목축]`)를 사용할 것.

---

### 6. 필터 켜진 상태에서 '새 항목 추가' 시 화면에서 증발하는 착시
- **증상**: `[진행(ING)]` 필터나 검색어가 입력된 상태에서 `[➕ 새 업무]`를 누르면 새 업무(`OPEN` 상태)가 추가되자마자 필터에 걸러져 목록에서 안 보임.
- **해결 공식**: 신규 항목 생성 액션 시에는 반드시 활성 필터를 `"ALL"`로 자동 초기화하고 검색창을 비운 뒤 신규 항목으로 포커스를 이동시킬 것.

---

### 7. 파일 읽고 쓰기 시 원본 이미지/JSON 파일 잠금(`IOException`)
- **증상**: `Image.FromFile(path)`로 이미지를 읽으면 프로세스가 종료될 때까지 해당 파일에 OS 파일 락이 걸려 삭제나 덮어쓰기 시 에러 발생.
- **해결 공식**: 이미지 파일 로드 시 `File.ReadAllBytes(path)` + `MemoryStream` + `new Bitmap(tempBmp)` 방식으로 메모리에 즉시 복사 후 스트림을 닫아 디스크 파일 락을 0초로 유지할 것.

---

### 8. WinForms 패널 내 `Anchor=Right`와 `Dock=Right` 혼용 시 Z-Order 가림 결함
- **증상**: 작은 창(1000px)에서는 정상 노출되던 우측 상단 액션 버튼들이 창을 전체화면이나 가로로 크게 늘리면 흔적도 없이 사라짐.
- **원인**: 부모 패널 안에 `Anchor = Top | Left | Right`를 가진 가변 컨트롤과 `Dock = Right`를 가진 버튼 패널이 함께 존재할 때, 창이 가로로 넓어지면 `Anchor=Right` 컨트롤이 부모의 오른쪽 끝까지 강제 확장되면서 Z-Order 상 뒤에 있는 `Dock=Right` 버튼 패널 위를 불투명하게 완전히 덮어버림.
- **해결 공식**: 가변 텍스트 영역과 고정 버튼군이 공존하는 상단 헤더는 절대 Anchor와 Dock을 혼용하지 말고, 1행 3열의 **`TableLayoutPanel`** (고정 뱃지 / 100% 가변 텍스트 / 고정 버튼 그리드) 구조로 설계하여 창 크기와 관계없이 침범/가림 결함을 원천 차단할 것.

---

### 8. GDI+ `DrawString`의 서러게이트 이모지(`📋`, `📸`, `🔥`) 네모(`□`) 깨짐과 `TextRenderer` 해결 공식
- **증상**: 안내 문구(예: `📋 이 영역에서 [Ctrl+V]`)나 컨트롤 렌더링에 이모지를 넣었을 때 `□ 이 영역에서...` 처럼 네모 상자(Tofu Box)로 출력됨.
- **원인**: GDI+ `Graphics.DrawString`은 맑은 고딕 등 단일 폰트 지정 시 Windows 유니코드 서러게이트 이모지 자동 폰트 링크(Segoe UI Emoji 등) 폴백을 처리하지 못함. 반면 Windows GDI 기반의 `TextRenderer.DrawText`는 OS 레벨의 다중 폰트 링크 및 서러게이트 이모지 폴백을 완벽히 지원함.
- **해결 공식**: 유니코드 이모지나 특수 기호가 포함된 문자열 렌더링 시에는 `Graphics.DrawString`을 쓰지 말고 반드시 `TextRenderer.DrawText(g, text, font, rect, color, flags)`를 사용할 것.

---

### 9. WinForms 폼 최소화 시 `VisibleChanged` 미발생과 백그라운드 타이머 공회전 (CPU 낭비)
- **증상**: 폼을 작업표시줄로 최소화(`WindowState == FormWindowState.Minimized`)해도 `VisibleChanged` 이벤트가 트리거되지 않아 배경에서 타이머가 계속 돌아 유휴 CPU가 소모됨.
- **원인**: WinForms 명세상 최소화는 `Visible = false`가 아니라 창 상태(`WindowState`) 변경일 뿐이므로 `VisibleChanged`가 호출되지 않음.
- **해결 공식**: `OnResize(EventArgs e)`를 오버라이드하여 `this.WindowState == FormWindowState.Minimized` 여부를 감지하고 타이머를 정지(`timer.Stop()`)하거나 `UpdateTimerState()` 로직에 포함시킬 것.

---

### 10. 차트 렌더링 시 `minVal == maxVal` 데이터 균일 구간 나눗셈 0 (`DivideByZeroException` / `NaN`)
- **증상**: 리얼타임 차트 등에서 모든 샘플 값이 동일하거나 0일 때 `(val - minVal) / (maxVal - minVal)` 계산에서 분모가 0이 되어 `NaN`이나 무한대(`Infinity`) 좌표가 생성되고 GDI+ `DrawCurve`/`DrawLine`에서 `OverflowException` 또는 렌더링 먹통 발생.
- **해결 공식**: `float range = Math.Max(0.001f, maxVal - minVal);`와 같이 최소 보정값을 두어 0 나눗셈을 원천 방어할 것.



---

### 11. WinForms `Dock=Bottom`과 `Dock=Fill`의 Z-Order 우선순위 및 `Anchor=Right` 화면 밖 이탈 버그
- **증상**: 
  1. 바닥에 배치한 패널(`Dock = DockStyle.Bottom`)이 상단 메인 영역(`Dock = DockStyle.Fill`) 뒤로 숨겨져 화면에 보이지 않음.
  2. 패널 안의 버튼에 `Anchor = AnchorStyles.Right`를 주었더니, 폼이 커질 때 버튼 좌표(X)가 화면 오른쪽 바깥(예: X=1039px)으로 영원히 날아가버림.
- **원인**:
  1. WinForms의 도킹 엔진은 Z-Order 상 뒤쪽(`Controls.Add`가 먼저 호출되거나 `SendToBack()`이 적용된 컨트롤)을 먼저 화면 외곽(Top, Bottom, Left, Right)에 도킹하고, 나중에 남은 내부 영역을 `Dock = Fill`로 채움. 반대로 `BringToFront()`를 호출하면 도킹 엔진에서 우선순위가 밀려 `Fill` 컨트롤 아래로 깔림.
  2. 컨트롤이 생성될 때 부모 패널의 초기 너비(예: 200px)가 실제 렌더링 너비(예: 884px)보다 작은 상태에서 `Anchor = Right`가 설정되면, 폼 리사이즈 시 초기 오프셋 차이(`Right Margin`)를 유지하려다 X 좌표가 비정상적으로 부모 경계 밖으로 밀려남.
- **해결 공식**:
  1. 하단 바 패널은 반드시 `pnlFooter.SendToBack();`을 호출하여 외곽 도킹 우선권을 부여할 것.
  2. 우측 정렬 버튼은 불필요하게 `Anchor = Right`를 쓰지 말고 `btn.Dock = DockStyle.Right;`와 부모 패널의 `Padding`을 활용하거나 `FlowLayoutPanel(FlowDirection.RightToLeft)`를 사용할 것.

---

### 12. `FlowLayoutPanel` 기본 마진(Top=3px)에 의한 버튼 Y축 삐뚤어짐 및 UI 정적 눈대중 검사의 함정
- **증상**: 
  1. 다이얼로그 하단에 `[✔ 등록/저장]`과 `[취소]` 버튼을 나란히 배치했을 때, `[✔ 등록/저장]`이 `[취소]`보다 위로 3px 붕 떠서 상단 테두리가 어긋남.
  2. 텍스트가 조금 긴 버튼에서 끝 글자가 잘리거나 부모 패널 밖으로 삐져나가는 현상이 발생함.
- **원인**:
  1. `FlowLayoutPanel`은 자식 컨트롤의 `Margin.Top`을 기준으로 세로 위치를 잡음. `btnCancel`은 기본 마진(Top=3px)이 적용되고, `btnSave`는 커스텀 마진(Top=0px)이 적용되어 3px Y축 어긋남이 발생.
  2. UX 감리관이 정적 코드 리뷰(눈대중)로 "Width가 140이면 대충 맞겠지"라고 추정하여, 실제 GDI+ 렌더링 픽셀(`TextRenderer.MeasureText`)과 Windows 폰트 패딩의 누적 오차를 사전에 잡아내지 못함.
- **해결 공식**:
  1. 액션 버튼군은 `FlowLayoutPanel`의 가변 마진에 맡기지 말고, 명시적 `Panel`에서 `Location = new Point(..., btnY)`로 **절대 Y좌표(`btnY = 5`)와 동일 높이(`Height = 34`)를 칼같이 고정**할 것.
  2. 모든 UI 검증 시 단순 눈대중 확인을 금지하고, 메모리 상에서 폼을 인스턴스화하여 `(TextRenderer.MeasureText.Width + 14 > btn.ClientSize.Width)` 및 인접 버튼 Y좌표 일치 여부를 1px 단위로 기계적 실측(`UI Pixel Auditor`)할 것.

---

### 13. ListView 컬럼 추가 시 `OriginalColumnNames` 캐시 배열 미동기화로 인한 열헤더 정렬 시 대량 데이터 인덱스 밀림(Shift) 버그
- **증상**: 
  - 신규 컬럼(예: `AI 등급`)을 리스트뷰 중간(인덱스 4)에 추가한 후, 사용자가 열 헤더를 클릭하여 정렬하는 순간 `14일 RSI` 헤더 밑에 등급(`C`)이 나오고, `5일 거래량` 헤더 밑에 RSI 수치(`28.2%`)가 나오는 등 4번 이후 모든 열의 데이터가 1칸씩 통째로 밀리는 현상 발생.
- **원인**:
  - `_lvStocks.Columns.Add`와 `ListViewItem(new[] { ... })`에는 13개 컬럼을 정상 추가했으나, 정렬 시 화살표(`▲/▼`)를 붙였다 떼는 데 사용되는 원본 컬럼명 캐시 배열(`OriginalColumnNames`)에 신규 컬럼이 누락되어 12개 구버전 배열로 남아 있었음.
  - 열 헤더 클릭 시 `_lvStocks.Columns[i].Text = OriginalColumnNames[i]`가 실행되면서 4번 인덱스 컬럼명이 "AI 등급"에서 "14일 RSI"로 덮어써져 헤더 텍스트만 1칸씩 당겨진 착시와 데이터 불일치가 발생함.
- **해결 공식**:
  1. 리스트뷰에 컬럼을 추가/삭제/변경할 때는 반드시 **(1) `Columns.Add`, (2) `OriginalColumnNames` 배열, (3) `ListViewItem` 서브아이템 생성 배열, (4) `IComparer` switch-case 인덱스, (5) `AutoFitLastColumn` 마지막 인덱스**의 5대 요소를 1:1 완벽 동기화할 것.
  2. 동률 발생 시 정렬 안정성을 위해 `IComparer` 내부에 `result == 0`일 때 2차 정렬 기준(예: 종합점수 내림차순 -> 종목명 가나다순)을 기본 탑재할 것.
  3. `UpdateUi()`에서 리스트뷰 대량 아이템 추가 전 `_lvStocks.ListViewItemSorter = null;`로 정렬기 간섭을 차단하여 속도와 정렬 무결성을 보장할 것.

---

### 14. 트레이 복원 시 `ShowWindow(hWnd, SW_RESTORE)` 호출 순서 오류로 인한 최대화(`Maximized`) 풀림 버그
- **증상**:
  - 창을 최대화(`FormWindowState.Maximized`) 상태로 사용하다가 시스템 트레이로 숨긴 후, 다시 트레이 아이콘을 더블클릭해 복원했을 때 `_previousNonMinimizedState`에 `Maximized`를 저장했음에도 창이 일반 크기(`Normal`)로 작아져서 복원됨.
- **원인**:
  - Win32 API `ShowWindow(hWnd, SW_RESTORE)`(`SW_RESTORE = 9`)는 창이 최소화(`Minimized`) 상태일 때뿐만 아니라 최대화(`Maximized`) 상태일 때도 원래의 `Normal` 크기로 되돌리는 명세를 가짐.
  - 따라서 `this.WindowState = _previousNonMinimizedState;`로 `Maximized`를 먼저 적용한 뒤에 `ShowWindow(this.Handle, SW_RESTORE)`를 호출하면 방금 적용한 최대화 상태가 즉시 해제되어 `Normal`로 돌아감.
- **해결 공식**:
  - 트레이나 최소화 상태에서 창을 복원할 때는 반드시 `if (this.WindowState == FormWindowState.Minimized)` 블록 안에서 **`ShowWindow(this.Handle, SW_RESTORE)`를 먼저 호출하여 최소화를 해제한 직후**, 마지막에 `this.WindowState = _previousNonMinimizedState;`를 대입할 것.
