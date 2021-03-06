﻿using StockBot.DlgControl;
using StockBot.KiwoomStock;
using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace StockBot
{
    public partial class StockBotDlg : Form
    {
        public StockBotDlg()
        {
            InitializeComponent();
        }        

        private void setup()
        {
            Button_start.Enabled = false;

            // 컨트롤 넘겨줘서 셋팅하기
            Logger.getInstance.setup(ListView_Log);
            StockEngine.getInstance.setup(this);
        }

        public void quit()
        {
            Logger.getInstance.close();
            StockEngine.getInstance.shutdown();
            StockEngine.getInstance.quit();
            Application.Exit();
        }

        //--------------------------------------------
        // 이벤트 처리
        private void StockBotDlg_Shown(object sender, EventArgs e)
        {
            this.setup();
            StockEngine.getInstance.start();
        }

        private bool started_ = false;
        private void Button_start_Click(object sender, EventArgs e)
        {
            if (this.started_) {
                return;
            }
            Button_start.Enabled = false;
            if (StockEngine.getInstance.start()) {
                StockManager.getInstance.start();
            }
            this.started_ = true;
        }

        private void Button_quit_Click_1(object sender, EventArgs e)
        {
            this.quit();
        }

        public void updateAccountMoney(int money)
        {
            if (Label_money.InvokeRequired) {
                Label_money.BeginInvoke(new Action(() => Label_money.Text = money.ToString()));
            }
            else {
                Label_money.Text = money.ToString();
            }
            StockManager.getInstance.accountMoney_ = money;
        }
        //--------------------------------------------
        // 컨트롤러 getter
        public AxKHOpenAPILib.AxKHOpenAPI openApi()
        {
            return axKHOpenAPI;
        }

        public Label userId()
        {
            return Label_id;
        }

        public Label account()
        {
            return Label_account;
        }

        public Button buttonStart()
        {
            return Button_start;
        }

        public DataGridView dataGridView_Stock()
        {
            return DataGridView_StockPool;
        }

        public Chart chart_Stock()
        {
            return chart_stock;
        }

        public Chart chart_Macd()
        {
            return chart_macd;
        }

        private void DataGridView_StockPool_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            StockPoolViewer.getInstance.cellClick(sender, e);
        }

        public WebBrowser chart_WebBrowser()
        {
            return webBrowser;
        }
    }
}
