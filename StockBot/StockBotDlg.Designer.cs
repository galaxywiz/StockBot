namespace StockBot
{
    partial class StockBotDlg
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StockBotDlg));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.ListView_Log = new System.Windows.Forms.ListView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.axKHOpenAPI = new AxKHOpenAPILib.AxKHOpenAPI();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Label_account = new System.Windows.Forms.Label();
            this.Label_id = new System.Windows.Forms.Label();
            this.Label_money = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.Button_quit = new System.Windows.Forms.Button();
            this.Button_start = new System.Windows.Forms.Button();
            this.DataGridView_StockPool = new System.Windows.Forms.DataGridView();
            this.chart_stock = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chart_macd = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axKHOpenAPI)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView_StockPool)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart_stock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart_macd)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.ListView_Log);
            this.groupBox3.Location = new System.Drawing.Point(223, 13);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox3.Size = new System.Drawing.Size(629, 139);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Log";
            // 
            // ListView_Log
            // 
            this.ListView_Log.FullRowSelect = true;
            this.ListView_Log.GridLines = true;
            this.ListView_Log.HoverSelection = true;
            this.ListView_Log.Location = new System.Drawing.Point(6, 21);
            this.ListView_Log.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ListView_Log.Name = "ListView_Log";
            this.ListView_Log.Size = new System.Drawing.Size(617, 110);
            this.ListView_Log.TabIndex = 0;
            this.ListView_Log.UseCompatibleStateImageBehavior = false;
            this.ListView_Log.View = System.Windows.Forms.View.Details;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.axKHOpenAPI);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.Label_account);
            this.groupBox1.Controls.Add(this.Label_id);
            this.groupBox1.Controls.Add(this.Label_money);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.Button_quit);
            this.groupBox1.Controls.Add(this.Button_start);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(204, 139);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Account";
            // 
            // axKHOpenAPI
            // 
            this.axKHOpenAPI.Enabled = true;
            this.axKHOpenAPI.Location = new System.Drawing.Point(151, 19);
            this.axKHOpenAPI.Name = "axKHOpenAPI";
            this.axKHOpenAPI.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axKHOpenAPI.OcxState")));
            this.axKHOpenAPI.Size = new System.Drawing.Size(47, 33);
            this.axKHOpenAPI.TabIndex = 10;
            this.axKHOpenAPI.Visible = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(16, 12);
            this.label3.TabIndex = 8;
            this.label3.Text = "ID";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 12);
            this.label2.TabIndex = 7;
            this.label2.Text = "계좌 번호";
            // 
            // Label_account
            // 
            this.Label_account.AutoSize = true;
            this.Label_account.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Label_account.Location = new System.Drawing.Point(98, 40);
            this.Label_account.Name = "Label_account";
            this.Label_account.Size = new System.Drawing.Size(47, 14);
            this.Label_account.TabIndex = 6;
            this.Label_account.Text = "          ";
            // 
            // Label_id
            // 
            this.Label_id.AutoSize = true;
            this.Label_id.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Label_id.Location = new System.Drawing.Point(98, 19);
            this.Label_id.Name = "Label_id";
            this.Label_id.Size = new System.Drawing.Size(47, 14);
            this.Label_id.TabIndex = 5;
            this.Label_id.Text = "          ";
            // 
            // Label_money
            // 
            this.Label_money.AutoSize = true;
            this.Label_money.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Label_money.Location = new System.Drawing.Point(98, 62);
            this.Label_money.Name = "Label_money";
            this.Label_money.Size = new System.Drawing.Size(47, 14);
            this.Label_money.TabIndex = 4;
            this.Label_money.Text = "          ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "계좌 가용액";
            // 
            // Button_quit
            // 
            this.Button_quit.Location = new System.Drawing.Point(110, 95);
            this.Button_quit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Button_quit.Name = "Button_quit";
            this.Button_quit.Size = new System.Drawing.Size(88, 22);
            this.Button_quit.TabIndex = 2;
            this.Button_quit.Text = "종료";
            this.Button_quit.UseVisualStyleBackColor = true;
            this.Button_quit.Click += new System.EventHandler(this.Button_quit_Click_1);
            // 
            // Button_start
            // 
            this.Button_start.Location = new System.Drawing.Point(6, 95);
            this.Button_start.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Button_start.Name = "Button_start";
            this.Button_start.Size = new System.Drawing.Size(88, 22);
            this.Button_start.TabIndex = 1;
            this.Button_start.Text = "시작";
            this.Button_start.UseVisualStyleBackColor = true;
            this.Button_start.Click += new System.EventHandler(this.Button_start_Click);
            // 
            // DataGridView_StockPool
            // 
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.DataGridView_StockPool.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.DataGridView_StockPool.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.DataGridView_StockPool.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGridView_StockPool.Location = new System.Drawing.Point(12, 152);
            this.DataGridView_StockPool.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DataGridView_StockPool.Name = "DataGridView_StockPool";
            this.DataGridView_StockPool.ReadOnly = true;
            this.DataGridView_StockPool.RowTemplate.Height = 23;
            this.DataGridView_StockPool.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DataGridView_StockPool.Size = new System.Drawing.Size(840, 168);
            this.DataGridView_StockPool.TabIndex = 4;
            this.DataGridView_StockPool.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView_StockPool_CellClick);
            // 
            // chart_stock
            // 
            chartArea1.Name = "ChartArea1";
            this.chart_stock.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart_stock.Legends.Add(legend1);
            this.chart_stock.Location = new System.Drawing.Point(11, 20);
            this.chart_stock.Name = "chart_stock";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart_stock.Series.Add(series1);
            this.chart_stock.Size = new System.Drawing.Size(821, 205);
            this.chart_stock.TabIndex = 9;
            this.chart_stock.Text = "chart1";
            // 
            // chart_macd
            // 
            chartArea2.Name = "ChartArea1";
            this.chart_macd.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.chart_macd.Legends.Add(legend2);
            this.chart_macd.Location = new System.Drawing.Point(11, 231);
            this.chart_macd.Name = "chart_macd";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.chart_macd.Series.Add(series2);
            this.chart_macd.Size = new System.Drawing.Size(821, 100);
            this.chart_macd.TabIndex = 10;
            this.chart_macd.Text = "chart2";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chart_stock);
            this.groupBox2.Controls.Add(this.chart_macd);
            this.groupBox2.Location = new System.Drawing.Point(13, 327);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(838, 341);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "차트";
            // 
            // StockBotDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 678);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.DataGridView_StockPool);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Name = "StockBotDlg";
            this.Text = "StockBot";
            this.Shown += new System.EventHandler(this.StockBotDlg_Shown);
            this.groupBox3.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axKHOpenAPI)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridView_StockPool)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart_stock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart_macd)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListView ListView_Log;
        private System.Windows.Forms.GroupBox groupBox1;
        private AxKHOpenAPILib.AxKHOpenAPI axKHOpenAPI;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label Label_account;
        private System.Windows.Forms.Label Label_id;
        private System.Windows.Forms.Label Label_money;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Button_quit;
        private System.Windows.Forms.Button Button_start;
        private System.Windows.Forms.DataGridView DataGridView_StockPool;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart_stock;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart_macd;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

