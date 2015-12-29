namespace AntShares.UI
{
    partial class CertificateRequestDialog
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
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.wizardStep1 = new AntShares.UI.HyperLib.WizardStep();
            this.wizardStep2 = new AntShares.UI.HyperLib.WizardStep();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.wizardStep3 = new AntShares.UI.HyperLib.WizardStep();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.wizardStep4 = new AntShares.UI.HyperLib.WizardStep();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.wizardStep5 = new AntShares.UI.HyperLib.WizardStep();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.wizardStep6 = new AntShares.UI.HyperLib.WizardStep();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox11 = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.wizardStep2.SuspendLayout();
            this.wizardStep3.SuspendLayout();
            this.wizardStep4.SuspendLayout();
            this.wizardStep5.SuspendLayout();
            this.wizardStep6.SuspendLayout();
            this.SuspendLayout();
            // 
            // wizardStep1
            // 
            this.wizardStep1.Caption = "此向导将帮助您创建一个证书申请文件，随后您可用该文件向CA提交一个证书申请。\r\n若要开始，请点击下一步。";
            this.wizardStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardStep1.Location = new System.Drawing.Point(0, 0);
            this.wizardStep1.Name = "wizardStep1";
            this.wizardStep1.Size = new System.Drawing.Size(509, 397);
            this.wizardStep1.TabIndex = 0;
            this.wizardStep1.Title = "使用此向导创建证书申请";
            // 
            // wizardStep2
            // 
            this.wizardStep2.Caption = "您需要选择一个用户类型，然后点击下一步。";
            this.wizardStep2.Controls.Add(this.radioButton2);
            this.wizardStep2.Controls.Add(this.radioButton1);
            this.wizardStep2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardStep2.Location = new System.Drawing.Point(0, 0);
            this.wizardStep2.Name = "wizardStep2";
            this.wizardStep2.Size = new System.Drawing.Size(509, 397);
            this.wizardStep2.TabIndex = 0;
            this.wizardStep2.Title = "选择用户类型";
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(121, 101);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(71, 16);
            this.radioButton2.TabIndex = 3;
            this.radioButton2.Text = "企业用户";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(121, 79);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(71, 16);
            this.radioButton1.TabIndex = 2;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "个人用户";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // wizardStep3
            // 
            this.wizardStep3.Caption = "详细填写您的个人实名信息，必须真实有效，否则无法通过认证。";
            this.wizardStep3.Controls.Add(this.textBox4);
            this.wizardStep3.Controls.Add(this.label4);
            this.wizardStep3.Controls.Add(this.textBox3);
            this.wizardStep3.Controls.Add(this.label3);
            this.wizardStep3.Controls.Add(this.textBox2);
            this.wizardStep3.Controls.Add(this.label2);
            this.wizardStep3.Controls.Add(this.textBox1);
            this.wizardStep3.Controls.Add(this.label1);
            this.wizardStep3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardStep3.Location = new System.Drawing.Point(0, 0);
            this.wizardStep3.Name = "wizardStep3";
            this.wizardStep3.Size = new System.Drawing.Size(509, 397);
            this.wizardStep3.TabIndex = 0;
            this.wizardStep3.Title = "个人实名信息";
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(163, 158);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(266, 21);
            this.textBox4.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(122, 161);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "国家:";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(163, 131);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(266, 21);
            this.textBox3.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(110, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 12);
            this.label3.TabIndex = 6;
            this.label3.Text = "身份证:";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(163, 104);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(266, 21);
            this.textBox2.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(98, 107);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "电子邮件:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(163, 77);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(266, 21);
            this.textBox1.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(122, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "姓名:";
            // 
            // wizardStep4
            // 
            this.wizardStep4.Caption = "详细填写您的企业信息，必须真实有效，否则无法通过认证。";
            this.wizardStep4.Controls.Add(this.textBox9);
            this.wizardStep4.Controls.Add(this.label9);
            this.wizardStep4.Controls.Add(this.textBox5);
            this.wizardStep4.Controls.Add(this.label5);
            this.wizardStep4.Controls.Add(this.textBox6);
            this.wizardStep4.Controls.Add(this.label6);
            this.wizardStep4.Controls.Add(this.textBox7);
            this.wizardStep4.Controls.Add(this.label7);
            this.wizardStep4.Controls.Add(this.textBox8);
            this.wizardStep4.Controls.Add(this.label8);
            this.wizardStep4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardStep4.Location = new System.Drawing.Point(0, 0);
            this.wizardStep4.Name = "wizardStep4";
            this.wizardStep4.Size = new System.Drawing.Size(509, 397);
            this.wizardStep4.TabIndex = 0;
            this.wizardStep4.Title = "企业实名信息";
            // 
            // textBox9
            // 
            this.textBox9.Location = new System.Drawing.Point(163, 185);
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new System.Drawing.Size(266, 21);
            this.textBox9.TabIndex = 19;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(98, 188);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(59, 12);
            this.label9.TabIndex = 18;
            this.label9.Text = "营业执照:";
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(163, 158);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(266, 21);
            this.textBox5.TabIndex = 17;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(122, 161);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 12);
            this.label5.TabIndex = 16;
            this.label5.Text = "国家:";
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(163, 131);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(266, 21);
            this.textBox6.TabIndex = 15;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(74, 134);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(83, 12);
            this.label6.TabIndex = 14;
            this.label6.Text = "省/市/自治区:";
            // 
            // textBox7
            // 
            this.textBox7.Location = new System.Drawing.Point(163, 104);
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(266, 21);
            this.textBox7.TabIndex = 13;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(98, 107);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(59, 12);
            this.label7.TabIndex = 12;
            this.label7.Text = "街道地址:";
            // 
            // textBox8
            // 
            this.textBox8.Location = new System.Drawing.Point(163, 77);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(266, 21);
            this.textBox8.TabIndex = 11;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(98, 80);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 12);
            this.label8.TabIndex = 10;
            this.label8.Text = "企业名称:";
            // 
            // wizardStep5
            // 
            this.wizardStep5.Caption = "请仔细核对以下信息准确无误后，点击下一步。";
            this.wizardStep5.Controls.Add(this.textBox10);
            this.wizardStep5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardStep5.Location = new System.Drawing.Point(0, 0);
            this.wizardStep5.Name = "wizardStep5";
            this.wizardStep5.Size = new System.Drawing.Size(509, 397);
            this.wizardStep5.TabIndex = 0;
            this.wizardStep5.Title = "信息核对";
            // 
            // textBox10
            // 
            this.textBox10.Location = new System.Drawing.Point(20, 66);
            this.textBox10.Multiline = true;
            this.textBox10.Name = "textBox10";
            this.textBox10.ReadOnly = true;
            this.textBox10.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox10.Size = new System.Drawing.Size(471, 318);
            this.textBox10.TabIndex = 2;
            // 
            // wizardStep6
            // 
            this.wizardStep6.Caption = "选择证书申请文件要保存在位置，然后点击完成按钮保存。";
            this.wizardStep6.Controls.Add(this.button1);
            this.wizardStep6.Controls.Add(this.textBox11);
            this.wizardStep6.Controls.Add(this.label10);
            this.wizardStep6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wizardStep6.Location = new System.Drawing.Point(0, 0);
            this.wizardStep6.Name = "wizardStep6";
            this.wizardStep6.Size = new System.Drawing.Size(509, 397);
            this.wizardStep6.TabIndex = 0;
            this.wizardStep6.Title = "保存申请文件";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(435, 77);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(31, 23);
            this.button1.TabIndex = 13;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox11
            // 
            this.textBox11.Location = new System.Drawing.Point(86, 78);
            this.textBox11.Name = "textBox11";
            this.textBox11.ReadOnly = true;
            this.textBox11.Size = new System.Drawing.Size(343, 21);
            this.textBox11.TabIndex = 12;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(21, 81);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(59, 12);
            this.label10.TabIndex = 2;
            this.label10.Text = "文件位置:";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "req";
            this.saveFileDialog1.Filter = "证书申请文件|*.req";
            // 
            // CertificateRequestDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.ClientSize = new System.Drawing.Size(634, 452);
            this.MinimizeBox = false;
            this.Name = "CertificateRequestDialog";
            this.Steps.Add(this.wizardStep1);
            this.Steps.Add(this.wizardStep2);
            this.Steps.Add(this.wizardStep3);
            this.Steps.Add(this.wizardStep4);
            this.Steps.Add(this.wizardStep5);
            this.Steps.Add(this.wizardStep6);
            this.WizardStepChanged += new System.EventHandler<AntShares.UI.HyperLib.WizardStepChangedEventArgs>(this.CertificateRequestDialog_WizardStepChanged);
            this.WizardFinished += new System.EventHandler(this.CertificateRequestDialog_WizardFinished);
            this.wizardStep2.ResumeLayout(false);
            this.wizardStep2.PerformLayout();
            this.wizardStep3.ResumeLayout(false);
            this.wizardStep3.PerformLayout();
            this.wizardStep4.ResumeLayout(false);
            this.wizardStep4.PerformLayout();
            this.wizardStep5.ResumeLayout(false);
            this.wizardStep5.PerformLayout();
            this.wizardStep6.ResumeLayout(false);
            this.wizardStep6.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private HyperLib.WizardStep wizardStep1;
        private HyperLib.WizardStep wizardStep2;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private HyperLib.WizardStep wizardStep3;
        private HyperLib.WizardStep wizardStep4;
        private HyperLib.WizardStep wizardStep5;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox7;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox8;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox10;
        private HyperLib.WizardStep wizardStep6;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}
