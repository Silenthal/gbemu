namespace GBEmu
{
    partial class MonitorForm
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
            this.mainBar = new System.Windows.Forms.ProgressBar();
            this.blitBar = new System.Windows.Forms.ProgressBar();
            this.mainBarLabel = new System.Windows.Forms.Label();
            this.blitBarLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // mainBar
            // 
            this.mainBar.Location = new System.Drawing.Point(47, 12);
            this.mainBar.Name = "mainBar";
            this.mainBar.Size = new System.Drawing.Size(100, 23);
            this.mainBar.TabIndex = 0;
            // 
            // blitBar
            // 
            this.blitBar.Location = new System.Drawing.Point(47, 41);
            this.blitBar.Name = "blitBar";
            this.blitBar.Size = new System.Drawing.Size(100, 23);
            this.blitBar.TabIndex = 1;
            // 
            // mainBarLabel
            // 
            this.mainBarLabel.AutoSize = true;
            this.mainBarLabel.Location = new System.Drawing.Point(159, 22);
            this.mainBarLabel.Name = "mainBarLabel";
            this.mainBarLabel.Size = new System.Drawing.Size(30, 13);
            this.mainBarLabel.TabIndex = 2;
            this.mainBarLabel.Text = "Main";
            // 
            // blitBarLabel
            // 
            this.blitBarLabel.AutoSize = true;
            this.blitBarLabel.Location = new System.Drawing.Point(159, 51);
            this.blitBarLabel.Name = "blitBarLabel";
            this.blitBarLabel.Size = new System.Drawing.Size(21, 13);
            this.blitBarLabel.TabIndex = 3;
            this.blitBarLabel.Text = "Blit";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "CPU";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 51);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(21, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Blit";
            // 
            // MonitorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(199, 79);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.blitBarLabel);
            this.Controls.Add(this.mainBarLabel);
            this.Controls.Add(this.blitBar);
            this.Controls.Add(this.mainBar);
            this.Name = "MonitorForm";
            this.Text = "Monitor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar mainBar;
        private System.Windows.Forms.ProgressBar blitBar;
        private System.Windows.Forms.Label mainBarLabel;
        private System.Windows.Forms.Label blitBarLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}