namespace Rockon
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnRecord = new System.Windows.Forms.Button();
            this.picMonitor = new System.Windows.Forms.PictureBox();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.txtDebug = new System.Windows.Forms.TextBox();
            this.picInfo = new System.Windows.Forms.PictureBox();
            this.lblNumber = new System.Windows.Forms.Label();
            this.btnNumberIncrement = new System.Windows.Forms.Button();
            this.btnNumberDecrement = new System.Windows.Forms.Button();
            this.pnlControl = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.picMonitor)).BeginInit();
            this.pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picInfo)).BeginInit();
            this.SuspendLayout();
            // 
            // btnRecord
            // 
            this.btnRecord.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnRecord.ForeColor = System.Drawing.Color.Crimson;
            this.btnRecord.Location = new System.Drawing.Point(8, 8);
            this.btnRecord.Name = "btnRecord";
            this.btnRecord.Size = new System.Drawing.Size(64, 64);
            this.btnRecord.TabIndex = 0;
            this.btnRecord.Text = "●録音";
            this.btnRecord.UseVisualStyleBackColor = true;
            this.btnRecord.Click += new System.EventHandler(this.button1_Click);
            // 
            // picMonitor
            // 
            this.picMonitor.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.picMonitor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picMonitor.Location = new System.Drawing.Point(0, 0);
            this.picMonitor.Name = "picMonitor";
            this.picMonitor.Size = new System.Drawing.Size(922, 518);
            this.picMonitor.TabIndex = 1;
            this.picMonitor.TabStop = false;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.txtDebug);
            this.pnlBottom.Controls.Add(this.picInfo);
            this.pnlBottom.Controls.Add(this.lblNumber);
            this.pnlBottom.Controls.Add(this.btnNumberIncrement);
            this.pnlBottom.Controls.Add(this.btnNumberDecrement);
            this.pnlBottom.Controls.Add(this.btnRecord);
            this.pnlBottom.Controls.Add(this.pnlControl);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 518);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(8);
            this.pnlBottom.Size = new System.Drawing.Size(922, 80);
            this.pnlBottom.TabIndex = 2;
            // 
            // txtDebug
            // 
            this.txtDebug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDebug.Enabled = false;
            this.txtDebug.Location = new System.Drawing.Point(320, 8);
            this.txtDebug.Multiline = true;
            this.txtDebug.Name = "txtDebug";
            this.txtDebug.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDebug.Size = new System.Drawing.Size(594, 64);
            this.txtDebug.TabIndex = 6;
            // 
            // picInfo
            // 
            this.picInfo.Location = new System.Drawing.Point(168, 8);
            this.picInfo.Name = "picInfo";
            this.picInfo.Size = new System.Drawing.Size(144, 64);
            this.picInfo.TabIndex = 4;
            this.picInfo.TabStop = false;
            // 
            // lblNumber
            // 
            this.lblNumber.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblNumber.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblNumber.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.lblNumber.Location = new System.Drawing.Point(88, 44);
            this.lblNumber.Name = "lblNumber";
            this.lblNumber.Size = new System.Drawing.Size(64, 28);
            this.lblNumber.TabIndex = 3;
            this.lblNumber.Text = "label1";
            this.lblNumber.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnNumberIncrement
            // 
            this.btnNumberIncrement.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnNumberIncrement.Location = new System.Drawing.Point(124, 8);
            this.btnNumberIncrement.Name = "btnNumberIncrement";
            this.btnNumberIncrement.Size = new System.Drawing.Size(28, 28);
            this.btnNumberIncrement.TabIndex = 2;
            this.btnNumberIncrement.Text = ">";
            this.btnNumberIncrement.UseVisualStyleBackColor = true;
            this.btnNumberIncrement.Click += new System.EventHandler(this.btnNumberIncrement_Click);
            // 
            // btnNumberDecrement
            // 
            this.btnNumberDecrement.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnNumberDecrement.Location = new System.Drawing.Point(88, 8);
            this.btnNumberDecrement.Name = "btnNumberDecrement";
            this.btnNumberDecrement.Size = new System.Drawing.Size(28, 28);
            this.btnNumberDecrement.TabIndex = 1;
            this.btnNumberDecrement.Text = "<";
            this.btnNumberDecrement.UseVisualStyleBackColor = true;
            this.btnNumberDecrement.Click += new System.EventHandler(this.btnNumberDecrement_Click);
            // 
            // pnlControl
            // 
            this.pnlControl.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlControl.Location = new System.Drawing.Point(8, 8);
            this.pnlControl.Name = "pnlControl";
            this.pnlControl.Size = new System.Drawing.Size(312, 64);
            this.pnlControl.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(922, 598);
            this.Controls.Add(this.picMonitor);
            this.Controls.Add(this.pnlBottom);
            this.Name = "MainForm";
            this.Text = "Rockon - 録音くん2";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.picMonitor)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picInfo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnRecord;
        private System.Windows.Forms.PictureBox picMonitor;
        private System.Windows.Forms.Button btnNumberIncrement;
        private System.Windows.Forms.Button btnNumberDecrement;
        private System.Windows.Forms.Label lblNumber;
        private System.Windows.Forms.TextBox txtDebug;
        private System.Windows.Forms.Panel pnlControl;
        private System.Windows.Forms.PictureBox picInfo;
        private System.Windows.Forms.Panel pnlBottom;
    }
}

