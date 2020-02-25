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
            this.SuspendLayout();
            // 
            // PG_Settings
            // 
            this.PG_Settings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PG_Settings.Location = new System.Drawing.Point(0, 0);
            this.PG_Settings.Name = "PG_Settings";
            this.PG_Settings.Size = new System.Drawing.Size(334, 311);
            this.PG_Settings.TabIndex = 1;
            // 
            // ALMSettings
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(334, 311);
            this.Controls.Add(this.PG_Settings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = global::AutoModPlugins.Properties.Resources.icon;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ALMSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Auto Legality Plugin Settings";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SettingsEditor_KeyDown);
            this.FormClosing += new FormClosingEventHandler(this.ALMSettings_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid PG_Settings;
    }
}