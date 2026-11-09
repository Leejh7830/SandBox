using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper
{
    public enum ToastIcon
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class ToastNotification : Form
    {
        private Timer _animationTimer;
        private bool _isClosing = false;
        private const double OpacityStep = 0.08;

        public ToastNotification(string message, string title, ToastIcon iconType)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(40, 40, 40); // Dark Gray
            this.ForeColor = Color.White;
            this.Size = new Size(380, 90);
            this.Opacity = 0.0;
            this.Padding = new Padding(15);
            
            // To make it rounded
            this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));

            // Icon
            PictureBox pbIcon = new PictureBox();
            pbIcon.Size = new Size(32, 32);
            pbIcon.Location = new Point(15, 29);
            pbIcon.SizeMode = PictureBoxSizeMode.Zoom;
            
            switch (iconType)
            {
                case ToastIcon.Warning:
                    pbIcon.Image = SystemIcons.Warning.ToBitmap();
                    break;
                case ToastIcon.Error:
                    pbIcon.Image = SystemIcons.Error.ToBitmap();
                    break;
                case ToastIcon.Info:
                    pbIcon.Image = SystemIcons.Information.ToBitmap();
                    break;
                case ToastIcon.Success:
                    // Using asterisk as a fallback for success if no specific icon exists
                    pbIcon.Image = SystemIcons.Asterisk.ToBitmap();
                    break;
            }

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = $"[GateHelper] {title}";
            lblTitle.Font = new Font("Malgun Gothic", 10, FontStyle.Bold);
            lblTitle.Location = new Point(60, 15);
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Color.FromArgb(255, 215, 0); // Gold/Yellowish for visibility

            // Message
            Label lblMessage = new Label();
            lblMessage.Text = message;
            lblMessage.Font = new Font("Malgun Gothic", 9, FontStyle.Regular);
            lblMessage.Location = new Point(60, 38);
            lblMessage.Size = new Size(300, 45);
            lblMessage.ForeColor = Color.FromArgb(220, 220, 220);
            
            // Close Hint
            Label lblCloseHint = new Label();
            lblCloseHint.Text = "클릭하여 닫기";
            lblCloseHint.Font = new Font("Malgun Gothic", 8, FontStyle.Regular);
            lblCloseHint.ForeColor = Color.Gray;
            lblCloseHint.AutoSize = true;
            lblCloseHint.Location = new Point(this.Width - 85, 10);

            this.Controls.Add(pbIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblMessage);
            this.Controls.Add(lblCloseHint);

            // Add click events to dismiss
            this.Click += ToastNotification_Click;
            pbIcon.Click += ToastNotification_Click;
            lblTitle.Click += ToastNotification_Click;
            lblMessage.Click += ToastNotification_Click;
            lblCloseHint.Click += ToastNotification_Click;

            // Positioning at bottom right
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(workingArea.Right - this.Width - 20, workingArea.Bottom - this.Height - 20);

            // Animation Timer
            _animationTimer = new Timer();
            _animationTimer.Interval = 20; // 20ms
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void ToastNotification_Click(object sender, EventArgs e)
        {
            if (!_isClosing)
            {
                _isClosing = true;
                _animationTimer.Start(); // Start fade out
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!_isClosing)
            {
                // Fade In
                if (this.Opacity < 1.0)
                {
                    this.Opacity += OpacityStep;
                }
                else
                {
                    this.Opacity = 1.0;
                    _animationTimer.Stop(); // Stay visible until clicked
                }
            }
            else
            {
                // Fade Out
                if (this.Opacity > 0.0)
                {
                    this.Opacity -= OpacityStep;
                }
                else
                {
                    _animationTimer.Stop();
                    this.Close();
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _animationTimer.Start();
        }

        // Must be called on UI thread
        public static void Show(string message, string title = "알림", ToastIcon icon = ToastIcon.Info)
        {
            // Create and show it
            // Normally Show() is non-blocking. If we are called from a background thread, 
            // we might need Control.Invoke. But PerformGateOneAutoLogin is called directly from BtnStart2_Click (UI thread).
            ToastNotification toast = new ToastNotification(message, title, icon);
            toast.Show();
        }

        // P/Invoke for rounded corners
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );
    }
}
