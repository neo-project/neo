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
            System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("unchecked", System.Windows.Forms.HorizontalAlignment.Left);
            System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("checked", System.Windows.Forms.HorizontalAlignment.Left);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.钱包WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.创建钱包数据库NToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开钱包数据库OToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.修改密码CToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.退出XToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.交易TToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.转账TToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.签名SToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.高级AToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.注册资产RToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.资产分发IToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.帮助HToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.查看帮助VToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.官网WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.开发人员工具TToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.关于AntSharesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ContractListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.创建新地址NToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.导入私钥IToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importWIFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importCertificateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.创建智能合约SToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.多方签名MToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.查看私钥VToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.复制到剪贴板CToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.删除DToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lbl_height = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lbl_count_node = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.listView2 = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.钱包WToolStripMenuItem,
            this.交易TToolStripMenuItem,
            this.高级AToolStripMenuItem,
            this.帮助HToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 3, 0, 3);
            this.menuStrip1.Size = new System.Drawing.Size(756, 27);
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
            this.退出XToolStripMenuItem.Click += new System.EventHandler(this.退出XToolStripMenuItem_Click);
            // 
            // 交易TToolStripMenuItem
            // 
            this.交易TToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.转账TToolStripMenuItem,
            this.toolStripSeparator5,
            this.签名SToolStripMenuItem});
            this.交易TToolStripMenuItem.Enabled = false;
            this.交易TToolStripMenuItem.Name = "交易TToolStripMenuItem";
            this.交易TToolStripMenuItem.Size = new System.Drawing.Size(59, 21);
            this.交易TToolStripMenuItem.Text = "交易(&T)";
            // 
            // 转账TToolStripMenuItem
            // 
            this.转账TToolStripMenuItem.Name = "转账TToolStripMenuItem";
            this.转账TToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.转账TToolStripMenuItem.Text = "转账(&T)...";
            this.转账TToolStripMenuItem.Click += new System.EventHandler(this.转账TToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(121, 6);
            // 
            // 签名SToolStripMenuItem
            // 
            this.签名SToolStripMenuItem.Name = "签名SToolStripMenuItem";
            this.签名SToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.签名SToolStripMenuItem.Text = "签名(&S)...";
            this.签名SToolStripMenuItem.Click += new System.EventHandler(this.签名SToolStripMenuItem_Click);
            // 
            // 高级AToolStripMenuItem
            // 
            this.高级AToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.注册资产RToolStripMenuItem,
            this.资产分发IToolStripMenuItem});
            this.高级AToolStripMenuItem.Enabled = false;
            this.高级AToolStripMenuItem.Name = "高级AToolStripMenuItem";
            this.高级AToolStripMenuItem.Size = new System.Drawing.Size(60, 21);
            this.高级AToolStripMenuItem.Text = "高级(&A)";
            // 
            // 注册资产RToolStripMenuItem
            // 
            this.注册资产RToolStripMenuItem.Name = "注册资产RToolStripMenuItem";
            this.注册资产RToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.注册资产RToolStripMenuItem.Text = "注册资产(&R)...";
            this.注册资产RToolStripMenuItem.Click += new System.EventHandler(this.注册资产RToolStripMenuItem_Click);
            // 
            // 资产分发IToolStripMenuItem
            // 
            this.资产分发IToolStripMenuItem.Name = "资产分发IToolStripMenuItem";
            this.资产分发IToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.资产分发IToolStripMenuItem.Text = "发行资产(&I)...";
            this.资产分发IToolStripMenuItem.Click += new System.EventHandler(this.资产分发IToolStripMenuItem_Click);
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
            this.关于AntSharesToolStripMenuItem.Click += new System.EventHandler(this.关于AntSharesToolStripMenuItem_Click);
            // 
            // ContractListView
            // 
            this.ContractListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader4});
            this.ContractListView.ContextMenuStrip = this.contextMenuStrip1;
            this.ContractListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContractListView.FullRowSelect = true;
            this.ContractListView.GridLines = true;
            this.ContractListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.ContractListView.HideSelection = false;
            this.ContractListView.Location = new System.Drawing.Point(3, 3);
            this.ContractListView.Name = "ContractListView";
            this.ContractListView.Size = new System.Drawing.Size(742, 457);
            this.ContractListView.TabIndex = 1;
            this.ContractListView.UseCompatibleStateImageBehavior = false;
            this.ContractListView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "地址";
            this.columnHeader1.Width = 300;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "类型";
            this.columnHeader4.Width = 270;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.创建新地址NToolStripMenuItem,
            this.导入私钥IToolStripMenuItem,
            this.创建智能合约SToolStripMenuItem,
            this.toolStripSeparator6,
            this.查看私钥VToolStripMenuItem,
            this.复制到剪贴板CToolStripMenuItem,
            this.删除DToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(174, 186);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // 创建新地址NToolStripMenuItem
            // 
            this.创建新地址NToolStripMenuItem.Enabled = false;
            this.创建新地址NToolStripMenuItem.Name = "创建新地址NToolStripMenuItem";
            this.创建新地址NToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.创建新地址NToolStripMenuItem.Text = "创建新地址(&N)";
            this.创建新地址NToolStripMenuItem.Click += new System.EventHandler(this.创建新地址NToolStripMenuItem_Click);
            // 
            // 导入私钥IToolStripMenuItem
            // 
            this.导入私钥IToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importWIFToolStripMenuItem,
            this.importCertificateToolStripMenuItem});
            this.导入私钥IToolStripMenuItem.Enabled = false;
            this.导入私钥IToolStripMenuItem.Name = "导入私钥IToolStripMenuItem";
            this.导入私钥IToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.导入私钥IToolStripMenuItem.Text = "导入私钥(&I)...";
            // 
            // importWIFToolStripMenuItem
            // 
            this.importWIFToolStripMenuItem.Name = "importWIFToolStripMenuItem";
            this.importWIFToolStripMenuItem.Size = new System.Drawing.Size(331, 38);
            this.importWIFToolStripMenuItem.Text = "导入&WIF...";
            this.importWIFToolStripMenuItem.Click += new System.EventHandler(this.importWIFToolStripMenuItem_Click);
            // 
            // importCertificateToolStripMenuItem
            // 
            this.importCertificateToolStripMenuItem.Name = "importCertificateToolStripMenuItem";
            this.importCertificateToolStripMenuItem.Size = new System.Drawing.Size(331, 38);
            this.importCertificateToolStripMenuItem.Text = "导入证书(&C)...";
            this.importCertificateToolStripMenuItem.Click += new System.EventHandler(this.importCertificateToolStripMenuItem_Click);
            // 
            // 创建智能合约SToolStripMenuItem
            // 
            this.创建智能合约SToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.多方签名MToolStripMenuItem});
            this.创建智能合约SToolStripMenuItem.Enabled = false;
            this.创建智能合约SToolStripMenuItem.Name = "创建智能合约SToolStripMenuItem";
            this.创建智能合约SToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.创建智能合约SToolStripMenuItem.Text = "创建智能合约(&S)";
            // 
            // 多方签名MToolStripMenuItem
            // 
            this.多方签名MToolStripMenuItem.Name = "多方签名MToolStripMenuItem";
            this.多方签名MToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.多方签名MToolStripMenuItem.Text = "多方签名(&M)...";
            this.多方签名MToolStripMenuItem.Click += new System.EventHandler(this.多方签名MToolStripMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(170, 6);
            // 
            // 查看私钥VToolStripMenuItem
            // 
            this.查看私钥VToolStripMenuItem.Enabled = false;
            this.查看私钥VToolStripMenuItem.Name = "查看私钥VToolStripMenuItem";
            this.查看私钥VToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.查看私钥VToolStripMenuItem.Text = "查看私钥(&V)";
            this.查看私钥VToolStripMenuItem.Click += new System.EventHandler(this.查看私钥VToolStripMenuItem_Click);
            // 
            // 复制到剪贴板CToolStripMenuItem
            // 
            this.复制到剪贴板CToolStripMenuItem.Enabled = false;
            this.复制到剪贴板CToolStripMenuItem.Name = "复制到剪贴板CToolStripMenuItem";
            this.复制到剪贴板CToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.复制到剪贴板CToolStripMenuItem.ShowShortcutKeys = false;
            this.复制到剪贴板CToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.复制到剪贴板CToolStripMenuItem.Text = "复制到剪贴板(&C)";
            this.复制到剪贴板CToolStripMenuItem.Click += new System.EventHandler(this.复制到剪贴板CToolStripMenuItem_Click);
            // 
            // 删除DToolStripMenuItem
            // 
            this.删除DToolStripMenuItem.Enabled = false;
            this.删除DToolStripMenuItem.Name = "删除DToolStripMenuItem";
            this.删除DToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.删除DToolStripMenuItem.Text = "删除(&D)...";
            this.删除DToolStripMenuItem.Click += new System.EventHandler(this.删除DToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.lbl_height,
            this.toolStripStatusLabel4,
            this.lbl_count_node});
            this.statusStrip1.Location = new System.Drawing.Point(0, 520);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(756, 22);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(35, 17);
            this.toolStripStatusLabel1.Text = "高度:";
            // 
            // lbl_height
            // 
            this.lbl_height.Name = "lbl_height";
            this.lbl_height.Size = new System.Drawing.Size(27, 17);
            this.lbl_height.Text = "0/0";
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(47, 17);
            this.toolStripStatusLabel4.Text = "连接数:";
            // 
            // lbl_count_node
            // 
            this.lbl_count_node.Name = "lbl_count_node";
            this.lbl_count_node.Size = new System.Drawing.Size(15, 17);
            this.lbl_count_node.Text = "0";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 27);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(756, 493);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.ContractListView);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(748, 463);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "账户";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.listView2);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(748, 463);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "资产";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // listView2
            // 
            this.listView2.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2,
            this.columnHeader6,
            this.columnHeader3,
            this.columnHeader5});
            this.listView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView2.FullRowSelect = true;
            this.listView2.GridLines = true;
            listViewGroup1.Header = "unchecked";
            listViewGroup1.Name = "unchecked";
            listViewGroup2.Header = "checked";
            listViewGroup2.Name = "checked";
            this.listView2.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
            this.listView2.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView2.HideSelection = false;
            this.listView2.Location = new System.Drawing.Point(3, 3);
            this.listView2.Name = "listView2";
            this.listView2.ShowGroups = false;
            this.listView2.Size = new System.Drawing.Size(742, 457);
            this.listView2.TabIndex = 2;
            this.listView2.UseCompatibleStateImageBehavior = false;
            this.listView2.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "资产";
            this.columnHeader2.Width = 160;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "类型";
            this.columnHeader6.Width = 100;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "余额";
            this.columnHeader3.Width = 150;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "发行者";
            this.columnHeader5.Width = 296;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(756, 542);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AntShares UI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripMenuItem 创建新地址NToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 导入私钥IToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem 查看私钥VToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 复制到剪贴板CToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 删除DToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel lbl_height;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        private System.Windows.Forms.ToolStripStatusLabel lbl_count_node;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ListView listView2;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ToolStripMenuItem 转账TToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 高级AToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 注册资产RToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 资产分发IToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 创建智能合约SToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 多方签名MToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ListView ContractListView;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ToolStripMenuItem importWIFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importCertificateToolStripMenuItem;
    }
}

