---
name: visual-inspector
description: >-
  On-demand WinForms offscreen bitmap capture workflow. ONLY activate when the
  user explicitly asks to capture or show a screenshot of a form/control. NEVER
  run automatically on normal UI or layout edits.
---

# On-Demand Visual Inspector Skill (`visual-inspector`)

> [!IMPORTANT]
> **절대 자동 실행 금지 (`task running` 무한 대기 방지 수칙)**
> 1. 평소 UI 레이아웃, 텍스트, 열 너비, 색상 수정 시에는 **절대 임의로 캡처용 `.exe`를 컴파일하거나 실행하지 마세요.** (MSBuild 빌드 성공 확인만으로 즉시 작업을 완료합니다.)
> 2. 오직 사용자가 **"화면 캡처해서 보여줘"**라고 명시적으로 요청했을 때만 이 스킬을 사용합니다.
> 3. 캡처 코드 작성 시 **`Application.Run(form)` 및 비동기 네트워크 호출(`async/await`)을 절대 금지**하며, 반드시 **4초 강제 종료 타이머(`System.Threading.Timer`)와 `Environment.Exit(0)`**을 포함해야 합니다.

---

## Safe Synchronous Capture Template (4-Second Hard Timeout)

```csharp
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

class CaptureRunner
{
    [STAThread]
    static void Main(string[] args)
    {
        // 4초 초과 시 무조건 프로세스 강제 종료 (백그라운드 스레드/타이머로 인한 무한 대기 원천 차단)
        using (new System.Threading.Timer(_ => Environment.Exit(0), null, 4000, Timeout.Infinite))
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var host = new Form { Size = new Size(1280, 820), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
                {
                    host.Show();
                    host.PerformLayout();
                    host.Refresh();
                    Application.DoEvents();

                    using (var bmp = new Bitmap(host.Width, host.Height))
                    {
                        host.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        bmp.Save(args[0], ImageFormat.Png);
                    }
                    host.Close();
                }
            }
            catch { }
            Environment.Exit(0);
        }
    }
}
```
