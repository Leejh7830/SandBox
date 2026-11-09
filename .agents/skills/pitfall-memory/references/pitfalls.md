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


