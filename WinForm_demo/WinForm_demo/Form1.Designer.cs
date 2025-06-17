namespace WinForm_demo
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            button1 = new Button();
            textBox4 = new TextBox();
            label3 = new Label();
            textBox3 = new TextBox();
            label1 = new Label();
            button2 = new Button();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 4;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(button1, 0, 2);
            tableLayoutPanel1.Controls.Add(textBox4, 1, 1);
            tableLayoutPanel1.Controls.Add(label3, 0, 1);
            tableLayoutPanel1.Controls.Add(textBox3, 1, 0);
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(button2, 2, 2);
            tableLayoutPanel1.Location = new Point(12, 27);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(570, 266);
            tableLayoutPanel1.TabIndex = 6;
            tableLayoutPanel1.Paint += tableLayoutPanel1_Paint;
            // 
            // button1
            // 
            tableLayoutPanel1.SetColumnSpan(button1, 2);
            button1.FlatStyle = FlatStyle.System;
            button1.Font = new Font("Microsoft YaHei UI", 14F);
            button1.Location = new Point(100, 213);
            button1.Margin = new Padding(100, 15, 0, 0);
            button1.Name = "button1";
            button1.Size = new Size(100, 30);
            button1.TabIndex = 9;
            button1.Text = "登录";
            button1.UseVisualStyleBackColor = true;
            // 
            // textBox4
            // 
            tableLayoutPanel1.SetColumnSpan(textBox4, 2);
            textBox4.Font = new Font("Microsoft YaHei UI", 14F);
            textBox4.Location = new Point(97, 139);
            textBox4.Margin = new Padding(3, 40, 3, 3);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(284, 31);
            textBox4.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft YaHei UI", 14F);
            label3.Location = new Point(3, 139);
            label3.Margin = new Padding(3, 40, 3, 0);
            label3.Name = "label3";
            label3.Size = new Size(69, 25);
            label3.TabIndex = 7;
            label3.Text = "密码：";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // textBox3
            // 
            tableLayoutPanel1.SetColumnSpan(textBox3, 2);
            textBox3.Font = new Font("Microsoft YaHei UI", 14F);
            textBox3.Location = new Point(97, 40);
            textBox3.Margin = new Padding(3, 40, 3, 3);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(284, 31);
            textBox3.TabIndex = 6;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 14F);
            label1.Location = new Point(3, 40);
            label1.Margin = new Padding(3, 40, 3, 0);
            label1.Name = "label1";
            label1.Size = new Size(88, 25);
            label1.TabIndex = 3;
            label1.Text = "用户名：";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.Click += label1_Click_1;
            // 
            // button2
            // 
            tableLayoutPanel1.SetColumnSpan(button2, 2);
            button2.FlatStyle = FlatStyle.System;
            button2.Font = new Font("Microsoft YaHei UI", 14F);
            button2.Location = new Point(301, 213);
            button2.Margin = new Padding(95, 15, 3, 3);
            button2.Name = "button2";
            button2.Size = new Size(100, 30);
            button2.TabIndex = 11;
            button2.Text = "确定";
            button2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new Size(584, 361);
            Controls.Add(tableLayoutPanel1);
            MaximumSize = new Size(600, 400);
            MinimumSize = new Size(600, 400);
            Name = "Form1";
            Text = "Word";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private TextBox textBox4;
        private Label label3;
        private TextBox textBox3;
        private Label label1;
        private Button button1;
        private Button button2;
    }
}
