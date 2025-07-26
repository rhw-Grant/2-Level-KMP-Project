namespace Matlab_connect
{
    partial class SerialTest
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.openSerial = new System.Windows.Forms.Button();
            this.ChangeData = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.OpenThreadBtn = new System.Windows.Forms.Button();
            this.stopThread_btn = new System.Windows.Forms.Button();
            this.CSV_read = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.sql_insert = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).BeginInit();
            this.SuspendLayout();
            // 
            // openSerial
            // 
            this.openSerial.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.openSerial.Location = new System.Drawing.Point(52, 17);
            this.openSerial.Name = "openSerial";
            this.openSerial.Size = new System.Drawing.Size(150, 71);
            this.openSerial.TabIndex = 0;
            this.openSerial.Text = "打开串口";
            this.openSerial.UseVisualStyleBackColor = true;
            this.openSerial.Click += new System.EventHandler(this.openSerial_Click);
            // 
            // ChangeData
            // 
            this.ChangeData.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ChangeData.Location = new System.Drawing.Point(52, 127);
            this.ChangeData.Name = "ChangeData";
            this.ChangeData.Size = new System.Drawing.Size(150, 67);
            this.ChangeData.TabIndex = 1;
            this.ChangeData.Text = "数据接收";
            this.ChangeData.UseVisualStyleBackColor = true;
            this.ChangeData.Click += new System.EventHandler(this.ChangeData_Click);
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(254, 12);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(834, 435);
            this.chart1.TabIndex = 2;
            this.chart1.Text = "chart1";
            // 
            // timer1
            // 
            this.timer1.Interval = 50;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Interval = 500;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button1.Location = new System.Drawing.Point(52, 215);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(150, 56);
            this.button1.TabIndex = 3;
            this.button1.Text = "生成数据";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button2.Location = new System.Drawing.Point(52, 290);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(150, 61);
            this.button2.TabIndex = 4;
            this.button2.Text = "保存数据";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // OpenThreadBtn
            // 
            this.OpenThreadBtn.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.OpenThreadBtn.Location = new System.Drawing.Point(52, 375);
            this.OpenThreadBtn.Name = "OpenThreadBtn";
            this.OpenThreadBtn.Size = new System.Drawing.Size(150, 55);
            this.OpenThreadBtn.TabIndex = 5;
            this.OpenThreadBtn.Text = "继续线程";
            this.OpenThreadBtn.UseVisualStyleBackColor = true;
            this.OpenThreadBtn.Click += new System.EventHandler(this.OpenThreadBtn_Click);
            // 
            // stopThread_btn
            // 
            this.stopThread_btn.Font = new System.Drawing.Font("宋体", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.stopThread_btn.Location = new System.Drawing.Point(52, 454);
            this.stopThread_btn.Name = "stopThread_btn";
            this.stopThread_btn.Size = new System.Drawing.Size(150, 52);
            this.stopThread_btn.TabIndex = 6;
            this.stopThread_btn.Text = "暂停线程";
            this.stopThread_btn.UseVisualStyleBackColor = true;
            this.stopThread_btn.Click += new System.EventHandler(this.stopThread_btn_Click);
            // 
            // CSV_read
            // 
            this.CSV_read.Location = new System.Drawing.Point(52, 512);
            this.CSV_read.Name = "CSV_read";
            this.CSV_read.Size = new System.Drawing.Size(150, 58);
            this.CSV_read.TabIndex = 7;
            this.CSV_read.Text = "CSV_read_btn";
            this.CSV_read.UseVisualStyleBackColor = true;
            this.CSV_read.Click += new System.EventHandler(this.CSV_read_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(1114, 17);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 27;
            this.dataGridView1.Size = new System.Drawing.Size(578, 265);
            this.dataGridView1.TabIndex = 8;
            // 
            // chart2
            // 
            chartArea2.Name = "ChartArea1";
            this.chart2.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.chart2.Legends.Add(legend2);
            this.chart2.Location = new System.Drawing.Point(1114, 314);
            this.chart2.Name = "chart2";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chart2.Series.Add(series2);
            this.chart2.Size = new System.Drawing.Size(578, 266);
            this.chart2.TabIndex = 9;
            this.chart2.Text = "chart2";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(254, 469);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(131, 58);
            this.button3.TabIndex = 10;
            this.button3.Text = "添加数据点1";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(404, 469);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(124, 58);
            this.button4.TabIndex = 11;
            this.button4.Text = "添加数据点2";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // sql_insert
            // 
            this.sql_insert.Location = new System.Drawing.Point(563, 537);
            this.sql_insert.Name = "sql_insert";
            this.sql_insert.Size = new System.Drawing.Size(111, 67);
            this.sql_insert.TabIndex = 12;
            this.sql_insert.Text = "数据库插入";
            this.sql_insert.UseVisualStyleBackColor = true;
            this.sql_insert.Click += new System.EventHandler(this.sql_insert_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(699, 560);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(56, 25);
            this.textBox1.TabIndex = 13;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(761, 560);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(50, 25);
            this.textBox2.TabIndex = 14;
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(817, 560);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(55, 25);
            this.textBox3.TabIndex = 15;
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(878, 560);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(69, 25);
            this.textBox4.TabIndex = 16;
            // 
            // SerialTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1813, 683);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.sql_insert);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.chart2);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.CSV_read);
            this.Controls.Add(this.stopThread_btn);
            this.Controls.Add(this.OpenThreadBtn);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.ChangeData);
            this.Controls.Add(this.openSerial);
            this.Name = "SerialTest";
            this.Text = "SerialTest";
            this.Load += new System.EventHandler(this.SerialTest_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openSerial;
        private System.Windows.Forms.Button ChangeData;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.IO.Ports.SerialPort serialPort1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button OpenThreadBtn;
        private System.Windows.Forms.Button stopThread_btn;
        private System.Windows.Forms.Button CSV_read;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button sql_insert;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox4;
    }
}