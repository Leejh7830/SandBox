# SandBox Modules — Folder-Scoped Architecture & Game Design Rules

이 규칙은 `SandBox/` 디렉토리 내부(1. BitFlip, 2. SignalLink, 3. MysteryTime, 4. AiTrend, 5. ServerTycoon, 6. DataFlip/Core Front, 7. PocketFarm) 파일을 열람하거나 수정할 때 자동으로 활성화되는 전용 설계 수칙입니다.

## 1. GDI+ 렌더링 & 제로 리소스(Zero-Resource) 절대 수칙
1. **프레임당 GC 할당 0 Byte**:
   - `OnPaint` 내부에서 `new Font`, `new SolidBrush`, `new LinearGradientBrush`, `new Pen`, `new GraphicsPath`의 매 프레임 생성을 엄격히 금지합니다.
   - 모든 폰트/브러시/펜은 `static readonly` 캐시 또는 레이아웃 크기 변경 시 1회성 계산으로만 관리합니다.
2. **알파(Alpha) 클램프 & Red X 크래시 방어**:
   - 펄스/페이드 연산(`Math.Sin`, `Math.Cos`)으로 `Color.FromArgb(alpha, ...)`를 호출할 때는 반드시 `Math.Max(0, Math.Min(255, alpha))` 클램프를 적용하여 음수/오버플로 `ArgumentException`에 의한 WinForms Red X 크래시를 원천 차단합니다.
3. **유니코드 서러게이트 이모지 깨짐(Tofu Box `□`) 금지**:
   - Windows GDI+ `Graphics.DrawString`에서 폰트 폴백 실패로 네모 상자(`□`)로 깨지기 쉬운 서러게이트 페어 이모지 대신, BMP 표준 기호(`★`, `♥`, `⚡`, `▶`, `↻`, `✦`) 또는 직관적인 한글 텍스트 뱃지(`[농경]`, `[수산]`, `[목축]`, `[완성]`)를 사용합니다.
4. **스마트 유휴 절전 (`CPU 0.0%`)**:
   - 애니메이션(플립, 파티클, 일꾼 이동)이 없거나 탭이 비활성화(`VisibleChanged` / 최소화)되면 타이머를 즉시 멈추거나 렌더링을 쉬어 백그라운드 CPU 점유율 0.0%를 유지합니다.
   - 방치형/경영 시뮬레이션(PocketFarm, ServerTycoon 등)은 백그라운드/오프라인 경과 시간을 복귀 시점 1회 델타 연산(`O(1)`)으로 정산합니다.

## 2. 조작감 & 게임플레이 밸런스 원칙
- **스내피(Snappy) 반응성**: 퍼즐 타일 플립(BitFlip 70ms) 및 드래그 선로 연결(SignalLink Bresenham 보간)은 마우스 속도에 관계없이 끊김이나 입력 렉이 없어야 합니다.
- **100% 풀이 보장 & 허무 맵 차단**: 퍼즐 생성기는 역산 시뮬레이션 또는 검증 필터(Warnsdorff + D4 대칭 미로 폴백 등)를 통해 항상 풀 수 있으면서도 난이도에 걸맞은 깊이를 보장해야 합니다.
- **오피스 친화 UX**: 과도한 마우스 광클 노가다나 불쾌한 트롤링 기믹(커서 강제 이동, 화면 암전)을 배제하고, 무제한 Undo(`Z`), AI 힌트(`H`), 원클릭/자동 파견 등 쾌적한 전략적 재미를 지향합니다.

