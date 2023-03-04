namespace AutoModPlugins.GUI
{
    partial class ALMError
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ALMError));
            this.BTN4 = new System.Windows.Forms.Button();
            this.BTN3 = new System.Windows.Forms.Button();
            this.BTN2 = new System.Windows.Forms.Button();
            this.BTN1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BTN4
            // 
            this.BTN4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BTN4.Enabled = false;
            this.BTN4.Location = new System.Drawing.Point(12, 138);
            this.BTN4.Name = "BTN4";
            this.BTN4.Size = new System.Drawing.Size(75, 23);
            this.BTN4.TabIndex = 0;
            this.BTN4.Text = "DR_Yes";
            this.BTN4.UseVisualStyleBackColor = true;
            this.BTN4.Visible = false;
            this.BTN4.Click += new System.EventHandler(this.BTN4_Click);
            // 
            // BTN3
            // 
            this.BTN3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BTN3.Enabled = false;
            this.BTN3.Location = new System.Drawing.Point(93, 138);
            this.BTN3.Name = "BTN3";
            this.BTN3.Size = new System.Drawing.Size(75, 23);
            this.BTN3.TabIndex = 1;
            this.BTN3.Text = "DR_No";
            this.BTN3.UseVisualStyleBackColor = true;
            this.BTN3.Visible = false;
            this.BTN3.Click += new System.EventHandler(this.BTN3_Click);
            // 
            // BTN2
            // 
            this.BTN2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BTN2.Enabled = false;
            this.BTN2.Location = new System.Drawing.Point(174, 138);
            this.BTN2.Name = "BTN2";
            this.BTN2.Size = new System.Drawing.Size(75, 23);
            this.BTN2.TabIndex = 2;
            this.BTN2.Text = "DR_Retry";
            this.BTN2.UseVisualStyleBackColor = true;
            this.BTN2.Visible = false;
            this.BTN2.Click += new System.EventHandler(this.BTN2_Click);
            // 
            // BTN1
            // 
            this.BTN1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BTN1.Enabled = false;
            this.BTN1.Location = new System.Drawing.Point(255, 138);
            this.BTN1.Name = "BTN1";
            this.BTN1.Size = new System.Drawing.Size(75, 23);
            this.BTN1.TabIndex = 3;
            this.BTN1.Text = "DR_OK";
            this.BTN1.UseVisualStyleBackColor = true;
            this.BTN1.Visible = false;
            this.BTN1.Click += new System.EventHandler(this.BTN1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.MaximumSize = new System.Drawing.Size(320, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Error";
            // 
            // ALMError
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(341, 173);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BTN1);
            this.Controls.Add(this.BTN2);
            this.Controls.Add(this.BTN3);
            this.Controls.Add(this.BTN4);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MdiChildrenMinimizedAnchorBottom = false;
            this.MinimizeBox = false;
            this.Name = "ALMError";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ALMError";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button BTN4;
        private System.Windows.Forms.Button BTN3;
        private System.Windows.Forms.Button BTN2;
        private System.Windows.Forms.Button BTN1;
        private System.Windows.Forms.Label label1;
    }
}