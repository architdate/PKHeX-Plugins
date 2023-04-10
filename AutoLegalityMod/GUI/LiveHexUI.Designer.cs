﻿namespace AutoModPlugins
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
            this.B_Disconnect = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
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
            this.L_ReadRamOffset = new System.Windows.Forms.Label();
            this.L_USBState = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.B_ReadPointer = new System.Windows.Forms.Button();
            this.B_CopyAddress = new System.Windows.Forms.Button();
            this.B_EditPointer = new System.Windows.Forms.Button();
            this.L_Pointer = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.CB_BlockName = new System.Windows.Forms.ComboBox();
            this.B_EditBlock = new System.Windows.Forms.Button();
            this.L_Block = new System.Windows.Forms.Label();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.RB_Absolute = new System.Windows.Forms.RadioButton();
            this.RB_Main = new System.Windows.Forms.RadioButton();
            this.RB_Heap = new System.Windows.Forms.RadioButton();
            this.L_OffsRelative = new System.Windows.Forms.Label();
            this.TB_Pointer = new AutoModPlugins.HexTextBox();
            this.RamOffset = new AutoModPlugins.HexTextBox();
            this.TB_Offset = new AutoModPlugins.HexTextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Slot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NUD_Box)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
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
            this.B_WriteCurrent.Location = new System.Drawing.Point(13, 87);
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
            this.L_IP.Location = new System.Drawing.Point(4, 11);
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
            // L_Port
            // 
            this.L_Port.Location = new System.Drawing.Point(4, 37);
            this.L_Port.Name = "L_Port";
            this.L_Port.Size = new System.Drawing.Size(40, 20);
            this.L_Port.TabIndex = 6;
            this.L_Port.Text = "Port:";
            this.L_Port.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
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
            // B_Disconnect
            // 
            this.B_Disconnect.Location = new System.Drawing.Point(98, 37);
            this.B_Disconnect.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.B_Disconnect.Name = "B_Disconnect";
            this.B_Disconnect.Size = new System.Drawing.Size(63, 23);
            this.B_Disconnect.TabIndex = 15;
            this.B_Disconnect.Text = "Disconnect";
            this.B_Disconnect.UseVisualStyleBackColor = true;
            this.B_Disconnect.Visible = false;
            this.B_Disconnect.Click += new System.EventHandler(this.B_Disconnect_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBox2);
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.B_ReadCurrent);
            this.groupBox1.Controls.Add(this.B_WriteCurrent);
            this.groupBox1.Enabled = false;
            this.groupBox1.Location = new System.Drawing.Point(12, 66);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(149, 116);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Boxes";
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(13, 38);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(85, 17);
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
            this.groupBox2.Location = new System.Drawing.Point(171, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(149, 170);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "PKM Editor";
            // 
            // L_ReadOffset
            // 
            this.L_ReadOffset.Location = new System.Drawing.Point(17, 141);
            this.L_ReadOffset.Name = "L_ReadOffset";
            this.L_ReadOffset.Size = new System.Drawing.Size(42, 20);
            this.L_ReadOffset.TabIndex = 15;
            this.L_ReadOffset.Text = "Offset:";
            this.L_ReadOffset.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // B_ReadOffset
            // 
            this.B_ReadOffset.Location = new System.Drawing.Point(13, 115);
            this.B_ReadOffset.Name = "B_ReadOffset";
            this.B_ReadOffset.Size = new System.Drawing.Size(125, 23);
            this.B_ReadOffset.TabIndex = 13;
            this.B_ReadOffset.Text = "Read from Offset";
            this.B_ReadOffset.UseVisualStyleBackColor = true;
            this.B_ReadOffset.Click += new System.EventHandler(this.B_ReadOffset_Click);
            // 
            // L_Slot
            // 
            this.L_Slot.Location = new System.Drawing.Point(27, 88);
            this.L_Slot.Name = "L_Slot";
            this.L_Slot.Size = new System.Drawing.Size(42, 20);
            this.L_Slot.TabIndex = 12;
            this.L_Slot.Text = "Slot:";
            this.L_Slot.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NUD_Slot
            // 
            this.NUD_Slot.Location = new System.Drawing.Point(75, 90);
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
            this.L_Box.Location = new System.Drawing.Point(27, 67);
            this.L_Box.Name = "L_Box";
            this.L_Box.Size = new System.Drawing.Size(42, 20);
            this.L_Box.TabIndex = 10;
            this.L_Box.Text = "Box:";
            this.L_Box.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // NUD_Box
            // 
            this.NUD_Box.Location = new System.Drawing.Point(75, 69);
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
            this.B_ReadSlot.Text = "Read from Slot";
            this.B_ReadSlot.UseVisualStyleBackColor = true;
            this.B_ReadSlot.Click += new System.EventHandler(this.B_ReadSlot_Click);
            // 
            // B_WriteSlot
            // 
            this.B_WriteSlot.Location = new System.Drawing.Point(13, 44);
            this.B_WriteSlot.Name = "B_WriteSlot";
            this.B_WriteSlot.Size = new System.Drawing.Size(125, 23);
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
            this.groupBox3.Location = new System.Drawing.Point(12, 188);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(307, 48);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "RAM Editor";
            // 
            // B_ReadRAM
            // 
            this.B_ReadRAM.Location = new System.Drawing.Point(225, 17);
            this.B_ReadRAM.Name = "B_ReadRAM";
            this.B_ReadRAM.Size = new System.Drawing.Size(72, 23);
            this.B_ReadRAM.TabIndex = 21;
            this.B_ReadRAM.Text = "Edit RAM";
            this.B_ReadRAM.UseVisualStyleBackColor = true;
            this.B_ReadRAM.Click += new System.EventHandler(this.B_ReadRAM_Click);
            // 
            // RamSize
            // 
            this.RamSize.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RamSize.Location = new System.Drawing.Point(165, 18);
            this.RamSize.MaxLength = 8;
            this.RamSize.Name = "RamSize";
            this.RamSize.Size = new System.Drawing.Size(55, 20);
            this.RamSize.TabIndex = 20;
            this.RamSize.Text = "344";
            // 
            // L_ReadRamSize
            // 
            this.L_ReadRamSize.Location = new System.Drawing.Point(128, 17);
            this.L_ReadRamSize.Name = "L_ReadRamSize";
            this.L_ReadRamSize.Size = new System.Drawing.Size(36, 20);
            this.L_ReadRamSize.TabIndex = 19;
            this.L_ReadRamSize.Text = "Size:";
            this.L_ReadRamSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // L_ReadRamOffset
            // 
            this.L_ReadRamOffset.Location = new System.Drawing.Point(10, 17);
            this.L_ReadRamOffset.Name = "L_ReadRamOffset";
            this.L_ReadRamOffset.Size = new System.Drawing.Size(42, 20);
            this.L_ReadRamOffset.TabIndex = 17;
            this.L_ReadRamOffset.Text = "Offset:";
            this.L_ReadRamOffset.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // L_USBState
            // 
            this.L_USBState.AutoSize = true;
            this.L_USBState.Location = new System.Drawing.Point(15, 15);
            this.L_USBState.Name = "L_USBState";
            this.L_USBState.Size = new System.Drawing.Size(101, 13);
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
            this.groupBox4.Location = new System.Drawing.Point(12, 242);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(307, 73);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Pointer Lookup";
            // 
            // B_ReadPointer
            // 
            this.B_ReadPointer.Location = new System.Drawing.Point(204, 43);
            this.B_ReadPointer.Name = "B_ReadPointer";
            this.B_ReadPointer.Size = new System.Drawing.Size(94, 23);
            this.B_ReadPointer.TabIndex = 23;
            this.B_ReadPointer.Text = "Read Pointer";
            this.B_ReadPointer.UseVisualStyleBackColor = true;
            this.B_ReadPointer.Click += new System.EventHandler(this.B_ReadPointer_Click);
            // 
            // B_CopyAddress
            // 
            this.B_CopyAddress.Location = new System.Drawing.Point(9, 43);
            this.B_CopyAddress.Name = "B_CopyAddress";
            this.B_CopyAddress.Size = new System.Drawing.Size(94, 23);
            this.B_CopyAddress.TabIndex = 22;
            this.B_CopyAddress.Text = "Copy Address";
            this.B_CopyAddress.UseVisualStyleBackColor = true;
            this.B_CopyAddress.Click += new System.EventHandler(this.B_CopyAddress_Click);
            // 
            // B_EditPointer
            // 
            this.B_EditPointer.Location = new System.Drawing.Point(107, 43);
            this.B_EditPointer.Name = "B_EditPointer";
            this.B_EditPointer.Size = new System.Drawing.Size(94, 23);
            this.B_EditPointer.TabIndex = 21;
            this.B_EditPointer.Text = "Edit RAM";
            this.B_EditPointer.UseVisualStyleBackColor = true;
            this.B_EditPointer.Click += new System.EventHandler(this.B_EditPointerData_Click);
            // 
            // L_Pointer
            // 
            this.L_Pointer.Location = new System.Drawing.Point(3, 16);
            this.L_Pointer.Name = "L_Pointer";
            this.L_Pointer.Size = new System.Drawing.Size(49, 20);
            this.L_Pointer.TabIndex = 17;
            this.L_Pointer.Text = "Pointer:";
            this.L_Pointer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.CB_BlockName);
            this.groupBox5.Controls.Add(this.B_EditBlock);
            this.groupBox5.Controls.Add(this.L_Block);
            this.groupBox5.Enabled = false;
            this.groupBox5.Location = new System.Drawing.Point(12, 378);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(307, 48);
            this.groupBox5.TabIndex = 13;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Block Editor";
            // 
            // CB_BlockName
            // 
            this.CB_BlockName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CB_BlockName.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CB_BlockName.Location = new System.Drawing.Point(58, 19);
            this.CB_BlockName.Name = "CB_BlockName";
            this.CB_BlockName.Size = new System.Drawing.Size(162, 22);
            this.CB_BlockName.Sorted = true;
            this.CB_BlockName.TabIndex = 22;
            // 
            // B_EditBlock
            // 
            this.B_EditBlock.Location = new System.Drawing.Point(225, 18);
            this.B_EditBlock.Name = "B_EditBlock";
            this.B_EditBlock.Size = new System.Drawing.Size(72, 24);
            this.B_EditBlock.TabIndex = 21;
            this.B_EditBlock.Text = "Edit Block";
            this.B_EditBlock.UseVisualStyleBackColor = true;
            this.B_EditBlock.Click += new System.EventHandler(this.B_EditBlock_Click);
            // 
            // L_Block
            // 
            this.L_Block.Location = new System.Drawing.Point(10, 18);
            this.L_Block.Name = "L_Block";
            this.L_Block.Size = new System.Drawing.Size(42, 20);
            this.L_Block.TabIndex = 17;
            this.L_Block.Text = "Block:";
            this.L_Block.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.RB_Absolute);
            this.groupBox6.Controls.Add(this.RB_Main);
            this.groupBox6.Controls.Add(this.RB_Heap);
            this.groupBox6.Controls.Add(this.L_OffsRelative);
            this.groupBox6.Enabled = false;
            this.groupBox6.Location = new System.Drawing.Point(12, 322);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(308, 50);
            this.groupBox6.TabIndex = 14;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "RAM Config";
            // 
            // RB_Absolute
            // 
            this.RB_Absolute.AutoSize = true;
            this.RB_Absolute.Location = new System.Drawing.Point(238, 20);
            this.RB_Absolute.Name = "RB_Absolute";
            this.RB_Absolute.Size = new System.Drawing.Size(66, 17);
            this.RB_Absolute.TabIndex = 3;
            this.RB_Absolute.Text = "Absolute";
            this.RB_Absolute.UseVisualStyleBackColor = true;
            // 
            // RB_Main
            // 
            this.RB_Main.AutoSize = true;
            this.RB_Main.Location = new System.Drawing.Point(189, 20);
            this.RB_Main.Name = "RB_Main";
            this.RB_Main.Size = new System.Drawing.Size(48, 17);
            this.RB_Main.TabIndex = 2;
            this.RB_Main.Text = "Main";
            this.RB_Main.UseVisualStyleBackColor = true;
            // 
            // RB_Heap
            // 
            this.RB_Heap.AutoSize = true;
            this.RB_Heap.Checked = true;
            this.RB_Heap.Location = new System.Drawing.Point(137, 20);
            this.RB_Heap.Name = "RB_Heap";
            this.RB_Heap.Size = new System.Drawing.Size(51, 17);
            this.RB_Heap.TabIndex = 1;
            this.RB_Heap.TabStop = true;
            this.RB_Heap.Text = "Heap";
            this.RB_Heap.UseVisualStyleBackColor = true;
            // 
            // L_OffsRelative
            // 
            this.L_OffsRelative.AutoSize = true;
            this.L_OffsRelative.Location = new System.Drawing.Point(13, 21);
            this.L_OffsRelative.Name = "L_OffsRelative";
            this.L_OffsRelative.Size = new System.Drawing.Size(117, 13);
            this.L_OffsRelative.TabIndex = 0;
            this.L_OffsRelative.Text = "RAM offsets relative to:";
            // 
            // TB_Pointer
            // 
            this.TB_Pointer.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.TB_Pointer.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.TB_Pointer.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_Pointer.Location = new System.Drawing.Point(58, 18);
            this.TB_Pointer.Name = "TB_Pointer";
            this.TB_Pointer.Size = new System.Drawing.Size(239, 20);
            this.TB_Pointer.TabIndex = 18;
            // 
            // RamOffset
            // 
            this.RamOffset.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RamOffset.Location = new System.Drawing.Point(58, 18);
            this.RamOffset.MaxLength = 16;
            this.RamOffset.Name = "RamOffset";
            this.RamOffset.Size = new System.Drawing.Size(70, 20);
            this.RamOffset.TabIndex = 18;
            this.RamOffset.Text = "2E32206A";
            // 
            // TB_Offset
            // 
            this.TB_Offset.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TB_Offset.Location = new System.Drawing.Point(65, 141);
            this.TB_Offset.MaxLength = 16;
            this.TB_Offset.Name = "TB_Offset";
            this.TB_Offset.Size = new System.Drawing.Size(70, 20);
            this.TB_Offset.TabIndex = 16;
            this.TB_Offset.Text = "2E32206A";
            // 
            // LiveHeXUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 435);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.B_Connect);
            this.Controls.Add(this.B_Disconnect);
            this.Controls.Add(this.L_Port);
            this.Controls.Add(this.TB_Port);
            this.Controls.Add(this.L_IP);
            this.Controls.Add(this.TB_IP);
            this.Controls.Add(this.L_USBState);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
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
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
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
        private System.Windows.Forms.Button B_Disconnect;
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
        private System.Windows.Forms.ComboBox CB_BlockName;
        private System.Windows.Forms.Button B_EditBlock;
        private System.Windows.Forms.Label L_Block;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.RadioButton RB_Absolute;
        private System.Windows.Forms.RadioButton RB_Main;
        private System.Windows.Forms.RadioButton RB_Heap;
        private System.Windows.Forms.Label L_OffsRelative;
    }
}