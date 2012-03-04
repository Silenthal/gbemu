namespace GBEmu
{
    partial class MainForm
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.button1 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.pcLabel = new System.Windows.Forms.Label();
			this.spLabel = new System.Windows.Forms.Label();
			this.hlLabel = new System.Windows.Forms.Label();
			this.deLabel = new System.Windows.Forms.Label();
			this.bcLabel = new System.Windows.Forms.Label();
			this.afLabel = new System.Windows.Forms.Label();
			this.gdiWindow1 = new GBEmu.Render.Gdi.GdiWindow();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(440, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.openToolStripMenuItem.Text = "&Open GB/C File";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.Filter = "GB Files|*.gb|GBC Files|*.gbc|All Files|*";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 177);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 24;
			this.button1.Text = "Bacon";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(178, 27);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(65, 13);
			this.label1.TabIndex = 27;
			this.label1.Text = "Diagnostics:";
			// 
			// richTextBox1
			// 
			this.richTextBox1.Location = new System.Drawing.Point(181, 43);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.Size = new System.Drawing.Size(188, 140);
			this.richTextBox1.TabIndex = 29;
			this.richTextBox1.Text = "";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(375, 43);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(20, 13);
			this.label2.TabIndex = 30;
			this.label2.Text = "AF";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(375, 56);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(21, 13);
			this.label3.TabIndex = 31;
			this.label3.Text = "BC";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(375, 82);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(21, 13);
			this.label4.TabIndex = 33;
			this.label4.Text = "HL";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(375, 69);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(22, 13);
			this.label5.TabIndex = 32;
			this.label5.Text = "DE";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(375, 108);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(21, 13);
			this.label6.TabIndex = 35;
			this.label6.Text = "PC";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(375, 95);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(21, 13);
			this.label7.TabIndex = 34;
			this.label7.Text = "SP";
			// 
			// pcLabel
			// 
			this.pcLabel.AutoSize = true;
			this.pcLabel.Location = new System.Drawing.Point(401, 108);
			this.pcLabel.Name = "pcLabel";
			this.pcLabel.Size = new System.Drawing.Size(21, 13);
			this.pcLabel.TabIndex = 41;
			this.pcLabel.Text = "PC";
			// 
			// spLabel
			// 
			this.spLabel.AutoSize = true;
			this.spLabel.Location = new System.Drawing.Point(401, 95);
			this.spLabel.Name = "spLabel";
			this.spLabel.Size = new System.Drawing.Size(21, 13);
			this.spLabel.TabIndex = 40;
			this.spLabel.Text = "SP";
			// 
			// hlLabel
			// 
			this.hlLabel.AutoSize = true;
			this.hlLabel.Location = new System.Drawing.Point(401, 82);
			this.hlLabel.Name = "hlLabel";
			this.hlLabel.Size = new System.Drawing.Size(21, 13);
			this.hlLabel.TabIndex = 39;
			this.hlLabel.Text = "HL";
			// 
			// deLabel
			// 
			this.deLabel.AutoSize = true;
			this.deLabel.Location = new System.Drawing.Point(401, 69);
			this.deLabel.Name = "deLabel";
			this.deLabel.Size = new System.Drawing.Size(22, 13);
			this.deLabel.TabIndex = 38;
			this.deLabel.Text = "DE";
			// 
			// bcLabel
			// 
			this.bcLabel.AutoSize = true;
			this.bcLabel.Location = new System.Drawing.Point(401, 56);
			this.bcLabel.Name = "bcLabel";
			this.bcLabel.Size = new System.Drawing.Size(21, 13);
			this.bcLabel.TabIndex = 37;
			this.bcLabel.Text = "BC";
			// 
			// afLabel
			// 
			this.afLabel.AutoSize = true;
			this.afLabel.Location = new System.Drawing.Point(401, 43);
			this.afLabel.Name = "afLabel";
			this.afLabel.Size = new System.Drawing.Size(20, 13);
			this.afLabel.TabIndex = 36;
			this.afLabel.Text = "AF";
			// 
			// gdiWindow1
			// 
			this.gdiWindow1.Location = new System.Drawing.Point(12, 27);
			this.gdiWindow1.Name = "gdiWindow1";
			this.gdiWindow1.Size = new System.Drawing.Size(160, 144);
			this.gdiWindow1.TabIndex = 26;
			this.gdiWindow1.Text = "gdiWindow1";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(440, 215);
			this.Controls.Add(this.pcLabel);
			this.Controls.Add(this.spLabel);
			this.Controls.Add(this.hlLabel);
			this.Controls.Add(this.deLabel);
			this.Controls.Add(this.bcLabel);
			this.Controls.Add(this.afLabel);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.richTextBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.gdiWindow1);
			this.Controls.Add(this.button1);
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "GBRead";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyUp);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button button1;
        private GBEmu.Render.Gdi.GdiWindow gdiWindow1;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label pcLabel;
        private System.Windows.Forms.Label spLabel;
        private System.Windows.Forms.Label hlLabel;
        private System.Windows.Forms.Label deLabel;
        private System.Windows.Forms.Label bcLabel;
        private System.Windows.Forms.Label afLabel;
    }
}

