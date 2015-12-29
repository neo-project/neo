namespace AntShares.UI.HyperLib
{
    partial class WizardStep
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WizardStep));
            this.lbl_title = new System.Windows.Forms.Label();
            this.lbl_caption = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbl_title
            // 
            resources.ApplyResources(this.lbl_title, "lbl_title");
            this.lbl_title.Name = "lbl_title";
            // 
            // lbl_caption
            // 
            resources.ApplyResources(this.lbl_caption, "lbl_caption");
            this.lbl_caption.Name = "lbl_caption";
            // 
            // WizardStep
            // 
            this.Controls.Add(this.lbl_caption);
            this.Controls.Add(this.lbl_title);
            this.Name = "WizardStep";
            resources.ApplyResources(this, "$this");
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_title;
        private System.Windows.Forms.Label lbl_caption;
    }
}
