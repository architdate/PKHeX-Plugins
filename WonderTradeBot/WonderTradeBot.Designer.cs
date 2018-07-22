namespace WonderTradeBot
{
    partial class WonderTradeBot
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WonderTradeBot));
            this.WTAfter = new System.Windows.Forms.GroupBox();
            this.afterDelete = new System.Windows.Forms.RadioButton();
            this.afterRestore = new System.Windows.Forms.RadioButton();
            this.afterNothing = new System.Windows.Forms.RadioButton();
            this.afterDump = new System.Windows.Forms.CheckBox();
            this.label58 = new System.Windows.Forms.Label();
            this.runEndless = new System.Windows.Forms.CheckBox();
            this.label57 = new System.Windows.Forms.Label();
            this.RunStop = new System.Windows.Forms.Button();
            this.Box = new System.Windows.Forms.NumericUpDown();
            this.WTSource = new System.Windows.Forms.GroupBox();
            this.sourceRandom = new System.Windows.Forms.RadioButton();
            this.sourceFolder = new System.Windows.Forms.RadioButton();
            this.sourceBox = new System.Windows.Forms.RadioButton();
            this.label59 = new System.Windows.Forms.Label();
            this.Trades = new System.Windows.Forms.NumericUpDown();
            this.Slot = new System.Windows.Forms.NumericUpDown();
            this.collectFC = new System.Windows.Forms.CheckBox();
            this.WTAfter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Box)).BeginInit();
            this.WTSource.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Trades)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Slot)).BeginInit();
            this.SuspendLayout();
            // 
            // WTAfter
            // 
            this.WTAfter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.WTAfter.Controls.Add(this.afterDelete);
            this.WTAfter.Controls.Add(this.afterRestore);
            this.WTAfter.Controls.Add(this.afterNothing);
            this.WTAfter.Controls.Add(this.afterDump);
            this.WTAfter.Location = new System.Drawing.Point(156, 12);
            this.WTAfter.Name = "WTAfter";
            this.WTAfter.Size = new System.Drawing.Size(148, 120);
            this.WTAfter.TabIndex = 4;
            this.WTAfter.TabStop = false;
            this.WTAfter.Text = "After trading";
            // 
            // afterDelete
            // 
            this.afterDelete.AutoSize = true;
            this.afterDelete.Location = new System.Drawing.Point(6, 88);
            this.afterDelete.Name = "afterDelete";
            this.afterDelete.Size = new System.Drawing.Size(136, 17);
            this.afterDelete.TabIndex = 3;
            this.afterDelete.Text = "Delete traded pokémon";
            this.afterDelete.UseVisualStyleBackColor = true;
            // 
            // afterRestore
            // 
            this.afterRestore.AutoSize = true;
            this.afterRestore.Location = new System.Drawing.Point(6, 65);
            this.afterRestore.Name = "afterRestore";
            this.afterRestore.Size = new System.Drawing.Size(101, 17);
            this.afterRestore.TabIndex = 2;
            this.afterRestore.Text = "Restore backup";
            this.afterRestore.UseVisualStyleBackColor = true;
            // 
            // afterNothing
            // 
            this.afterNothing.AutoSize = true;
            this.afterNothing.Checked = true;
            this.afterNothing.Location = new System.Drawing.Point(6, 42);
            this.afterNothing.Name = "afterNothing";
            this.afterNothing.Size = new System.Drawing.Size(77, 17);
            this.afterNothing.TabIndex = 1;
            this.afterNothing.TabStop = true;
            this.afterNothing.Text = "Do nothing";
            this.afterNothing.UseVisualStyleBackColor = true;
            // 
            // afterDump
            // 
            this.afterDump.AutoSize = true;
            this.afterDump.Location = new System.Drawing.Point(6, 19);
            this.afterDump.Name = "afterDump";
            this.afterDump.Size = new System.Drawing.Size(109, 17);
            this.afterDump.TabIndex = 0;
            this.afterDump.Text = "Dump boxes and:";
            this.afterDump.UseVisualStyleBackColor = true;
            // 
            // label58
            // 
            this.label58.AutoSize = true;
            this.label58.Location = new System.Drawing.Point(12, 9);
            this.label58.Name = "label58";
            this.label58.Size = new System.Drawing.Size(28, 13);
            this.label58.TabIndex = 17;
            this.label58.Text = "Box:";
            // 
            // runEndless
            // 
            this.runEndless.AutoSize = true;
            this.runEndless.Location = new System.Drawing.Point(214, 165);
            this.runEndless.Name = "runEndless";
            this.runEndless.Size = new System.Drawing.Size(90, 17);
            this.runEndless.TabIndex = 7;
            this.runEndless.Text = "Run endessly";
            this.runEndless.UseVisualStyleBackColor = true;
            // 
            // label57
            // 
            this.label57.AutoSize = true;
            this.label57.Location = new System.Drawing.Point(55, 9);
            this.label57.Name = "label57";
            this.label57.Size = new System.Drawing.Size(28, 13);
            this.label57.TabIndex = 18;
            this.label57.Text = "Slot:";
            // 
            // RunStop
            // 
            this.RunStop.Location = new System.Drawing.Point(12, 161);
            this.RunStop.Name = "RunStop";
            this.RunStop.Size = new System.Drawing.Size(196, 23);
            this.RunStop.TabIndex = 6;
            this.RunStop.Text = "Start Bot";
            this.RunStop.UseVisualStyleBackColor = true;
            this.RunStop.Click += new System.EventHandler(this.RunStop_Click);
            // 
            // Box
            // 
            this.Box.Location = new System.Drawing.Point(12, 28);
            this.Box.Maximum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.Box.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Box.Name = "Box";
            this.Box.Size = new System.Drawing.Size(40, 20);
            this.Box.TabIndex = 0;
            this.Box.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Box.ValueChanged += new System.EventHandler(this.Box_ValueChanged);
            // 
            // WTSource
            // 
            this.WTSource.AutoSize = true;
            this.WTSource.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.WTSource.Controls.Add(this.sourceRandom);
            this.WTSource.Controls.Add(this.sourceFolder);
            this.WTSource.Controls.Add(this.sourceBox);
            this.WTSource.Location = new System.Drawing.Point(12, 54);
            this.WTSource.Name = "WTSource";
            this.WTSource.Size = new System.Drawing.Size(138, 101);
            this.WTSource.TabIndex = 3;
            this.WTSource.TabStop = false;
            this.WTSource.Text = "Pokémon Source";
            // 
            // sourceRandom
            // 
            this.sourceRandom.AutoSize = true;
            this.sourceRandom.Location = new System.Drawing.Point(6, 65);
            this.sourceRandom.Name = "sourceRandom";
            this.sourceRandom.Size = new System.Drawing.Size(124, 17);
            this.sourceRandom.TabIndex = 2;
            this.sourceRandom.Text = "WT Folder (Random)";
            this.sourceRandom.UseVisualStyleBackColor = true;
            // 
            // sourceFolder
            // 
            this.sourceFolder.AutoSize = true;
            this.sourceFolder.Location = new System.Drawing.Point(6, 42);
            this.sourceFolder.Name = "sourceFolder";
            this.sourceFolder.Size = new System.Drawing.Size(126, 17);
            this.sourceFolder.TabIndex = 1;
            this.sourceFolder.Text = "Wonder Trade Folder";
            this.sourceFolder.UseVisualStyleBackColor = true;
            // 
            // sourceBox
            // 
            this.sourceBox.AutoSize = true;
            this.sourceBox.Checked = true;
            this.sourceBox.Location = new System.Drawing.Point(6, 19);
            this.sourceBox.Name = "sourceBox";
            this.sourceBox.Size = new System.Drawing.Size(71, 17);
            this.sourceBox.TabIndex = 0;
            this.sourceBox.TabStop = true;
            this.sourceBox.Text = "PC Boxes";
            this.sourceBox.UseVisualStyleBackColor = true;
            // 
            // label59
            // 
            this.label59.AutoSize = true;
            this.label59.Location = new System.Drawing.Point(101, 9);
            this.label59.Name = "label59";
            this.label59.Size = new System.Drawing.Size(49, 13);
            this.label59.TabIndex = 19;
            this.label59.Text = "# trades:";
            // 
            // Trades
            // 
            this.Trades.Location = new System.Drawing.Point(104, 28);
            this.Trades.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.Trades.Name = "Trades";
            this.Trades.Size = new System.Drawing.Size(46, 20);
            this.Trades.TabIndex = 2;
            this.Trades.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // Slot
            // 
            this.Slot.Location = new System.Drawing.Point(58, 28);
            this.Slot.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.Slot.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Slot.Name = "Slot";
            this.Slot.Size = new System.Drawing.Size(40, 20);
            this.Slot.TabIndex = 1;
            this.Slot.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Slot.ValueChanged += new System.EventHandler(this.Box_ValueChanged);
            // 
            // collectFC
            // 
            this.collectFC.AutoSize = true;
            this.collectFC.Location = new System.Drawing.Point(162, 138);
            this.collectFC.Name = "collectFC";
            this.collectFC.Size = new System.Drawing.Size(134, 17);
            this.collectFC.TabIndex = 5;
            this.collectFC.Text = "Collect FC after a trade";
            this.collectFC.UseVisualStyleBackColor = true;
            // 
            // Bot_WonderTrade7
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(319, 197);
            this.Controls.Add(this.collectFC);
            this.Controls.Add(this.WTAfter);
            this.Controls.Add(this.label58);
            this.Controls.Add(this.runEndless);
            this.Controls.Add(this.label57);
            this.Controls.Add(this.RunStop);
            this.Controls.Add(this.Box);
            this.Controls.Add(this.WTSource);
            this.Controls.Add(this.label59);
            this.Controls.Add(this.Trades);
            this.Controls.Add(this.Slot);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Padding = new System.Windows.Forms.Padding(0, 0, 6, 6);
            this.Text = "Wonder Trade Bot";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Bot_WonderTrade7_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Bot_WonderTrade7_FormClosed);
            this.WTAfter.ResumeLayout(false);
            this.WTAfter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Box)).EndInit();
            this.WTSource.ResumeLayout(false);
            this.WTSource.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Trades)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Slot)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox WTAfter;
        private System.Windows.Forms.RadioButton afterDelete;
        private System.Windows.Forms.RadioButton afterRestore;
        private System.Windows.Forms.RadioButton afterNothing;
        private System.Windows.Forms.CheckBox afterDump;
        private System.Windows.Forms.Label label58;
        private System.Windows.Forms.CheckBox runEndless;
        private System.Windows.Forms.Label label57;
        private System.Windows.Forms.Button RunStop;
        private System.Windows.Forms.NumericUpDown Box;
        private System.Windows.Forms.GroupBox WTSource;
        private System.Windows.Forms.RadioButton sourceRandom;
        private System.Windows.Forms.RadioButton sourceFolder;
        private System.Windows.Forms.RadioButton sourceBox;
        private System.Windows.Forms.Label label59;
        private System.Windows.Forms.NumericUpDown Trades;
        private System.Windows.Forms.NumericUpDown Slot;
        private System.Windows.Forms.CheckBox collectFC;
    }
}
