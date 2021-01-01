namespace ShopForm
{
    partial class Mysql
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.ip = new System.Windows.Forms.TextBox();
            this.port = new System.Windows.Forms.TextBox();
            this.username = new System.Windows.Forms.TextBox();
            this.dbname = new System.Windows.Forms.TextBox();
            this.password = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(52, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(116, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "您的数据库ip";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(52, 136);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "您的用户名";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(52, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(134, 18);
            this.label3.TabIndex = 2;
            this.label3.Text = "您的数据库端口";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(52, 230);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 18);
            this.label4.TabIndex = 3;
            this.label4.Text = "您的数据库名";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(52, 182);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 18);
            this.label5.TabIndex = 4;
            this.label5.Text = "您的密码";
            // 
            // ip
            // 
            this.ip.Location = new System.Drawing.Point(197, 39);
            this.ip.Name = "ip";
            this.ip.Size = new System.Drawing.Size(100, 28);
            this.ip.TabIndex = 6;
            // 
            // port
            // 
            this.port.Location = new System.Drawing.Point(197, 73);
            this.port.Name = "port";
            this.port.Size = new System.Drawing.Size(100, 28);
            this.port.TabIndex = 7;
            // 
            // username
            // 
            this.username.Location = new System.Drawing.Point(197, 126);
            this.username.Name = "username";
            this.username.Size = new System.Drawing.Size(100, 28);
            this.username.TabIndex = 8;
            // 
            // dbname
            // 
            this.dbname.Location = new System.Drawing.Point(197, 227);
            this.dbname.Name = "dbname";
            this.dbname.Size = new System.Drawing.Size(100, 28);
            this.dbname.TabIndex = 9;
            // 
            // password
            // 
            this.password.Location = new System.Drawing.Point(197, 172);
            this.password.Name = "password";
            this.password.Size = new System.Drawing.Size(100, 28);
            this.password.TabIndex = 10;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(55, 278);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(242, 43);
            this.button1.TabIndex = 11;
            this.button1.Text = "测试连接并保存";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Mysql
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(337, 355);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.password);
            this.Controls.Add(this.dbname);
            this.Controls.Add(this.username);
            this.Controls.Add(this.port);
            this.Controls.Add(this.ip);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Mysql";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Mysql_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox ip;
        private System.Windows.Forms.TextBox port;
        private System.Windows.Forms.TextBox username;
        private System.Windows.Forms.TextBox dbname;
        private System.Windows.Forms.TextBox password;
        private System.Windows.Forms.Button button1;
    }
}