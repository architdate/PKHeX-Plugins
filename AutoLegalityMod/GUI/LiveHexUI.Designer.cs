namespace AutoModPlugins
{
    partial class LiveHexUI
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
            this.B_ReadCurrent = new System.Windows.Forms.Button();
            this.B_WriteCurrent = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.TB_IP = new System.Windows.Forms.TextBox();
            this.L_IP = new System.Windows.Forms.Label();
            this.TB_Port = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.B_Connect = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.L_Slot = new System.Windows.Forms.Label();
            this.NUD_Slot = new System.Windows.Forms.NumericUpDown();
            this.L_Box = new System.Windows.Forms.Label();
            this.NUD_Box = new System.Windows.Forms.NumericUpDown();
            this.B_ReadSlot = new System.Windows.Forms.Button();
            this.B_WriteSlot = new System.Windows.Forms.Button();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Slot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Box)).BeginInit();
            this.SuspendLayout();
            // 
            // B_ReadCurrent
            // 
            this.B_ReadCurrent.Location = new System.Drawing.Point(13, 61);
            this.B_ReadCurrent.Name = "B_ReadCurrent";
            this.B_ReadCurrent.Size = new System.Drawing.Size(125, 23);
            this.B_ReadCurrent.TabIndex = 0;
            this.B_ReadCurrent.Text = "Read Current Box";
            this.B_ReadCurrent.UseVisualStyleBackColor = true;
            this.B_ReadCurrent.Click += new System.EventHandler(this.B_ReadCurrent_Click);
            // 
            // B_WriteCurrent
            // 
            this.B_WriteCurrent.Location = new System.Drawing.Point(13, 90);
            this.B_WriteCurrent.Name = "B_WriteCurrent";
            this.B_WriteCurrent.Size = new System.Drawing.Size(125, 23);
            this.B_WriteCurrent.TabIndex = 1;
            this.B_WriteCurrent.Text = "Write Current Box";
            this.B_WriteCurrent.UseVisualStyleBackColor = true;
            this.B_WriteCurrent.Click += new System.EventHandler(this.B_WriteCurrent_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Enabled = false;
            this.checkBox1.Location = new System.Drawing.Point(13, 19);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(130, 17);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "Read On Change Box";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // TB_IP
            // 
            this.TB_IP.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_IP.Location = new System.Drawing.Point(50, 12);
            this.TB_IP.Name = "TB_IP";
            this.TB_IP.Size = new System.Drawing.Size(111, 20);
            this.TB_IP.TabIndex = 3;
            this.TB_IP.Text = "111.111.111.111";
            // 
            // L_IP
            // 
            this.L_IP.Location = new System.Drawing.Point(9, 12);
            this.L_IP.Name = "L_IP";
            this.L_IP.Size = new System.Drawing.Size(40, 20);
            this.L_IP.TabIndex = 4;
            this.L_IP.Text = "IP:";
            this.L_IP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TB_Port
            // 
            this.TB_Port.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_Port.Location = new System.Drawing.Point(50, 38);
            this.TB_Port.Name = "TB_Port";
            this.TB_Port.Size = new System.Drawing.Size(42, 20);
            this.TB_Port.TabIndex = 5;
            this.TB_Port.Text = "65535";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(4, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "Port:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // B_Connect
            // 
            this.B_Connect.Location = new System.Drawing.Point(98, 37);
            this.B_Connect.Name = "B_Connect";
            this.B_Connect.Size = new System.Drawing.Size(63, 23);
            this.B_Connect.TabIndex = 7;
            this.B_Connect.Text = "Connect";
            this.B_Connect.UseVisualStyleBackColor = true;
            this.B_Connect.Click += new System.EventHandler(this.B_Connect_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBox2);
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.B_ReadCurrent);
            this.groupBox1.Controls.Add(this.B_WriteCurrent);
            this.groupBox1.Location = new System.Drawing.Point(12, 66);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(149, 126);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Boxes";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.L_Slot);
            this.groupBox2.Controls.Add(this.NUD_Slot);
            this.groupBox2.Controls.Add(this.L_Box);
            this.groupBox2.Controls.Add(this.NUD_Box);
            this.groupBox2.Controls.Add(this.B_ReadSlot);
            this.groupBox2.Controls.Add(this.B_WriteSlot);
            this.groupBox2.Location = new System.Drawing.Point(12, 198);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(149, 120);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "PKM Editor";
            // 
            // L_Slot
            // 
            this.L_Slot.Location = new System.Drawing.Point(27, 94);
            this.L_Slot.Name = "L_Slot";
            this.L_Slot.Size = new System.Drawing.Size(42, 20);
            this.L_Slot.TabIndex = 12;
            this.L_Slot.Text = "Slot:";
            this.L_Slot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NUD_Slot
            // 
            this.NUD_Slot.Location = new System.Drawing.Point(75, 94);
            this.NUD_Slot.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.NUD_Slot.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.NUD_Slot.Name = "NUD_Slot";
            this.NUD_Slot.Size = new System.Drawing.Size(38, 20);
            this.NUD_Slot.TabIndex = 11;
            this.NUD_Slot.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // L_Box
            // 
            this.L_Box.Location = new System.Drawing.Point(27, 73);
            this.L_Box.Name = "L_Box";
            this.L_Box.Size = new System.Drawing.Size(42, 20);
            this.L_Box.TabIndex = 10;
            this.L_Box.Text = "Box:";
            this.L_Box.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NUD_Box
            // 
            this.NUD_Box.Location = new System.Drawing.Point(75, 73);
            this.NUD_Box.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.NUD_Box.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.NUD_Box.Name = "NUD_Box";
            this.NUD_Box.Size = new System.Drawing.Size(38, 20);
            this.NUD_Box.TabIndex = 2;
            this.NUD_Box.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // B_ReadSlot
            // 
            this.B_ReadSlot.Location = new System.Drawing.Point(13, 18);
            this.B_ReadSlot.Name = "B_ReadSlot";
            this.B_ReadSlot.Size = new System.Drawing.Size(125, 23);
            this.B_ReadSlot.TabIndex = 0;
            this.B_ReadSlot.Text = "Read From Slot";
            this.B_ReadSlot.UseVisualStyleBackColor = true;
            this.B_ReadSlot.Click += new System.EventHandler(this.B_ReadSlot_Click);
            // 
            // B_WriteSlot
            // 
            this.B_WriteSlot.Location = new System.Drawing.Point(13, 47);
            this.B_WriteSlot.Name = "B_WriteSlot";
            this.B_WriteSlot.Size = new System.Drawing.Size(125, 23);
            this.B_WriteSlot.TabIndex = 1;
            this.B_WriteSlot.Text = "Write to Slot";
            this.B_WriteSlot.UseVisualStyleBackColor = true;
            this.B_WriteSlot.Click += new System.EventHandler(this.B_WriteSlot_Click);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Enabled = false;
            this.checkBox2.Location = new System.Drawing.Point(13, 38);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(85, 17);
            this.checkBox2.TabIndex = 3;
            this.checkBox2.Text = "Inject In Slot";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // LiveHexUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(174, 326);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.B_Connect);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TB_Port);
            this.Controls.Add(this.L_IP);
            this.Controls.Add(this.TB_IP);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LiveHexUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "LiveHexUI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LiveHexUI_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Slot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Box)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button B_ReadCurrent;
        private System.Windows.Forms.Button B_WriteCurrent;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TextBox TB_IP;
        private System.Windows.Forms.Label L_IP;
        private System.Windows.Forms.TextBox TB_Port;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button B_Connect;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button B_ReadSlot;
        private System.Windows.Forms.Button B_WriteSlot;
        private System.Windows.Forms.Label L_Slot;
        private System.Windows.Forms.NumericUpDown NUD_Slot;
        private System.Windows.Forms.Label L_Box;
        private System.Windows.Forms.NumericUpDown NUD_Box;
        private System.Windows.Forms.CheckBox checkBox2;
    }
}