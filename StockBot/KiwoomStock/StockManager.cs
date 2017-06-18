using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace StockBot.KiwoomStock
{
    //---------------------------------------------------------------------
    // 주식 매니져, 주식 데이터나 주문 등을 담당하는 클래스 입니다. 
    class StockManager : SingleTon<StockManager>
    {
        public int accountMoney_ { get; set; }

        public void start()
        {
            this.reloadAccountInfo();
        }

        public void process()
        {
            Thread.Sleep(1);
        }

        // 외부 (체결등이 왔을때 강제로 계좌 정보를 다시 읽도록 한다)
        public void reloadAccountInfo()
        {
            StockEngine.getInstance.addOrder(new AccountStockStatement());
            StockEngine.getInstance.addOrder(new AccountMoneyStatement());
        }

        //---------------------------------------------------------------------
        // 주식 봇이 갖고 있는 주식들에 대한처리
        private object lockObject_;
        Dictionary<int, StockData> stockPool_;

        StockManager()
        {
            lockObject_ = new object();
            stockPool_ = new Dictionary<int, StockData>();
            accountMoney_ = 0;
        }

        // 주식 추가 
        public void addStockData(StockData stockData)
        {
            int code = stockData.code_;

            lock (lockObject_) {
                StockData oldData = this.stockData(code);
                if (oldData == null) {
                    stockPool_.Add(code, stockData);
                }
                else {
                    oldData.addLevel(stockData.valuation_);
                }
            }
        }

        // 주식 가지고 오기
        public StockData stockData(int code)
        {
            lock (lockObject_) {
                StockData stockData = null;
                if (stockPool_.TryGetValue(code, out stockData)) {
                    return stockData;
                }
                return null;
            }
        }

        // 산 주식 종목의 갯수 (삼성전자 * 10개, LG전자 * 10 개이면, 삼성, LG 해서 2개로 인식합니다)
        // 주식 구입시 내가 가지고 있는 금액에 비래해서 할당해야 하기 때문에 넣은 함수 입니다.
        private int countBuyedStock()
        {
            int count = 0;
            lock (lockObject_) {
                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;

                    if (stockData.isBuyedStock()) {
                        count++;
                    }
                    if (stockData.alreadyOrder_) {
                        count++;
                    }
                }
            }
            return count;
        }

        // 모니터닝할 주식 정리용.
        public void shrink()
        {
            lock (lockObject_) {
                ArrayList deletePool = new ArrayList();

                const int minValuation = 50;       // 가치 평가 50점 이상만 담기
                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;
                    int valuation = stockData.valuation_;

                    if (valuation < minValuation) {
                        deletePool.Add(code);
                    }
                }

                foreach (int deleteKey in deletePool) {
                    stockPool_.Remove(deleteKey);
                }
            }
        }

        //--------------------------------------------------------
        // 사고 파는 처리
        const int MAX_BUYED_STOCK_COUNT = 5;
        const int NONE_COUNT = 0;
        private bool calculateBuyCount(StockData stockData, out int tradingCount, out int tradingPrice)
        {
            tradingCount = NONE_COUNT;
            tradingPrice = NONE_COUNT;

            int count = MAX_BUYED_STOCK_COUNT - this.countBuyedStock();
            if (count <= 0) {
                return false;
            }

            int nowPrice = stockData.nowPrice(PRICE_TYPE.MIN);
            if (nowPrice == 0) {
                return false;
            }
            int buyable = accountMoney_ / count;

            if (buyable < nowPrice) {
                return false;
            }
            tradingCount = buyable / nowPrice;
            tradingCount -= 5;          // 시장가 구입이므로 넉넉하게 설정한다
            if (tradingCount < 0) {
                return false;
            }
            tradingPrice = nowPrice;

            return true;
        }

        // 계정 관련 
        private void addAccountMoney(int money)
        {
            int accountMoney = StockManager.getInstance.accountMoney_;
            accountMoney = accountMoney + money;

            StockManager.getInstance.accountMoney_ = accountMoney;
            Program.stockBotDlg_.updateAccountMoney(accountMoney);
        }

        private void subAccountMoney(int money)
        {
            this.addAccountMoney(-money);
        }

        // 주식을 사자.
        public void buyStock()
        {
            lock (lockObject_) {
                if (accountMoney_ < 0) {
                    return;
                }

                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;

                    if (stockData.alreadyOrder_) {
                        continue;
                    }

                    if (stockData.isBuyedStock()) {
                        continue;
                    }

                    if (stockData.isBuyTime() == false) {
                        continue;
                    }

                    int tradingCount, tradingPrice;
                    if (this.calculateBuyCount(stockData, out tradingCount, out tradingPrice) == false) {
                        continue;
                    }

                    // 실제 구입주문 넣자.
                    int totalMoney = tradingCount * tradingPrice;
                    this.subAccountMoney(totalMoney);
                    
                    StockEngine.getInstance.addOrder(new BuyStock(code, tradingCount, tradingPrice));
                    Logger.getInstance.print(KiwoomCode.Log.주식봇, "{0}:{1} 주식 주문", stockData.name_, stockData.codeString());

                    stockData.alreadyOrder_ = true;
                }
            }
        }

        // 주식을 팔자.
        public void sellStock()
        {
            lock (lockObject_) {
                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;

                    if (stockData.alreadyOrder_) {
                        continue;
                    }

                    if (stockData.isBuyedStock() == false) {
                        continue;
                    }
                    BuyedStockData buyedStockData = (BuyedStockData)stockData;

                    if (buyedStockData.alreadyOrder_) {
                        continue;
                    }

                    if (buyedStockData.isSellTime() == false) {
                        continue;
                    }

                    // 실제 판매 주문 넣기
                    StockEngine.getInstance.addOrder(new SellStock(buyedStockData));
                    int price = buyedStockData.nowPrice(PRICE_TYPE.MIN);
                    int tradingCount = buyedStockData.buyCount_;
                    int totalMoney = price * tradingCount;
                    this.addAccountMoney(totalMoney);

                    Logger.getInstance.print(KiwoomCode.Log.주식봇, "{0}:{1} 주식 판매", buyedStockData.name_, buyedStockData.codeString());
                    buyedStockData.alreadyOrder_ = true;

                }
            }
        }

        //--------------------------------------------------------
        // 주식풀의 모든 주식의 재갱신을 요구한다
        public void loadStockHistory(PRICE_TYPE type = PRICE_TYPE.MIN)
        {
            lock (lockObject_) {
                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;
                    if (!stockData.checkUpdateTime()) {
                        continue;
                    }
                    stockData.setUpdateTime();

                    Logger.getInstance.consolePrint("* 데이터 요청 : {0:S15}:{1:D6} 주식 데이터", stockData.name_, stockData.code_);

                    if (type == PRICE_TYPE.DAY) {
                        StockEngine.getInstance.addOrder(new HistoryTheDaysStock(code));
                        continue;
                    }

                    List<PriceData> data = stockData.priceTable_[(int)PRICE_TYPE.DAY];
                    if (data.Count == 0) {
                        StockEngine.getInstance.addOrder(new HistoryTheDaysStock(code));
                    }
                    //StockEngine.getInstance.addOrder(new HistoryTheTicksStock(code));
                    StockEngine.getInstance.addOrder(new HistoryTheMinsStock(code));
                }
            }
        }

        //--------------------------------------------------------
        // watching을 고르기 위한 함수
        public bool allStockLoadComplete()
        {
            lock (lockObject_) {
                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;
                    if (stockData.tableDataCount() == 0) {
                        return false;
                    }
                }
            }
            Logger.getInstance.consolePrint("* 주식 풀내의 모든 주식 데이터 일일 / 분봉 차트 로딩 완료");
            return true;
        }

        // 주식 리스트를 확정 합니다.
        public void selectWatchingStock()
        {
            lock (lockObject_) {
                ArrayList deleteData = new ArrayList();

                foreach (KeyValuePair<int, StockData> keyValue in stockPool_) {
                    int code = keyValue.Key;
                    StockData stockData = keyValue.Value;
                    if (stockData.isWathing() == false) {
                        deleteData.Add(code);
                    }
                }

                foreach (int code in deleteData) {
                    stockPool_.Remove(code);
                }
            }
        }
    }
}