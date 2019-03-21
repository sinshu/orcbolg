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
            this.btnRecordingStartStop = new System.Windows.Forms.Button();
            this.picWaveformMonitor = new System.Windows.Forms.PictureBox();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.txtDebugInfo = new System.Windows.Forms.TextBox();
            this.picLoadInfo = new System.Windows.Forms.PictureBox();
            this.lblRecordingNumber = new System.Windows.Forms.Label();
            this.btnRecordingNumberIncrement = new System.Windows.Forms.Button();
            this.btnRecordingNumberDecrement = new System.Windows.Forms.Button();
            this.pnlBottomLeft = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.picWaveformMonitor)).BeginInit();
            this.pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLoadInfo)).BeginInit();
            this.SuspendLayout();
            // 
            // btnRecordingStartStop
            // 
            this.btnRecordingStartStop.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnRecordingStartStop.Location = new System.Drawing.Point(8, 8);
            this.btnRecordingStartStop.Name = "btnRecordingStartStop";
            this.btnRecordingStartStop.Size = new System.Drawing.Size(64, 64);
            this.btnRecordingStartStop.TabIndex = 0;
            this.btnRecordingStartStop.Text = "●録音";
            this.btnRecordingStartStop.UseVisualStyleBackColor = false;
            this.btnRecordingStartStop.Click += new System.EventHandler(this.btnRecordingStartStop_Click);
            // 
            // picWaveformMonitor
            // 
            this.picWaveformMonitor.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.picWaveformMonitor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picWaveformMonitor.Location = new System.Drawing.Point(0, 0);
            this.picWaveformMonitor.Name = "picWaveformMonitor";
            this.picWaveformMonitor.Size = new System.Drawing.Size(800, 520);
            this.picWaveformMonitor.TabIndex = 1;
            this.picWaveformMonitor.TabStop = false;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.txtDebugInfo);
            this.pnlBottom.Controls.Add(this.picLoadInfo);
            this.pnlBottom.Controls.Add(this.lblRecordingNumber);
            this.pnlBottom.Controls.Add(this.btnRecordingNumberIncrement);
            this.pnlBottom.Controls.Add(this.btnRecordingNumberDecrement);
            this.pnlBottom.Controls.Add(this.btnRecordingStartStop);
            this.pnlBottom.Controls.Add(this.pnlBottomLeft);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 520);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(8);
            this.pnlBottom.Size = new System.Drawing.Size(800, 80);
            this.pnlBottom.TabIndex = 2;
            // 
            // txtDebugInfo
            // 
            this.txtDebugInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDebugInfo.Location = new System.Drawing.Point(320, 8);
            this.txtDebugInfo.Multiline = true;
            this.txtDebugInfo.Name = "txtDebugInfo";
            this.txtDebugInfo.ReadOnly = true;
            this.txtDebugInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDebugInfo.Size = new System.Drawing.Size(472, 64);
            this.txtDebugInfo.TabIndex = 6;
            // 
            // picLoadInfo
            // 
            this.picLoadInfo.Location = new System.Drawing.Point(168, 8);
            this.picLoadInfo.Name = "picLoadInfo";
            this.picLoadInfo.Size = new System.Drawing.Size(144, 64);
            this.picLoadInfo.TabIndex = 4;
            this.picLoadInfo.TabStop = false;
            // 
            // lblRecordingNumber
            // 
            this.lblRecordingNumber.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblRecordingNumber.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblRecordingNumber.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.lblRecordingNumber.Location = new System.Drawing.Point(88, 44);
            this.lblRecordingNumber.Name = "lblRecordingNumber";
            this.lblRecordingNumber.Padding = new System.Windows.Forms.Padding(4, 0, 0, 2);
            this.lblRecordingNumber.Size = new System.Drawing.Size(64, 28);
            this.lblRecordingNumber.TabIndex = 3;
            this.lblRecordingNumber.Text = "1";
            this.lblRecordingNumber.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnRecordingNumberIncrement
            // 
            this.btnRecordingNumberIncrement.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnRecordingNumberIncrement.Location = new System.Drawing.Point(124, 8);
            this.btnRecordingNumberIncrement.Name = "btnRecordingNumberIncrement";
            this.btnRecordingNumberIncrement.Size = new System.Drawing.Size(28, 28);
            this.btnRecordingNumberIncrement.TabIndex = 2;
            this.btnRecordingNumberIncrement.Text = ">";
            this.btnRecordingNumberIncrement.UseVisualStyleBackColor = true;
            this.btnRecordingNumberIncrement.Click += new System.EventHandler(this.btnRecordingNumberIncrement_Click);
            // 
            // btnRecordingNumberDecrement
            // 
            this.btnRecordingNumberDecrement.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.btnRecordingNumberDecrement.Location = new System.Drawing.Point(88, 8);
            this.btnRecordingNumberDecrement.Name = "btnRecordingNumberDecrement";
            this.btnRecordingNumberDecrement.Size = new System.Drawing.Size(28, 28);
            this.btnRecordingNumberDecrement.TabIndex = 1;
            this.btnRecordingNumberDecrement.Text = "<";
            this.btnRecordingNumberDecrement.UseVisualStyleBackColor = true;
            this.btnRecordingNumberDecrement.Click += new System.EventHandler(this.btnRecordingNumberDecrement_Click);
            // 
            // pnlBottomLeft
            // 
            this.pnlBottomLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlBottomLeft.Location = new System.Drawing.Point(8, 8);
            this.pnlBottomLeft.Name = "pnlBottomLeft";
            this.pnlBottomLeft.Size = new System.Drawing.Size(312, 64);
            this.pnlBottomLeft.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.picWaveformMonitor);
            this.Controls.Add(this.pnlBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Rockon - 録音ツール";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.picWaveformMonitor)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLoadInfo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnRecordingStartStop;
        private System.Windows.Forms.PictureBox picWaveformMonitor;
        private System.Windows.Forms.Button btnRecordingNumberIncrement;
        private System.Windows.Forms.Button btnRecordingNumberDecrement;
        private System.Windows.Forms.Label lblRecordingNumber;
        private System.Windows.Forms.TextBox txtDebugInfo;
        private System.Windows.Forms.Panel pnlBottomLeft;
        private System.Windows.Forms.PictureBox picLoadInfo;
        private System.Windows.Forms.Panel pnlBottom;
    }
}

