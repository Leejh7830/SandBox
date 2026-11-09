---
name: visual-inspector
description: >-
  Automated WinForms offscreen bitmap capture and AI visual inspection workflow
  for GateHelper. Activate after modifying WinForms UI layouts, control
  coordinates, or GDI+ OnPaint rendering in WorkLog or SandBox so the agent can
  capture a real rendered PNG of the form/control, visually inspect it via
  read_file for clipping/overlap, and show the screenshot to the user.
---

# GateHelper Automated Visual Inspector Skill (`visual-inspector`)

Use this skill after making visual or layout changes to any WinForms `Form` or `UserControl` (such as `WorkLogForm`, `BitFlipControl`, `SignalLinkControl`, `DataFlipControl`, `PocketFarmControl`, or `ServerTycoonControl`) so the user doesn't have to manually launch `GateHelper.exe` just to verify the screen.

---

## Step-by-Step Workflow

### 1. Build Release Binary First
Ensure `GateHelper.csproj` compiles cleanly with `0 Warnings, 0 Errors`:
```powershell
Stop-Process -Name GateHelper -Force -ErrorAction SilentlyContinue
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild.exe" GateHelper.csproj /p:Configuration=Release /t:Rebuild
```
*(Or use the standard VS2022 Community MSBuild path: `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe`)*

### 2. Capture Actual Rendered Form/Control to PNG
Create a temporary C# harness in `<appDataDir>\brain\<conversation-id>\scratch\capture_ui.cs`, compile its executable directly into `bin\Release\__temp_capture.exe` (so `.NET Framework` automatically resolves `GateHelper.exe`, `MaterialSkin.dll`, `ObjectListView.dll`, and `Newtonsoft.Json.dll` in the same folder), run it, and immediately delete `__temp_capture.exe` and `capture_ui.cs`:

```csharp
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

class CaptureRunner
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Example: Instantiate target Form or UserControl inside a host Form
        using (var host = new Form { Size = new Size(1280, 820), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
        {
            // Attach target control or use target form directly
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
}
```
- **Important**: Always isolate any test state in memory or a temporary folder; **never** modify real user data in `_meta/`.
- Delete the temporary `.cs` and `.exe` harness files immediately after capturing the `.png` into `<appDataDir>\brain\<conversation-id>\`.

### 3. AI Visual Self-Inspection (`read_file`)
Call `read_file` on the generated `.png` file in `<appDataDir>\brain\<conversation-id>\`.
Inspect the rendered image with your own eyes for:
- Text truncation or ellipsis (`...`) on buttons/labels
- 1px border misalignment between rows/columns
- Low contrast or overlapping controls
- Tofu boxes (`□`) from unsupported glyphs

If any visual defect is spotted, fix the C# code and re-capture before reporting to the user.

### 4. Present to User
Embed or link the verified screenshot in the completion report alongside the inline Squad Status HUD (`<agent-embed>`).

