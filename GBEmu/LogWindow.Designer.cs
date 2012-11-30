namespace GBEmu
{
    partial class LogWindow
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
            this.logBox = new System.Windows.Forms.ListBox();
            this.clearMsgButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logBox.FormattingEnabled = true;
            this.logBox.Location = new System.Drawing.Point(13, 13);
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(259, 199);
            this.logBox.TabIndex = 0;
            // 
            // clearMsgButton
            // 
            this.clearMsgButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.clearMsgButton.Location = new System.Drawing.Point(12, 218);
            this.clearMsgButton.Name = "clearMsgButton";
            this.clearMsgButton.Size = new System.Drawing.Size(39, 23);
            this.clearMsgButton.TabIndex = 1;
            this.clearMsgButton.Text = "Clear";
            this.clearMsgButton.UseVisualStyleBackColor = true;
            this.clearMsgButton.Click += new System.EventHandler(this.clearMsgButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.closeButton.Location = new System.Drawing.Point(232, 218);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(41, 23);
            this.closeButton.TabIndex = 2;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // LogWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 253);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.clearMsgButton);
            this.Controls.Add(this.logBox);
            this.Name = "LogWindow";
            this.Text = "Log Window";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox logBox;
        private System.Windows.Forms.Button clearMsgButton;
        private System.Windows.Forms.Button closeButton;
    }
}