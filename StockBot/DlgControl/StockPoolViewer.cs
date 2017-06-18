using StockBot.KiwoomStock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StockBot.DlgControl
{
    class StockPoolViewer : SingleTon<StockPoolViewer>
    {
        private DataGridView dataGridView_;
        private Dictionary<int, int> viewData_;                        // 보여지는 가격 데이터
        private readonly PRICE_TYPE priceType_ = PRICE_TYPE.DAY;       // 재갱신 여부 판단용

        StockPoolViewer()
        {
            dataGridView_ = Program.stockBotDlg_.dataGridView_Stock();
            viewData_ = new Dictionary<int, int>();
            this.setup();
        }

        private void initScreen()
        {
            int i = 0;
            string[] columnName = { "구분", "중요도", "index", "종목코드", "종목명", "현재가", "보유수량", "주당 매입가", "총 매입가", "이익", "이익율" };

            dataGridView_.ColumnCount = columnName.Length;
            foreach (string name in columnName) {
                dataGridView_.Columns[i++].Name = name;
            }
            dataGridView_.ReadOnly = true;
            dataGridView_.Show();
        }

        public void setup()
        {
            if (dataGridView_.InvokeRequired) {
                dataGridView_.BeginInvoke(new Action(() => this.initScreen()));
            }
            else {
                this.initScreen();
            }
            dataGridView_.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
        }

        //---- 각 데이터가 달라야 갱신해주도록 한다.
        private void setViewData(Dictionary<int, StockData> stockPool)
        {
            viewData_.Clear();
            foreach (KeyValuePair<int, StockData> keyValue in stockPool) {
                int code = keyValue.Key;
                StockData stockData = keyValue.Value;

                viewData_.Add(code, stockData.nowPrice(priceType_));
            }
        }

        private bool needSyncViewData(Dictionary<int, StockData> stockPool)
        {
            if (stockPool.Count != viewData_.Count) {
                return true;
            }

            foreach (KeyValuePair<int, StockData> keyValue in stockPool) {
                int code = keyValue.Key;
                StockData stockData = keyValue.Value;
                int nowPrice = stockData.nowPrice(priceType_);

                if (nowPrice == 0) {
                    return true;
                }

                if (viewData_[code] != nowPrice) {
                    return true;
                }                
            }
            return false;
        }

        private void printList(Dictionary<int, StockData> stockPool)
        {
            dataGridView_.Rows.Clear();
            const int INIT_INDEX = 1;
            int index = INIT_INDEX;

            var rankDesc = stockPool.OrderByDescending(num => num.Value.valuation_);

            foreach (KeyValuePair<int, StockData> keyValue in rankDesc) {
                int code = keyValue.Key;
                StockData orgData = keyValue.Value;

                StockData stockData = (StockData)orgData.Clone();
                //정렬을 위해...
                string column = "2_감시";
                if (stockData.isBuyedStock()) {
                    column = "1_보유";
                }
                string valuation = String.Format("{0:D4}", stockData.valuation_);
                string codeString = stockData.codeString();
                string name = stockData.name_;
                string nowPrice = stockData.nowPrice(priceType_).ToString();

                string buyCount = "";
                string buyPrice = "";
                string totalBuyPrice = "";
                string profitPrice = "";
                string profitPriceRate = "";

                if (stockData.isBuyedStock()) {
                    BuyedStockData buyedStockData = (BuyedStockData)stockData;
                    buyCount = buyedStockData.buyCount_.ToString();
                    buyPrice = buyedStockData.buyPrice_.ToString();
                    totalBuyPrice = buyedStockData.totalBuyPrice().ToString();
                    profitPrice = buyedStockData.profitPrice().ToString();
                    profitPriceRate = buyedStockData.profitPriceRate().ToString();
                }
                string indexStr = String.Format("{0:D3}", index);
                string[] row = new string[] { column, valuation, indexStr, codeString, name, nowPrice, buyCount, buyPrice, totalBuyPrice, profitPrice, profitPriceRate };
                dataGridView_.Rows.Add(row);
                index++;
            }
            dataGridView_.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            //첫열을 기준으로 정렬을 시켜서 구입한 주식을 위로 올린다.
            dataGridView_.Sort(dataGridView_.Columns[0], System.ComponentModel.ListSortDirection.Ascending);
        }

        private void consolePrintList(Dictionary<int, StockData> stockPool)
        {
            Logger.getInstance.consolePrint("=============================================================");
            foreach (KeyValuePair<int, StockData> keyValue in stockPool) {
                int code = keyValue.Key;
                StockData orgData = keyValue.Value;

                StockData stockData = (StockData)orgData.Clone();
                //정렬을 위해...
                string column = "2_감시";
                if (stockData.isBuyedStock()) {
                    column = "1_보유";
                }

                string codeString = stockData.codeString();
                string name = stockData.name_;
                string nowPrice = stockData.nowPrice(priceType_).ToString();

                string buyCount = "";
                string buyPrice = "";
                string totalBuyPrice = "";
                string profitPrice = "";
                string profitPriceRate = "";

                if (stockData.isBuyedStock()) {
                    BuyedStockData buyedStockData = (BuyedStockData)stockData;
                    buyCount = buyedStockData.buyCount_.ToString();
                    buyPrice = buyedStockData.buyPrice_.ToString();
                    totalBuyPrice = buyedStockData.totalBuyPrice().ToString();
                    profitPrice = buyedStockData.profitPrice().ToString();
                    profitPriceRate = buyedStockData.profitPriceRate().ToString();
                }

                Logger.getInstance.consolePrint("{0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8}",
                    column, codeString, name, nowPrice, buyCount, buyPrice, totalBuyPrice, profitPrice, profitPriceRate);
            }
        }

        // 주식 정보를 출력합니다.
        public void print(Dictionary<int, StockData> stockPool)
        {
            if (!this.needSyncViewData(stockPool)) {
                return;
            }
            this.setViewData(stockPool);
            
            //콘솔로 프린트 하고 싶으면 주석 푼다
            //consolePrintList(stockPool);

            if (dataGridView_.InvokeRequired) {
                dataGridView_.BeginInvoke(new Action(() => this.printList(stockPool)));
            }
            else {
                this.printList(stockPool);
            }
        }

        // 셀을 선택했을때 처리
        public void cellClick(Object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0
                || e.RowIndex >= (dataGridView_.RowCount - 1)) {
                return;
            }
            int stockCode = int.Parse(dataGridView_.Rows[e.RowIndex].Cells[3].Value.ToString());

            StockData stockData = StockManager.getInstance.stockData(stockCode);
            if (stockData == null) {
                return;
            }

            //TODO : 주식 선택시 다른 정보 표시를 하고 싶으면 이 이하에 추가 한다.
        }
    }
}
