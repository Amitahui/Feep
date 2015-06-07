namespace Feep.Configure
{
    partial class Configure
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Configure));
            this.gbFiles = new System.Windows.Forms.GroupBox();
            this.cbTIF = new System.Windows.Forms.CheckBox();
            this.cbGIF = new System.Windows.Forms.CheckBox();
            this.cbPNG = new System.Windows.Forms.CheckBox();
            this.cbBMP = new System.Windows.Forms.CheckBox();
            this.cbJPG = new System.Windows.Forms.CheckBox();
            this.btnSetting = new System.Windows.Forms.Button();
            this.gbBackColor = new System.Windows.Forms.GroupBox();
            this.txtColor = new System.Windows.Forms.TextBox();
            this.lblColor = new System.Windows.Forms.Label();
            this.pnlColor = new System.Windows.Forms.Panel();
            this.gbFiles.SuspendLayout();
            this.gbBackColor.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbFiles
            // 
            this.gbFiles.Controls.Add(this.cbTIF);
            this.gbFiles.Controls.Add(this.cbGIF);
            this.gbFiles.Controls.Add(this.cbPNG);
            this.gbFiles.Controls.Add(this.cbBMP);
            this.gbFiles.Controls.Add(this.cbJPG);
            this.gbFiles.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gbFiles.Location = new System.Drawing.Point(12, 12);
            this.gbFiles.Name = "gbFiles";
            this.gbFiles.Size = new System.Drawing.Size(402, 65);
            this.gbFiles.TabIndex = 0;
            this.gbFiles.TabStop = false;
            this.gbFiles.Text = "关联文件";
            // 
            // cbTIF
            // 
            this.cbTIF.AutoSize = true;
            this.cbTIF.Checked = true;
            this.cbTIF.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbTIF.Location = new System.Drawing.Point(344, 30);
            this.cbTIF.Name = "cbTIF";
            this.cbTIF.Size = new System.Drawing.Size(47, 24);
            this.cbTIF.TabIndex = 4;
            this.cbTIF.Text = "TIF";
            this.cbTIF.UseVisualStyleBackColor = true;
            // 
            // cbGIF
            // 
            this.cbGIF.AutoSize = true;
            this.cbGIF.Checked = true;
            this.cbGIF.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbGIF.Location = new System.Drawing.Point(269, 30);
            this.cbGIF.Name = "cbGIF";
            this.cbGIF.Size = new System.Drawing.Size(49, 24);
            this.cbGIF.TabIndex = 3;
            this.cbGIF.Text = "GIF";
            this.cbGIF.UseVisualStyleBackColor = true;
            // 
            // cbPNG
            // 
            this.cbPNG.AutoSize = true;
            this.cbPNG.Checked = true;
            this.cbPNG.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbPNG.Location = new System.Drawing.Point(185, 30);
            this.cbPNG.Name = "cbPNG";
            this.cbPNG.Size = new System.Drawing.Size(58, 24);
            this.cbPNG.TabIndex = 2;
            this.cbPNG.Text = "PNG";
            this.cbPNG.UseVisualStyleBackColor = true;
            // 
            // cbBMP
            // 
            this.cbBMP.AutoSize = true;
            this.cbBMP.Checked = true;
            this.cbBMP.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbBMP.Location = new System.Drawing.Point(99, 30);
            this.cbBMP.Name = "cbBMP";
            this.cbBMP.Size = new System.Drawing.Size(60, 24);
            this.cbBMP.TabIndex = 1;
            this.cbBMP.Text = "BMP";
            this.cbBMP.UseVisualStyleBackColor = true;
            // 
            // cbJPG
            // 
            this.cbJPG.AutoSize = true;
            this.cbJPG.Checked = true;
            this.cbJPG.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbJPG.Location = new System.Drawing.Point(20, 30);
            this.cbJPG.Name = "cbJPG";
            this.cbJPG.Size = new System.Drawing.Size(53, 24);
            this.cbJPG.TabIndex = 0;
            this.cbJPG.Text = "JPG";
            this.cbJPG.UseVisualStyleBackColor = true;
            // 
            // btnSetting
            // 
            this.btnSetting.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetting.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSetting.Location = new System.Drawing.Point(354, 193);
            this.btnSetting.Name = "btnSetting";
            this.btnSetting.Size = new System.Drawing.Size(60, 28);
            this.btnSetting.TabIndex = 2;
            this.btnSetting.Text = "设置";
            this.btnSetting.UseVisualStyleBackColor = true;
            this.btnSetting.Click += new System.EventHandler(this.btnSetting_Click);
            // 
            // gbBackColor
            // 
            this.gbBackColor.Controls.Add(this.txtColor);
            this.gbBackColor.Controls.Add(this.lblColor);
            this.gbBackColor.Controls.Add(this.pnlColor);
            this.gbBackColor.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gbBackColor.Location = new System.Drawing.Point(12, 83);
            this.gbBackColor.Name = "gbBackColor";
            this.gbBackColor.Size = new System.Drawing.Size(402, 65);
            this.gbBackColor.TabIndex = 1;
            this.gbBackColor.TabStop = false;
            this.gbBackColor.Text = "背景颜色";
            // 
            // txtColor
            // 
            this.txtColor.Location = new System.Drawing.Point(119, 28);
            this.txtColor.MaxLength = 6;
            this.txtColor.Name = "txtColor";
            this.txtColor.Size = new System.Drawing.Size(65, 26);
            this.txtColor.TabIndex = 2;
            this.txtColor.Text = "444444";
            this.txtColor.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtColor.MouseClick += new System.Windows.Forms.MouseEventHandler(this.txtColor_MouseClick);
            this.txtColor.TextChanged += new System.EventHandler(this.txtColor_TextChanged);
            // 
            // lblColor
            // 
            this.lblColor.AutoSize = true;
            this.lblColor.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblColor.Location = new System.Drawing.Point(95, 28);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new System.Drawing.Size(24, 26);
            this.lblColor.TabIndex = 1;
            this.lblColor.Text = "#";
            // 
            // pnlColor
            // 
            this.pnlColor.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(68)))), ((int)(((byte)(68)))), ((int)(((byte)(68)))));
            this.pnlColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlColor.Location = new System.Drawing.Point(20, 31);
            this.pnlColor.Name = "pnlColor";
            this.pnlColor.Size = new System.Drawing.Size(20, 20);
            this.pnlColor.TabIndex = 0;
            // 
            // Configure
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(426, 233);
            this.Controls.Add(this.gbBackColor);
            this.Controls.Add(this.btnSetting);
            this.Controls.Add(this.gbFiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Configure";
            this.Opacity = 0.98D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Feep 配置辅助程序";
            this.gbFiles.ResumeLayout(false);
            this.gbFiles.PerformLayout();
            this.gbBackColor.ResumeLayout(false);
            this.gbBackColor.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbFiles;
        private System.Windows.Forms.CheckBox cbTIF;
        private System.Windows.Forms.CheckBox cbGIF;
        private System.Windows.Forms.CheckBox cbPNG;
        private System.Windows.Forms.CheckBox cbBMP;
        private System.Windows.Forms.CheckBox cbJPG;
        private System.Windows.Forms.Button btnSetting;
        private System.Windows.Forms.GroupBox gbBackColor;
        private System.Windows.Forms.TextBox txtColor;
        private System.Windows.Forms.Label lblColor;
        private System.Windows.Forms.Panel pnlColor;
    }
}

