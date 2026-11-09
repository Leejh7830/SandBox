using System;
using System.Windows.Forms;

namespace GateHelper
{
    public partial class GameListControl : UserControl
    {
        public GameListControl()
        {
            InitializeComponent();
        }

        private void btnSelectBitFlip_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("BitFlip");
            }
        }

        private void GL_btnSelectSignalLink_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("SignalLink");
            }
        }

        private void btnSelectMysteryTime_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("MysteryTime");
            }
        }

        private void GL_btnSelectAiTrend_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("AiTrend");
            }
        }

        private void GL_btnSelectServerTycoon_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("ServerTycoon");
            }
        }

        private void GL_btnSelectDataFlip_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("DataFlip");
            }
        }

        private void GL_btnSelectPocketFarm_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.SwitchToGame("PocketFarm");
            }
        }
    }
}