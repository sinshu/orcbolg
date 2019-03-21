namespace Rockon
{
    partial class ErrorBox
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.s_lblMessage = new System.Windows.Forms.Label();
            this.lblMessage = new System.Windows.Forms.Label();
            this.s_lblModule = new System.Windows.Forms.Label();
            this.lblModule = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblTitle.Location = new System.Drawing.Point(16, 16);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(48, 12);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "lblTitle";
            // 
            // s_lblMessage
            // 
            this.s_lblMessage.AutoSize = true;
            this.s_lblMessage.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.s_lblMessage.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.s_lblMessage.Location = new System.Drawing.Point(16, 48);
            this.s_lblMessage.Name = "s_lblMessage";
            this.s_lblMessage.Size = new System.Drawing.Size(55, 12);
            this.s_lblMessage.TabIndex = 1;
            this.s_lblMessage.Text = "メッセージ";
            // 
            // lblMessage
            // 
            this.lblMessage.Location = new System.Drawing.Point(48, 64);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(536, 32);
            this.lblMessage.TabIndex = 2;
            this.lblMessage.Text = "lblMessage";
            // 
            // s_lblModule
            // 
            this.s_lblModule.AutoSize = true;
            this.s_lblModule.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.s_lblModule.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.s_lblModule.Location = new System.Drawing.Point(16, 96);
            this.s_lblModule.Name = "s_lblModule";
            this.s_lblModule.Size = new System.Drawing.Size(83, 12);
            this.s_lblModule.TabIndex = 3;
            this.s_lblModule.Text = "原因モジュール";
            // 
            // lblModule
            // 
            this.lblModule.Location = new System.Drawing.Point(48, 112);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(536, 32);
            this.lblModule.TabIndex = 4;
            this.lblModule.Text = "lblModule";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label6.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label6.Location = new System.Drawing.Point(16, 144);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 12);
            this.label6.TabIndex = 5;
            this.label6.Text = "詳細";
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBox1.Location = new System.Drawing.Point(16, 160);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(568, 176);
            this.textBox1.TabIndex = 6;
            // 
            // btnCopy
            // 
            this.btnCopy.Location = new System.Drawing.Point(440, 352);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(64, 32);
            this.btnCopy.TabIndex = 7;
            this.btnCopy.Text = "コピー";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(520, 352);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(64, 32);
            this.btnExit.TabIndex = 8;
            this.btnExit.Text = "終了";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // ErrorBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblModule);
            this.Controls.Add(this.s_lblModule);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.s_lblMessage);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "例外";
            this.Shown += new System.EventHandler(this.ErrorBox_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label s_lblMessage;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Label s_lblModule;
        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnExit;
    }
}