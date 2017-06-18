using System;
using System.Collections.Generic;

namespace StockBot
{
    enum StockDataValuation
    {
        HAVE_STOCK = 10000,      // 가진 주식
        LEVEL_1 = 50,           // 안전하게 오를거 같은 주식
        LEVEL_2 = 40,           // 빨리 오르는 주식 
        LEVEL_3 = 30,           // 모니터닝... 
    }

    class StockData : ICloneable
    {
        // stockData 갱신 락
        public object dataLock_ { get; set; }

        //-------------------------------------------------------------
        public int code_ { get; set; }          // 주식 코드
        public int valuation_ { get; set; }     // 중요도
        public string name_ { get; }

        public void addLevel(int level)
        {
            valuation_ += level;
        }

        public string codeString()
        {
            return String.Format("{0:D6}", code_);
        }

        public virtual bool isBuyedStock()
        {
            return false;
        }

        public bool alreadyOrder_ { get; set; }

        public List<PriceData>[] priceTable_;
        public List<PriceData> priceTable(PRICE_TYPE type)
        {
            lock (dataLock_) {
                List<PriceData> priceTable = priceTable_[(int)type];
                if (priceTable.Count == 0) {
                    return null;
                }
                return priceTable;
            }
        }

        //-------------------------------------------------------------
        STOCK_EVALUATION[] evalTotal_;     //총 평가
        STOCK_EVALUATION[] evalAvg_;       //평균지수 평가
        STOCK_EVALUATION[] evalTech_;      //기술 평가
        public STOCK_EVALUATION evalTotal(PRICE_TYPE type)
        {
            lock (dataLock_) {
                return evalTotal_[(int)type];
            }
        }
        public STOCK_EVALUATION evalAvg(PRICE_TYPE type)
        {
            lock (dataLock_) {
                return evalAvg_[(int)type];
            }
        }
        public STOCK_EVALUATION evalTech(PRICE_TYPE type)
        {
            lock (dataLock_) {
                return evalTech_[(int)type];
            }
        }

        //-------------------------------------------------------------
        // 기본 데이터 생성, 주식코드, 이름, 주식 가치 (리스트 업 할때, 쳐내는 기준)
        public StockData(int code, string name, StockDataValuation value)
        {
            code_ = code;
            name_ = name;
            valuation_ = (int)value;

            priceTable_ = new List<PriceData>[(int)PRICE_TYPE.MAX];
            evalTotal_ = new STOCK_EVALUATION[(int)PRICE_TYPE.MAX];
            evalAvg_ = new STOCK_EVALUATION[(int)PRICE_TYPE.MAX];
            evalTech_ = new STOCK_EVALUATION[(int)PRICE_TYPE.MAX];

            for (int i = 0; i < (int)PRICE_TYPE.MAX; ++i) {
                priceTable_[i] = new List<PriceData>();
            }

            alreadyOrder_ = false;
            dataLock_ = new object();
        }

        //-------------------------------------------------------------
        // 복사 메소드
        protected void copyPriceDatas(StockData clone)
        {
            for (int type = 0; type < priceTable_.Length; ++type) {
                for (int index = 0; index < priceTable_[type].Count; ++index) {
                    clone.updatePrice((PRICE_TYPE)type, index, priceTable_[type][index]);
                }
            }

            evalTotal_.CopyTo(clone.evalTotal_, 0);
            evalAvg_.CopyTo(clone.evalAvg_, 0);
            evalTech_.CopyTo(clone.evalTech_, 0);
        }

        public virtual Object Clone()
        {
            StockData clone = new StockData(code_, name_, (StockDataValuation)valuation_);
            this.copyPriceDatas(clone);
            return clone;
        }

        public int tableDataCount()
        {
            int count = 0;
            for (int i = 0; i < (int)PRICE_TYPE.MAX; ++i) {
                count += priceTable_[i].Count;
            }
            return count;
        }

        public int nowPrice(PRICE_TYPE type)
        {
            List<PriceData> priceTable = this.priceTable(type);
            if (priceTable == null) {
                return 0;
            }
            return priceTable[0].price_;
        }

        //-------------------------------------------------------------
        // 업데이트 시간 체크 (30초에 1번씩 갱신하도록 한다)
        protected DateTime updateTime_ = new DateTime(0);
        public bool checkUpdateTime()
        {
            if (DateTime.Now.Ticks > updateTime_.Ticks + (TimeSpan.TicksPerMinute / 2)) {
                return true;
            }
            return false;
        }

        public void setUpdateTime()
        {
            updateTime_ = DateTime.Now;
        }

        //-------------------------------------------------------------
        //가격표관련 처리
        public void clearPrice(PRICE_TYPE type)
        {
            priceTable_[(int)type].Clear();
        }
        public void updatePrice(PRICE_TYPE type, int index, PriceData priceData)
        {
            priceTable_[(int)type].Insert(index, priceData);
        }

        public void calculatePrice(PRICE_TYPE type)
        {
            List<PriceData> priceTable = this.priceTable(type);
            if (priceTable == null) {
                return;
            }
            Calculater calculater;
            calculater = new AvgCalculater();
            calculater.calculate(ref priceTable);

            calculater = new BollingerCalculater();
            calculater.calculate(ref priceTable);

            calculater = new MacdCalculater();
            calculater.calculate(ref priceTable);
                        
            Evaluation stockAnalysis = new Evaluation();
            evalTotal_[(int)type] = stockAnalysis.analysis(priceTable);
            evalAvg_[(int)type] = stockAnalysis.evalAvg();
            evalTech_[(int)type] = stockAnalysis.evalTech();                      
        }

        //-------------------------------------------------------------
        // 가격 정상 판단 처리
        public bool isWathing()
        {
            //일이 강력 매수인 것을 상대로.
            // 최대 3만원이하 주식을 대상
            int maxPrice = 30000;
            if (this.nowPrice(PRICE_TYPE.MIN) > maxPrice) {
                return false;
            }

            STOCK_EVALUATION evaluation = this.evalTotal(PRICE_TYPE.DAY);
            if (evaluation == STOCK_EVALUATION.매수) {
                return true;
            }
            return false;
        }

        //-------------------------------------------------------------
        // 테스트로 볼린저 그래프로 판단
        public bool isBuyTime()
        {
            STOCK_EVALUATION evaluation = this.evalTotal(PRICE_TYPE.MIN);
            if (evaluation == STOCK_EVALUATION.매수) {
                return true;
            }
            return false;
        }
    }

    //-------------------------------------------------------------
    // 구입한 주식에 대한 데이터는 내가 얼마에 몇개를 가지고 있는지도 알고 있어야 합니다.
    class BuyedStockData : StockData
    {
        public int buyCount_ { get; set; }
        public int buyPrice_ { get; set; }

        public BuyedStockData(int code, string name, int buyCount, int buyPrice) : base(code, name, StockDataValuation.HAVE_STOCK)
        {
            buyCount_ = buyCount;
            buyPrice_ = buyPrice;
        }

        public override Object Clone()
        {
            BuyedStockData clone = new BuyedStockData(code_, name_, buyCount_, buyPrice_);
            this.copyPriceDatas(clone);
            return clone;
        }

        public override Boolean isBuyedStock()
        {
            return true;
        }

        public int totalBuyPrice()
        {
            return buyCount_ * buyPrice_;
        }

        public int profitPrice()
        {
            int nowValue = this.nowPrice(PRICE_TYPE.MIN) * buyCount_;
            int myValue = totalBuyPrice();
            return nowValue - myValue;
        }

        public double profitPriceRate()
        {
            int now = this.nowPrice(PRICE_TYPE.MIN);
            if (now == 0) {
                return 0;
            }
            double nowPrice = (double)now;
            double profitRate = (nowPrice - (double)buyPrice_) / nowPrice * 100;
            return profitRate;
        }

        public bool isSellTime()
        {
            STOCK_EVALUATION evaluation = this.evalTotal(PRICE_TYPE.DAY);
            if (evaluation == STOCK_EVALUATION.매도) {
                Logger.getInstance.print(KiwoomCode.Log.주식봇, "{0}, {1} 기술 지표적 매도 도달", name_, this.codeString());
                return true;
            }

            double profitRate = this.profitPriceRate();
            if (profitRate > 3.0f /*이익 초과값*/) {
            Logger.getInstance.print(KiwoomCode.Log.주식봇, "{0}, {1} 이익율 초과 매도", name_, this.codeString());
                return true;
            }

            if (profitRate < -5.0f /*손절매율*/) {
            Logger.getInstance.print(KiwoomCode.Log.주식봇, "{0}, {1} 손절매 매도", name_, this.codeString());
                return true;
            }

            return false;
        }
    }
}
