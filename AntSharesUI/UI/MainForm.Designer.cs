namespace AntShares.UI
{
    partial class MainForm
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.钱包WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.创建钱包数据库NToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开钱包数据库OToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.修改密码CToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.钱包WToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 3, 0, 3);
            this.menuStrip1.Size = new System.Drawing.Size(493, 27);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 钱包WToolStripMenuItem
            // 
            this.钱包WToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.创建钱包数据库NToolStripMenuItem,
            this.打开钱包数据库OToolStripMenuItem,
            this.toolStripSeparator1,
            this.修改密码CToolStripMenuItem});
            this.钱包WToolStripMenuItem.Name = "钱包WToolStripMenuItem";
            this.钱包WToolStripMenuItem.Size = new System.Drawing.Size(64, 21);
            this.钱包WToolStripMenuItem.Text = "钱包(&W)";
            // 
            // 创建钱包数据库NToolStripMenuItem
            // 
            this.创建钱包数据库NToolStripMenuItem.Name = "创建钱包数据库NToolStripMenuItem";
            this.创建钱包数据库NToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.创建钱包数据库NToolStripMenuItem.Text = "创建钱包数据库(&N)...";
            this.创建钱包数据库NToolStripMenuItem.Click += new System.EventHandler(this.创建钱包数据库NToolStripMenuItem_Click);
            // 
            // 打开钱包数据库OToolStripMenuItem
            // 
            this.打开钱包数据库OToolStripMenuItem.Name = "打开钱包数据库OToolStripMenuItem";
            this.打开钱包数据库OToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.打开钱包数据库OToolStripMenuItem.Text = "打开钱包数据库(&O)...";
            this.打开钱包数据库OToolStripMenuItem.Click += new System.EventHandler(this.打开钱包数据库OToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(184, 6);
            // 
            // 修改密码CToolStripMenuItem
            // 
            this.修改密码CToolStripMenuItem.Enabled = false;
            this.修改密码CToolStripMenuItem.Name = "修改密码CToolStripMenuItem";
            this.修改密码CToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.修改密码CToolStripMenuItem.Text = "修改密码(&C)...";
            this.修改密码CToolStripMenuItem.Click += new System.EventHandler(this.修改密码CToolStripMenuItem_Click);
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(12, 30);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(469, 429);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "地址";
            this.columnHeader1.Width = 300;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(493, 544);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AntShares UI";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 钱包WToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 创建钱包数据库NToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 打开钱包数据库OToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem 修改密码CToolStripMenuItem;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
    }
}

