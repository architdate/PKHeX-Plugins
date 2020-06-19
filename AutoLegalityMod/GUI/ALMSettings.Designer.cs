using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    partial class ALMSettings
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
            this.PG_Settings = new System.Windows.Forms.PropertyGrid();
            this.RunBulkTests = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PG_Settings
            // 
            this.PG_Settings.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PG_Settings.Location = new System.Drawing.Point(0, 29);
            this.PG_Settings.Name = "PG_Settings";
            this.PG_Settings.Size = new System.Drawing.Size(334, 299);
            this.PG_Settings.TabIndex = 1;
            // 
            // RunBulkTests
            // 
            this.RunBulkTests.Dock = System.Windows.Forms.DockStyle.Top;
            this.RunBulkTests.Location = new System.Drawing.Point(0, 0);
            this.RunBulkTests.Name = "RunBulkTests";
            this.RunBulkTests.Size = new System.Drawing.Size(334, 23);
            this.RunBulkTests.TabIndex = 2;
            this.RunBulkTests.Text = "Run Tests";
            this.RunBulkTests.UseVisualStyleBackColor = true;
            this.RunBulkTests.Click += new System.EventHandler(this.RunBulkTests_Click);
            // 
            // ALMSettings
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(334, 328);
            this.Controls.Add(this.RunBulkTests);
            this.Controls.Add(this.PG_Settings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = global::AutoModPlugins.Properties.Resources.icon;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ALMSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Auto Legality Plugin Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ALMSettings_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SettingsEditor_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid PG_Settings;
        private Button RunBulkTests;
    }
}