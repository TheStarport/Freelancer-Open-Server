namespace FLServer
{
    partial class ControlWindow
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.checkBoxLogFile = new System.Windows.Forms.CheckBox();
            this.checkBoxLogFLMsg = new System.Windows.Forms.CheckBox();
            this.checkBoxLogPositionUpdates = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.checkBoxLogDPlay = new System.Windows.Forms.CheckBox();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.button2 = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(555, 603);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.checkBoxLogFile);
            this.tabPage3.Controls.Add(this.checkBoxLogFLMsg);
            this.tabPage3.Controls.Add(this.checkBoxLogPositionUpdates);
            this.tabPage3.Controls.Add(this.button1);
            this.tabPage3.Controls.Add(this.checkBoxLogDPlay);
            this.tabPage3.Controls.Add(this.listBoxLog);
            this.tabPage3.Controls.Add(this.button2);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(547, 577);
            this.tabPage3.TabIndex = 0;
            this.tabPage3.Text = "Setup";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // checkBoxLogFile
            // 
            this.checkBoxLogFile.AutoSize = true;
            this.checkBoxLogFile.Location = new System.Drawing.Point(190, 58);
            this.checkBoxLogFile.Name = "checkBoxLogFile";
            this.checkBoxLogFile.Size = new System.Drawing.Size(72, 17);
            this.checkBoxLogFile.TabIndex = 19;
            this.checkBoxLogFile.Text = "Log to file";
            this.checkBoxLogFile.UseVisualStyleBackColor = true;
            // 
            // checkBoxLogFLMsg
            // 
            this.checkBoxLogFLMsg.AutoSize = true;
            this.checkBoxLogFLMsg.Location = new System.Drawing.Point(190, 35);
            this.checkBoxLogFLMsg.Name = "checkBoxLogFLMsg";
            this.checkBoxLogFLMsg.Size = new System.Drawing.Size(102, 17);
            this.checkBoxLogFLMsg.TabIndex = 18;
            this.checkBoxLogFLMsg.Text = "Log fl messages";
            this.checkBoxLogFLMsg.UseVisualStyleBackColor = true;
            this.checkBoxLogFLMsg.CheckedChanged += new System.EventHandler(this.checkBoxLogFLMsg_CheckedChanged);
            // 
            // checkBoxLogPositionUpdates
            // 
            this.checkBoxLogPositionUpdates.AutoSize = true;
            this.checkBoxLogPositionUpdates.Location = new System.Drawing.Point(6, 58);
            this.checkBoxLogPositionUpdates.Name = "checkBoxLogPositionUpdates";
            this.checkBoxLogPositionUpdates.Size = new System.Drawing.Size(174, 17);
            this.checkBoxLogPositionUpdates.TabIndex = 17;
            this.checkBoxLogPositionUpdates.Text = "Log position updates messages";
            this.checkBoxLogPositionUpdates.UseVisualStyleBackColor = true;
            this.checkBoxLogPositionUpdates.CheckedChanged += new System.EventHandler(this.checkBoxLogPositionUpdates_CheckedChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(6, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Start Server";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.buttonStartServer_Click);
            // 
            // checkBoxLogDPlay
            // 
            this.checkBoxLogDPlay.AutoSize = true;
            this.checkBoxLogDPlay.Location = new System.Drawing.Point(6, 35);
            this.checkBoxLogDPlay.Name = "checkBoxLogDPlay";
            this.checkBoxLogDPlay.Size = new System.Drawing.Size(122, 17);
            this.checkBoxLogDPlay.TabIndex = 16;
            this.checkBoxLogDPlay.Text = "Log dplay messages";
            this.checkBoxLogDPlay.UseVisualStyleBackColor = true;
            this.checkBoxLogDPlay.CheckedChanged += new System.EventHandler(this.checkBoxLogDPlay_CheckedChanged);
            // 
            // listBoxLog
            // 
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.Location = new System.Drawing.Point(6, 81);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxLog.Size = new System.Drawing.Size(535, 490);
            this.listBoxLog.TabIndex = 20;
            this.listBoxLog.SelectedIndexChanged += new System.EventHandler(this.listBoxLog_SelectedIndexChanged);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(87, 6);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "Stop Server";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.buttonStopServer_Click);
            // 
            // ControlWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 627);
            this.Controls.Add(this.tabControl1);
            this.Name = "ControlWindow";
            this.ShowIcon = false;
            this.Text = "FLOpenServer - A Freelancer Server Clone";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ControlWindow_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBoxLogDPlay;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBoxLogPositionUpdates;
        private System.Windows.Forms.CheckBox checkBoxLogFLMsg;
        private System.Windows.Forms.CheckBox checkBoxLogFile;

    }
}

