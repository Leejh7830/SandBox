namespace GateHelper.PocketFarm
{
    partial class PocketFarmControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_model != null)
                {
                    _model.Save();
                }
                if (components != null)
                {
                    components.Dispose();
                }
                DisposeGdiResources();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PocketFarmControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Name = "PocketFarmControl";
            this.Size = new System.Drawing.Size(650, 630);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
