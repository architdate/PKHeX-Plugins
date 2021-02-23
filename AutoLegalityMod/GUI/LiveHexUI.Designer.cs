namespace AutoModPlugins
{
    partial class LiveHeXUI
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
            this.L_Port = new System.Windows.Forms.Label();
            this.B_Connect = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.TB_Offset = new AutoModPlugins.HexTextBox();
            this.L_ReadOffset = new System.Windows.Forms.Label();
            this.B_ReadOffset = new System.Windows.Forms.Button();
            this.L_Slot = new System.Windows.Forms.Label();
            this.NUD_Slot = new System.Windows.Forms.NumericUpDown();
            this.L_Box = new System.Windows.Forms.Label();
            this.NUD_Box = new System.Windows.Forms.NumericUpDown();
            this.B_ReadSlot = new System.Windows.Forms.Button();
            this.B_WriteSlot = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.B_ReadRAM = new System.Windows.Forms.Button();
            this.RamSize = new System.Windows.Forms.TextBox();
            this.L_ReadRamSize = new System.Windows.Forms.Label();
            this.RamOffset = new AutoModPlugins.HexTextBox();
            this.L_ReadRamOffset = new System.Windows.Forms.Label();
            this.L_USBState = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.B_ReadPointer = new System.Windows.Forms.Button();
            this.B_CopyAddress = new System.Windows.Forms.Button();
            this.B_EditPointer = new System.Windows.Forms.Button();
            this.TB_Pointer = new AutoModPlugins.HexTextBox();
            this.L_Pointer = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.B_EditBlock = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.TB_BlockName = new AutoModPlugins.HexTextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Slot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Box)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // B_ReadCurrent
            // 
            this.B_ReadCurrent.Location = new System.Drawing.Point(17, 75);
            this.B_ReadCurrent.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_ReadCurrent.Name = "B_ReadCurrent";
            this.B_ReadCurrent.Size = new System.Drawing.Size(167, 28);
            this.B_ReadCurrent.TabIndex = 0;
            this.B_ReadCurrent.Text = "Read Current Box";
            this.B_ReadCurrent.UseVisualStyleBackColor = true;
            this.B_ReadCurrent.Click += new System.EventHandler(this.B_ReadCurrent_Click);
            // 
            // B_WriteCurrent
            // 
            this.B_WriteCurrent.Location = new System.Drawing.Point(17, 107);
            this.B_WriteCurrent.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_WriteCurrent.Name = "B_WriteCurrent";
            this.B_WriteCurrent.Size = new System.Drawing.Size(167, 28);
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
            this.checkBox1.Location = new System.Drawing.Point(17, 23);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(167, 21);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "Read On Change Box";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // TB_IP
            // 
            this.TB_IP.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_IP.Location = new System.Drawing.Point(67, 15);
            this.TB_IP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.TB_IP.Name = "TB_IP";
            this.TB_IP.Size = new System.Drawing.Size(147, 23);
            this.TB_IP.TabIndex = 3;
            this.TB_IP.Text = "111.111.111.111";
            // 
            // L_IP
            // 
            this.L_IP.Location = new System.Drawing.Point(5, 14);
            this.L_IP.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_IP.Name = "L_IP";
            this.L_IP.Size = new System.Drawing.Size(53, 25);
            this.L_IP.TabIndex = 4;
            this.L_IP.Text = "IP:";
            this.L_IP.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TB_Port
            // 
            this.TB_Port.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_Port.Location = new System.Drawing.Point(67, 47);
            this.TB_Port.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.TB_Port.Name = "TB_Port";
            this.TB_Port.Size = new System.Drawing.Size(55, 23);
            this.TB_Port.TabIndex = 5;
            this.TB_Port.Text = "65535";
            // 
            // L_Port
            // 
            this.L_Port.Location = new System.Drawing.Point(5, 46);
            this.L_Port.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_Port.Name = "L_Port";
            this.L_Port.Size = new System.Drawing.Size(53, 25);
            this.L_Port.TabIndex = 6;
            this.L_Port.Text = "Port:";
            this.L_Port.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // B_Connect
            // 
            this.B_Connect.Location = new System.Drawing.Point(131, 46);
            this.B_Connect.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_Connect.Name = "B_Connect";
            this.B_Connect.Size = new System.Drawing.Size(84, 28);
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
            this.groupBox1.Enabled = false;
            this.groupBox1.Location = new System.Drawing.Point(16, 81);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(199, 143);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Boxes";
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(17, 47);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(106, 21);
            this.checkBox2.TabIndex = 3;
            this.checkBox2.Text = "Inject In Slot";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.TB_Offset);
            this.groupBox2.Controls.Add(this.L_ReadOffset);
            this.groupBox2.Controls.Add(this.B_ReadOffset);
            this.groupBox2.Controls.Add(this.L_Slot);
            this.groupBox2.Controls.Add(this.NUD_Slot);
            this.groupBox2.Controls.Add(this.L_Box);
            this.groupBox2.Controls.Add(this.NUD_Box);
            this.groupBox2.Controls.Add(this.B_ReadSlot);
            this.groupBox2.Controls.Add(this.B_WriteSlot);
            this.groupBox2.Enabled = false;
            this.groupBox2.Location = new System.Drawing.Point(228, 15);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox2.Size = new System.Drawing.Size(199, 209);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "PKM Editor";
            // 
            // TB_Offset
            // 
            this.TB_Offset.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_Offset.Location = new System.Drawing.Point(87, 174);
            this.TB_Offset.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.TB_Offset.MaxLength = 8;
            this.TB_Offset.Name = "TB_Offset";
            this.TB_Offset.Size = new System.Drawing.Size(83, 23);
            this.TB_Offset.TabIndex = 16;
            this.TB_Offset.Text = "2E32206A";
            // 
            // L_ReadOffset
            // 
            this.L_ReadOffset.Location = new System.Drawing.Point(23, 174);
            this.L_ReadOffset.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_ReadOffset.Name = "L_ReadOffset";
            this.L_ReadOffset.Size = new System.Drawing.Size(56, 25);
            this.L_ReadOffset.TabIndex = 15;
            this.L_ReadOffset.Text = "Offset:";
            this.L_ReadOffset.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // B_ReadOffset
            // 
            this.B_ReadOffset.Location = new System.Drawing.Point(17, 142);
            this.B_ReadOffset.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_ReadOffset.Name = "B_ReadOffset";
            this.B_ReadOffset.Size = new System.Drawing.Size(167, 28);
            this.B_ReadOffset.TabIndex = 13;
            this.B_ReadOffset.Text = "Read from Offset";
            this.B_ReadOffset.UseVisualStyleBackColor = true;
            this.B_ReadOffset.Click += new System.EventHandler(this.B_ReadOffset_Click);
            // 
            // L_Slot
            // 
            this.L_Slot.Location = new System.Drawing.Point(36, 108);
            this.L_Slot.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_Slot.Name = "L_Slot";
            this.L_Slot.Size = new System.Drawing.Size(56, 25);
            this.L_Slot.TabIndex = 12;
            this.L_Slot.Text = "Slot:";
            this.L_Slot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NUD_Slot
            // 
            this.NUD_Slot.Location = new System.Drawing.Point(100, 111);
            this.NUD_Slot.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.NUD_Slot.Size = new System.Drawing.Size(51, 22);
            this.NUD_Slot.TabIndex = 11;
            this.NUD_Slot.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // L_Box
            // 
            this.L_Box.Location = new System.Drawing.Point(36, 82);
            this.L_Box.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_Box.Name = "L_Box";
            this.L_Box.Size = new System.Drawing.Size(56, 25);
            this.L_Box.TabIndex = 10;
            this.L_Box.Text = "Box:";
            this.L_Box.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NUD_Box
            // 
            this.NUD_Box.Location = new System.Drawing.Point(100, 85);
            this.NUD_Box.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.NUD_Box.Size = new System.Drawing.Size(51, 22);
            this.NUD_Box.TabIndex = 2;
            this.NUD_Box.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // B_ReadSlot
            // 
            this.B_ReadSlot.Location = new System.Drawing.Point(17, 22);
            this.B_ReadSlot.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_ReadSlot.Name = "B_ReadSlot";
            this.B_ReadSlot.Size = new System.Drawing.Size(167, 28);
            this.B_ReadSlot.TabIndex = 0;
            this.B_ReadSlot.Text = "Read from Slot";
            this.B_ReadSlot.UseVisualStyleBackColor = true;
            this.B_ReadSlot.Click += new System.EventHandler(this.B_ReadSlot_Click);
            // 
            // B_WriteSlot
            // 
            this.B_WriteSlot.Location = new System.Drawing.Point(17, 54);
            this.B_WriteSlot.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_WriteSlot.Name = "B_WriteSlot";
            this.B_WriteSlot.Size = new System.Drawing.Size(167, 28);
            this.B_WriteSlot.TabIndex = 1;
            this.B_WriteSlot.Text = "Write to Slot";
            this.B_WriteSlot.UseVisualStyleBackColor = true;
            this.B_WriteSlot.Click += new System.EventHandler(this.B_WriteSlot_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.B_ReadRAM);
            this.groupBox3.Controls.Add(this.RamSize);
            this.groupBox3.Controls.Add(this.L_ReadRamSize);
            this.groupBox3.Controls.Add(this.RamOffset);
            this.groupBox3.Controls.Add(this.L_ReadRamOffset);
            this.groupBox3.Enabled = false;
            this.groupBox3.Location = new System.Drawing.Point(16, 231);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox3.Size = new System.Drawing.Size(409, 59);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "RAM Editor";
            // 
            // B_ReadRAM
            // 
            this.B_ReadRAM.Location = new System.Drawing.Point(300, 20);
            this.B_ReadRAM.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_ReadRAM.Name = "B_ReadRAM";
            this.B_ReadRAM.Size = new System.Drawing.Size(100, 28);
            this.B_ReadRAM.TabIndex = 21;
            this.B_ReadRAM.Text = "Edit RAM";
            this.B_ReadRAM.UseVisualStyleBackColor = true;
            this.B_ReadRAM.Click += new System.EventHandler(this.B_ReadRAM_Click);
            // 
            // RamSize
            // 
            this.RamSize.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RamSize.Location = new System.Drawing.Point(205, 23);
            this.RamSize.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.RamSize.MaxLength = 8;
            this.RamSize.Name = "RamSize";
            this.RamSize.Size = new System.Drawing.Size(83, 23);
            this.RamSize.TabIndex = 20;
            this.RamSize.Text = "344";
            // 
            // L_ReadRamSize
            // 
            this.L_ReadRamSize.Location = new System.Drawing.Point(157, 21);
            this.L_ReadRamSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_ReadRamSize.Name = "L_ReadRamSize";
            this.L_ReadRamSize.Size = new System.Drawing.Size(40, 25);
            this.L_ReadRamSize.TabIndex = 19;
            this.L_ReadRamSize.Text = "Size:";
            this.L_ReadRamSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // RamOffset
            // 
            this.RamOffset.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RamOffset.Location = new System.Drawing.Point(68, 23);
            this.RamOffset.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.RamOffset.MaxLength = 8;
            this.RamOffset.Name = "RamOffset";
            this.RamOffset.Size = new System.Drawing.Size(83, 23);
            this.RamOffset.TabIndex = 18;
            this.RamOffset.Text = "2E32206A";
            // 
            // L_ReadRamOffset
            // 
            this.L_ReadRamOffset.Location = new System.Drawing.Point(4, 21);
            this.L_ReadRamOffset.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_ReadRamOffset.Name = "L_ReadRamOffset";
            this.L_ReadRamOffset.Size = new System.Drawing.Size(56, 25);
            this.L_ReadRamOffset.TabIndex = 17;
            this.L_ReadRamOffset.Text = "Offset:";
            this.L_ReadRamOffset.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // L_USBState
            // 
            this.L_USBState.AutoSize = true;
            this.L_USBState.Location = new System.Drawing.Point(20, 18);
            this.L_USBState.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_USBState.Name = "L_USBState";
            this.L_USBState.Size = new System.Drawing.Size(132, 17);
            this.L_USBState.TabIndex = 11;
            this.L_USBState.Text = "USB-Botbase Mode";
            this.L_USBState.Visible = false;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.B_ReadPointer);
            this.groupBox4.Controls.Add(this.B_CopyAddress);
            this.groupBox4.Controls.Add(this.B_EditPointer);
            this.groupBox4.Controls.Add(this.TB_Pointer);
            this.groupBox4.Controls.Add(this.L_Pointer);
            this.groupBox4.Enabled = false;
            this.groupBox4.Location = new System.Drawing.Point(16, 298);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox4.Size = new System.Drawing.Size(409, 90);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Pointer Lookup";
            // 
            // B_ReadPointer
            // 
            this.B_ReadPointer.Location = new System.Drawing.Point(272, 53);
            this.B_ReadPointer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_ReadPointer.Name = "B_ReadPointer";
            this.B_ReadPointer.Size = new System.Drawing.Size(125, 28);
            this.B_ReadPointer.TabIndex = 23;
            this.B_ReadPointer.Text = "Read Pointer";
            this.B_ReadPointer.UseVisualStyleBackColor = true;
            this.B_ReadPointer.Click += new System.EventHandler(this.B_ReadPointer_Click);
            // 
            // B_CopyAddress
            // 
            this.B_CopyAddress.Location = new System.Drawing.Point(12, 53);
            this.B_CopyAddress.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_CopyAddress.Name = "B_CopyAddress";
            this.B_CopyAddress.Size = new System.Drawing.Size(125, 28);
            this.B_CopyAddress.TabIndex = 22;
            this.B_CopyAddress.Text = "Copy Address";
            this.B_CopyAddress.UseVisualStyleBackColor = true;
            this.B_CopyAddress.Click += new System.EventHandler(this.B_CopyAddress_Click);
            // 
            // B_EditPointer
            // 
            this.B_EditPointer.Location = new System.Drawing.Point(143, 53);
            this.B_EditPointer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.B_EditPointer.Name = "B_EditPointer";
            this.B_EditPointer.Size = new System.Drawing.Size(125, 28);
            this.B_EditPointer.TabIndex = 21;
            this.B_EditPointer.Text = "Edit RAM";
            this.B_EditPointer.UseVisualStyleBackColor = true;
            this.B_EditPointer.Click += new System.EventHandler(this.B_EditPointerData_Click);
            // 
            // TB_Pointer
            // 
            this.TB_Pointer.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_Pointer.Location = new System.Drawing.Point(83, 22);
            this.TB_Pointer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.TB_Pointer.Name = "TB_Pointer";
            this.TB_Pointer.Size = new System.Drawing.Size(312, 23);
            this.TB_Pointer.TabIndex = 18;
            // 
            // L_Pointer
            // 
            this.L_Pointer.Location = new System.Drawing.Point(11, 20);
            this.L_Pointer.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.L_Pointer.Name = "L_Pointer";
            this.L_Pointer.Size = new System.Drawing.Size(65, 25);
            this.L_Pointer.TabIndex = 17;
            this.L_Pointer.Text = "Pointer:";
            this.L_Pointer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.TB_BlockName);
            this.groupBox5.Controls.Add(this.B_EditBlock);
            this.groupBox5.Controls.Add(this.label2);
            this.groupBox5.Enabled = false;
            this.groupBox5.Location = new System.Drawing.Point(16, 396);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox5.Size = new System.Drawing.Size(409, 53);
            this.groupBox5.TabIndex = 13;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Block Editor";
            // 
            // B_EditBlock
            // 
            this.B_EditBlock.Location = new System.Drawing.Point(300, 20);
            this.B_EditBlock.Margin = new System.Windows.Forms.Padding(4);
            this.B_EditBlock.Name = "B_EditBlock";
            this.B_EditBlock.Size = new System.Drawing.Size(100, 28);
            this.B_EditBlock.TabIndex = 21;
            this.B_EditBlock.Text = "Edit Block";
            this.B_EditBlock.UseVisualStyleBackColor = true;
            this.B_EditBlock.Click += new System.EventHandler(this.B_EditBlock_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(4, 21);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 25);
            this.label2.TabIndex = 17;
            this.label2.Text = "Block:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TB_BlockName
            // 
            this.TB_BlockName.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_BlockName.Location = new System.Drawing.Point(68, 23);
            this.TB_BlockName.Margin = new System.Windows.Forms.Padding(4);
            this.TB_BlockName.Name = "TB_BlockName";
            this.TB_BlockName.Size = new System.Drawing.Size(224, 23);
            this.TB_BlockName.TabIndex = 22;
            // 
            // LiveHeXUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(441, 453);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.B_Connect);
            this.Controls.Add(this.L_Port);
            this.Controls.Add(this.TB_Port);
            this.Controls.Add(this.L_IP);
            this.Controls.Add(this.TB_IP);
            this.Controls.Add(this.L_USBState);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LiveHeXUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "LiveHeXUI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LiveHeXUI_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Slot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Box)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
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
        private System.Windows.Forms.Label L_Port;
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
        private System.Windows.Forms.Label L_ReadOffset;
        private System.Windows.Forms.Button B_ReadOffset;
        private HexTextBox TB_Offset;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button B_ReadRAM;
        private System.Windows.Forms.TextBox RamSize;
        private System.Windows.Forms.Label L_ReadRamSize;
        private HexTextBox RamOffset;
        private System.Windows.Forms.Label L_ReadRamOffset;
        private System.Windows.Forms.Label L_USBState;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button B_EditPointer;
        private HexTextBox TB_Pointer;
        private System.Windows.Forms.Label L_Pointer;
        private System.Windows.Forms.Button B_CopyAddress;
        private System.Windows.Forms.Button B_ReadPointer;
        private System.Windows.Forms.GroupBox groupBox5;
        private HexTextBox TB_BlockName;
        private System.Windows.Forms.Button B_EditBlock;
        private System.Windows.Forms.Label label2;
    }
}