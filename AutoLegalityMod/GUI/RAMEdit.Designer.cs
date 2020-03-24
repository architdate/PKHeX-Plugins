using System;
using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    partial class RAMEdit
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
            this.RAM = new System.Windows.Forms.TextBox();
            this.Update = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // RAM
            // 
            this.RAM.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RAM.Location = new System.Drawing.Point(9, 10);
            this.RAM.Multiline = true;
            this.RAM.Name = "RAM";
            this.RAM.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.RAM.Size = new System.Drawing.Size(364, 292);
            this.RAM.TabIndex = 0;
            // 
            // Update
            // 
            this.Update.Location = new System.Drawing.Point(9, 308);
            this.Update.Name = "Update";
            this.Update.Size = new System.Drawing.Size(363, 26);
            this.Update.TabIndex = 1;
            this.Update.Text = "Update";
            this.Update.UseVisualStyleBackColor = true;
            this.Update.Click += new System.EventHandler(this.Update_Click);
            // 
            // RAMEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 343);
            this.Controls.Add(this.Update);
            this.Controls.Add(this.RAM);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RAMEdit";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RAMEdit";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CloseForm);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.TextBox RAM;
        private Button Update;
    }
}