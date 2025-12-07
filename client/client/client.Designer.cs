namespace client
{
    partial class client
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
            this.butApp = new System.Windows.Forms.Button();
            this.butConnect = new System.Windows.Forms.Button();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.butTat = new System.Windows.Forms.Button();
            this.butReg = new System.Windows.Forms.Button();
            this.butExit = new System.Windows.Forms.Button();
            this.butPic = new System.Windows.Forms.Button();
            this.butKeyLock = new System.Windows.Forms.Button();
            this.butProcess = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // butApp
            // 
            this.butApp.Location = new System.Drawing.Point(124, 79);
            this.butApp.Margin = new System.Windows.Forms.Padding(4);
            this.butApp.Name = "butApp";
            this.butApp.Size = new System.Drawing.Size(193, 78);
            this.butApp.TabIndex = 0;
            this.butApp.Text = "App Running";
            this.butApp.UseVisualStyleBackColor = true;
            // 
            // butConnect
            // 
            this.butConnect.Location = new System.Drawing.Point(325, 33);
            this.butConnect.Margin = new System.Windows.Forms.Padding(4);
            this.butConnect.Name = "butConnect";
            this.butConnect.Size = new System.Drawing.Size(133, 28);
            this.butConnect.TabIndex = 1;
            this.butConnect.Text = "Kết nối";
            this.butConnect.UseVisualStyleBackColor = true;
            // 
            // txtIP
            // 
            this.txtIP.Location = new System.Drawing.Point(16, 36);
            this.txtIP.Margin = new System.Windows.Forms.Padding(4);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(300, 22);
            this.txtIP.TabIndex = 2;
            this.txtIP.Text = "Nhập IP";
            // 
            // butTat
            // 
            this.butTat.Location = new System.Drawing.Point(124, 164);
            this.butTat.Margin = new System.Windows.Forms.Padding(4);
            this.butTat.Name = "butTat";
            this.butTat.Size = new System.Drawing.Size(64, 70);
            this.butTat.TabIndex = 4;
            this.butTat.Text = "Tắt máy";
            this.butTat.UseVisualStyleBackColor = true;
            // 
            // butReg
            // 
            this.butReg.Location = new System.Drawing.Point(124, 241);
            this.butReg.Margin = new System.Windows.Forms.Padding(4);
            this.butReg.Name = "butReg";
            this.butReg.Size = new System.Drawing.Size(264, 80);
            this.butReg.TabIndex = 5;
            this.butReg.Text = "Sửa registry";
            this.butReg.UseVisualStyleBackColor = true;
            // 
            // butExit
            // 
            this.butExit.Location = new System.Drawing.Point(396, 241);
            this.butExit.Margin = new System.Windows.Forms.Padding(4);
            this.butExit.Name = "butExit";
            this.butExit.Size = new System.Drawing.Size(63, 80);
            this.butExit.TabIndex = 6;
            this.butExit.Text = "Thoát";
            this.butExit.UseVisualStyleBackColor = true;
            // 
            // butPic
            // 
            this.butPic.Location = new System.Drawing.Point(196, 164);
            this.butPic.Margin = new System.Windows.Forms.Padding(4);
            this.butPic.Name = "butPic";
            this.butPic.Size = new System.Drawing.Size(121, 70);
            this.butPic.TabIndex = 7;
            this.butPic.Text = "Chụp màn hình";
            this.butPic.UseVisualStyleBackColor = true;
            // 
            // butKeyLock
            // 
            this.butKeyLock.Location = new System.Drawing.Point(325, 79);
            this.butKeyLock.Margin = new System.Windows.Forms.Padding(4);
            this.butKeyLock.Name = "butKeyLock";
            this.butKeyLock.Size = new System.Drawing.Size(133, 155);
            this.butKeyLock.TabIndex = 8;
            this.butKeyLock.Text = "Keystroke";
            this.butKeyLock.UseVisualStyleBackColor = true;
            // 
            // butProcess
            // 
            this.butProcess.Location = new System.Drawing.Point(16, 79);
            this.butProcess.Margin = new System.Windows.Forms.Padding(4);
            this.butProcess.Name = "butProcess";
            this.butProcess.Size = new System.Drawing.Size(100, 242);
            this.butProcess.TabIndex = 9;
            this.butProcess.Text = "Process Running";
            this.butProcess.UseVisualStyleBackColor = true;
            // 
            // client
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 372);
            this.Controls.Add(this.butProcess);
            this.Controls.Add(this.butKeyLock);
            this.Controls.Add(this.butPic);
            this.Controls.Add(this.butExit);
            this.Controls.Add(this.butReg);
            this.Controls.Add(this.butTat);
            this.Controls.Add(this.txtIP);
            this.Controls.Add(this.butConnect);
            this.Controls.Add(this.butApp);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "client";
            this.Text = "Client";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button butApp;
        private System.Windows.Forms.Button butConnect;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Button butTat;
        private System.Windows.Forms.Button butReg;
        private System.Windows.Forms.Button butExit;
        private System.Windows.Forms.Button butPic;
        private System.Windows.Forms.Button butKeyLock;
        private System.Windows.Forms.Button butProcess;
    }
}

