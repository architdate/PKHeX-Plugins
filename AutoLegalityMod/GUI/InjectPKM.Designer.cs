namespace AutoModPlugins.GUI
{
    partial class InjectPKM
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
            this.boxnum = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.slotnum = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // boxnum
            // 
            this.boxnum.Location = new System.Drawing.Point(13, 13);
            this.boxnum.Name = "boxnum";
            this.boxnum.Size = new System.Drawing.Size(110, 20);
            this.boxnum.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(13, 39);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(235, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Inject Pokemon";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // slotnum
            // 
            this.slotnum.Location = new System.Drawing.Point(138, 13);
            this.slotnum.Name = "slotnum";
            this.slotnum.Size = new System.Drawing.Size(110, 20);
            this.slotnum.TabIndex = 1;
            // 
            // InjectPKM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(260, 74);
            this.Controls.Add(this.slotnum);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.boxnum);
            this.Name = "InjectPKM";
            this.Text = "Inject Pokemon";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox boxnum;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox slotnum;
    }
}