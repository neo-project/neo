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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.钱包WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.创建钱包数据库NToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开钱包数据库OToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.修改密码CToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.退出XToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.交易TToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.签名SToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.帮助HToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.查看帮助VToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.官网WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.开发人员工具TToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.关于AntSharesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.显示详情DToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.复制到剪贴板CToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.资产分发IToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.钱包WToolStripMenuItem,
            this.交易TToolStripMenuItem,
            this.帮助HToolStripMenuItem});
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
            this.修改密码CToolStripMenuItem,
            this.toolStripSeparator2,
            this.退出XToolStripMenuItem});
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
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(184, 6);
            // 
            // 退出XToolStripMenuItem
            // 
            this.退出XToolStripMenuItem.Name = "退出XToolStripMenuItem";
            this.退出XToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.退出XToolStripMenuItem.Text = "退出(&X)";
            // 
            // 交易TToolStripMenuItem
            // 
            this.交易TToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.签名SToolStripMenuItem,
            this.toolStripSeparator5,
            this.资产分发IToolStripMenuItem});
            this.交易TToolStripMenuItem.Enabled = false;
            this.交易TToolStripMenuItem.Name = "交易TToolStripMenuItem";
            this.交易TToolStripMenuItem.Size = new System.Drawing.Size(59, 21);
            this.交易TToolStripMenuItem.Text = "交易(&T)";
            // 
            // 签名SToolStripMenuItem
            // 
            this.签名SToolStripMenuItem.Name = "签名SToolStripMenuItem";
            this.签名SToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.签名SToolStripMenuItem.Text = "签名(&S)...";
            this.签名SToolStripMenuItem.Click += new System.EventHandler(this.签名SToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(149, 6);
            // 
            // 帮助HToolStripMenuItem
            // 
            this.帮助HToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.查看帮助VToolStripMenuItem,
            this.官网WToolStripMenuItem,
            this.toolStripSeparator3,
            this.开发人员工具TToolStripMenuItem,
            this.toolStripSeparator4,
            this.关于AntSharesToolStripMenuItem});
            this.帮助HToolStripMenuItem.Name = "帮助HToolStripMenuItem";
            this.帮助HToolStripMenuItem.Size = new System.Drawing.Size(61, 21);
            this.帮助HToolStripMenuItem.Text = "帮助(&H)";
            // 
            // 查看帮助VToolStripMenuItem
            // 
            this.查看帮助VToolStripMenuItem.Name = "查看帮助VToolStripMenuItem";
            this.查看帮助VToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.查看帮助VToolStripMenuItem.Text = "查看帮助(&V)";
            // 
            // 官网WToolStripMenuItem
            // 
            this.官网WToolStripMenuItem.Name = "官网WToolStripMenuItem";
            this.官网WToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.官网WToolStripMenuItem.Text = "官网(&W)";
            this.官网WToolStripMenuItem.Click += new System.EventHandler(this.官网WToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(188, 6);
            // 
            // 开发人员工具TToolStripMenuItem
            // 
            this.开发人员工具TToolStripMenuItem.Name = "开发人员工具TToolStripMenuItem";
            this.开发人员工具TToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F12;
            this.开发人员工具TToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.开发人员工具TToolStripMenuItem.Text = "开发人员工具(&T)";
            this.开发人员工具TToolStripMenuItem.Click += new System.EventHandler(this.开发人员工具TToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(188, 6);
            // 
            // 关于AntSharesToolStripMenuItem
            // 
            this.关于AntSharesToolStripMenuItem.Name = "关于AntSharesToolStripMenuItem";
            this.关于AntSharesToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.关于AntSharesToolStripMenuItem.Text = "关于&AntShares";
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
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
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.显示详情DToolStripMenuItem,
            this.复制到剪贴板CToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(165, 48);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // 显示详情DToolStripMenuItem
            // 
            this.显示详情DToolStripMenuItem.Name = "显示详情DToolStripMenuItem";
            this.显示详情DToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.显示详情DToolStripMenuItem.Text = "显示详情(&D)...";
            this.显示详情DToolStripMenuItem.Click += new System.EventHandler(this.显示详情DToolStripMenuItem_Click);
            // 
            // 复制到剪贴板CToolStripMenuItem
            // 
            this.复制到剪贴板CToolStripMenuItem.Name = "复制到剪贴板CToolStripMenuItem";
            this.复制到剪贴板CToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.复制到剪贴板CToolStripMenuItem.Text = "复制到剪贴板(&C)";
            this.复制到剪贴板CToolStripMenuItem.Click += new System.EventHandler(this.复制到剪贴板CToolStripMenuItem_Click);
            // 
            // 资产分发IToolStripMenuItem
            // 
            this.资产分发IToolStripMenuItem.Name = "资产分发IToolStripMenuItem";
            this.资产分发IToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.资产分发IToolStripMenuItem.Text = "资产分发(&I)...";
            this.资产分发IToolStripMenuItem.Click += new System.EventHandler(this.资产分发IToolStripMenuItem_Click);
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
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem 退出XToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 帮助HToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 查看帮助VToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 官网WToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem 开发人员工具TToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem 关于AntSharesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 交易TToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 签名SToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 复制到剪贴板CToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 显示详情DToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem 资产分发IToolStripMenuItem;
    }
}

