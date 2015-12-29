namespace AntShares.UI.HyperLib
{
    partial class Wizard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Wizard));
            this.btn_exit = new System.Windows.Forms.Button();
            this.btn_step_next = new System.Windows.Forms.Button();
            this.btn_step_prev = new System.Windows.Forms.Button();
            this.pnl_steps = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // btn_exit
            // 
            resources.ApplyResources(this.btn_exit, "btn_exit");
            this.btn_exit.Name = "btn_exit";
            this.btn_exit.UseVisualStyleBackColor = true;
            this.btn_exit.Click += new System.EventHandler(this.btn_exit_Click);
            // 
            // btn_step_next
            // 
            resources.ApplyResources(this.btn_step_next, "btn_step_next");
            this.btn_step_next.Name = "btn_step_next";
            this.btn_step_next.UseVisualStyleBackColor = true;
            this.btn_step_next.Click += new System.EventHandler(this.btn_step_next_Click);
            // 
            // btn_step_prev
            // 
            resources.ApplyResources(this.btn_step_prev, "btn_step_prev");
            this.btn_step_prev.Name = "btn_step_prev";
            this.btn_step_prev.UseVisualStyleBackColor = true;
            this.btn_step_prev.Click += new System.EventHandler(this.btn_step_prev_Click);
            // 
            // pnl_steps
            // 
            this.pnl_steps.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.pnl_steps, "pnl_steps");
            this.pnl_steps.Name = "pnl_steps";
            // 
            // Wizard
            // 
            this.AcceptButton = this.btn_step_next;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnl_steps);
            this.Controls.Add(this.btn_step_prev);
            this.Controls.Add(this.btn_step_next);
            this.Controls.Add(this.btn_exit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "Wizard";
            this.ShowInTaskbar = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Wizard_FormClosing);
            this.Load += new System.EventHandler(this.Wizard_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_exit;
        private System.Windows.Forms.Button btn_step_next;
        private System.Windows.Forms.Button btn_step_prev;
        private System.Windows.Forms.Panel pnl_steps;

    }
}