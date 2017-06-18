using System;
using System.Collections.Generic;

namespace StockBot
{
    enum EVALUATION_ITEM
    {
        // SMA / EMA의 경우 위의 AVG_SAMPLEING이랑 같이 수정되어야 한다.
        SMA_5,
        SMA_10,
        SMA_20,
        SMA_50,
        SMA_100,
        SMA_200,

        EMA_5,
        EMA_10,
        EMA_20,
        EMA_50,
        EMA_100,
        EMA_200,

        BOLLINGER,
        MACD,

        MAX,

        SMA_START = SMA_5,
        EMA_START = EMA_5,
        AVG_END = EMA_200 + 1,
    }

    enum STOCK_EVALUATION
    {
        중립,
        매도,
        매수,

        MAX,
    };

    //가격 평가
    class Evaluation : ICloneable
    {
        private STOCK_EVALUATION[] item_ = new STOCK_EVALUATION[(int)EVALUATION_ITEM.MAX];

        public Object Clone()
        {
            Evaluation clone = new Evaluation();
            item_.CopyTo(clone.item_, 0);
            return clone;
        }

        public STOCK_EVALUATION[] getItems()
        {
            return item_;
        }

        private int[] avgEvalCount_ = new int[(int)STOCK_EVALUATION.MAX];          // 평균지수 평가들 취합
        private int[] techEvalCount_ = new int[(int)STOCK_EVALUATION.MAX];         // 기술 평가들 취합
        private void counting()
        {
            for (int index = 0; index < (int)EVALUATION_ITEM.AVG_END; ++index) {
                STOCK_EVALUATION eval = item_[index];
                avgEvalCount_[(int)eval]++;
            }
            for (int index = (int)EVALUATION_ITEM.AVG_END; index < (int)EVALUATION_ITEM.MAX; ++index) {
                STOCK_EVALUATION eval = item_[index];
                techEvalCount_[(int)eval]++;
            }
        }

        public STOCK_EVALUATION evalAvg()
        {
            STOCK_EVALUATION result = STOCK_EVALUATION.중립;
            int nowEvalIdx = 0;

            for (int index = 0; index < (int)STOCK_EVALUATION.MAX; ++index) {
                if (nowEvalIdx < avgEvalCount_[index]) {
                    nowEvalIdx = avgEvalCount_[index];
                    result = (STOCK_EVALUATION)index;
                }
            }

            return result;
        }

        public STOCK_EVALUATION evalTech()
        {
            STOCK_EVALUATION result = STOCK_EVALUATION.중립;
            int nowEvalIdx = 0;

            for (int index = 0; index < (int)STOCK_EVALUATION.MAX; ++index) {
                if (nowEvalIdx < techEvalCount_[index]) {
                    nowEvalIdx = techEvalCount_[index];
                    result = (STOCK_EVALUATION)index;
                }
            }

            return result;
        }

        public STOCK_EVALUATION analysis(List<PriceData> priceTable)
        {
            this.analysisProcess(priceTable);
            this.counting();

            const int AVG_EVAL = 0;
            const int TECH_EVAL = 1;
            STOCK_EVALUATION[] eval = new STOCK_EVALUATION[2];
            eval[AVG_EVAL] = this.evalAvg();
            eval[TECH_EVAL] = this.evalTech();

            if (eval[AVG_EVAL] == eval[TECH_EVAL]) {
                if (eval[AVG_EVAL] == STOCK_EVALUATION.매도) {
                    return STOCK_EVALUATION.매도;
                }
                else if (eval[AVG_EVAL] == STOCK_EVALUATION.매수) {
                    return STOCK_EVALUATION.매수;
                }
            }
            else {
                if (eval[AVG_EVAL] == STOCK_EVALUATION.매도 || eval[TECH_EVAL] == STOCK_EVALUATION.매도) {
                    return STOCK_EVALUATION.매도;
                }
                else if (eval[AVG_EVAL] == STOCK_EVALUATION.매수 || eval[TECH_EVAL] == STOCK_EVALUATION.매수) {
                    return STOCK_EVALUATION.매수;
                }
            }

            return STOCK_EVALUATION.중립;
        }

        private void analysisProcess(List<PriceData> priceTable)
        {
            this.avgAnalysis(priceTable);
            this.bollingerAnalysis(priceTable);
            this.macdAnalysis(priceTable);            
        }

        const int NOW = 0;
        const int BEFORE = 1;
        private void avgAnalysis(List<PriceData> priceTable)
        {
            // 주식 차트 분석 119 page 참고해야함.
            // 지금은 kr.investing.com 의 것을 참고로 구현 되어 있음.
            // 현재값과 평균값 비교로 판단중...
            const int STAND_VAL = 3;

            double nowPrice = priceTable[NOW].price_;
            double percent = nowPrice * STAND_VAL / 100;

            //   매수 < -3% 중립 +3% < 매도
            for (int index = 0; index < 2; ++index) {
                for (int avgIdx = 0; avgIdx < (int)AVG_SAMPLEING.AVG_MAX; ++avgIdx) {
                    int valIdx = 0;
                    if (index == 0) {
                        valIdx = (int)EVALUATION_ITEM.SMA_START + avgIdx;
                    }
                    else {
                        valIdx = (int)EVALUATION_ITEM.EMA_START + avgIdx;
                    }
                    double calc = priceTable[NOW].calc_[valIdx];

                    //현재값 10000원 일때... 10000 - 300 = 9700 / 10300 기준으로 잡고.
                    //calc 가 밑에 있으면 매수
                    //calc 가 위에 있으면 매도임.
                    if (calc < (nowPrice - percent)) {
                        item_[valIdx] = STOCK_EVALUATION.매수;
                    }
                    else if ((nowPrice + percent) < calc) {
                        item_[valIdx] = STOCK_EVALUATION.매도;
                    }
                    else {
                        item_[valIdx] = STOCK_EVALUATION.중립;
                    }
                }
            }
        }

        private void bollingerAnalysis(List<PriceData> priceTable)
        {
            // 주식차트 분석 256page 참고
            double nowPrice = priceTable[NOW].price_;
            double beforePrice = priceTable[BEFORE].price_;

            double upper = priceTable[NOW].calc_[(int)EVALUATION_DATA.BOLLINGER_UPPER];
            double center = priceTable[NOW].calc_[(int)EVALUATION_DATA.BOLLINGER_CENTER];
            double lower = priceTable[NOW].calc_[(int)EVALUATION_DATA.BOLLINGER_LOWER];

            double nowBandWith = upper - lower;

            int bollingerAnalysisTerm = 30;
            int term = Math.Min(bollingerAnalysisTerm, priceTable.Count - 1 - NOW);

            double oldUpper = priceTable[NOW + term].calc_[(int)EVALUATION_DATA.BOLLINGER_UPPER];
            double oldLower = priceTable[NOW + term].calc_[(int)EVALUATION_DATA.BOLLINGER_LOWER];

            double oldBandWith = oldUpper - oldLower;

            // 30일전과 비교로 밴드폭이 확대 되었으면
            if ((oldBandWith * 1.3f) < nowBandWith) {
                if (upper < nowPrice) {
                    item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매수;
                    return;
                }
                if (beforePrice < center
                    && center < nowPrice) {
                    item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매수;
                    return;
                }
            }
            // 밴드 폭이 줄어 들면
            else if ((oldBandWith * 0.7f) > nowBandWith) {
                if (upper < nowPrice) {
                    item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매수;
                    return;
                }
                if (nowPrice < lower) {
                    item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매도;
                    return;
                }
            }

            // 중앙선 돌파 시 처리
            if (beforePrice < center
                && center < nowPrice) {
                item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매수;
                return;
            }
            if (beforePrice > center
                && center > nowPrice) {
                item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매도;
                return;
            }

            if (nowPrice < lower) {
                item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매수;
                return;
            }

            if (nowPrice > upper) {
                item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.매도;
                return;
            }

            item_[(int)EVALUATION_ITEM.BOLLINGER] = STOCK_EVALUATION.중립;
        }

        private void macdAnalysis(List<PriceData> priceTable)
        {
            //주식 차트 분석 191 page 참고
            double macdOscilNow = priceTable[NOW].calc_[(int)EVALUATION_DATA.MACD_OSCIL];
            double macdOscilBefore = priceTable[BEFORE].calc_[(int)EVALUATION_DATA.MACD_OSCIL];
            double macdOscilBefore2 = priceTable[BEFORE + 1].calc_[(int)EVALUATION_DATA.MACD_OSCIL];

            // macd 가 signal을 상향돌파 했으면
            if (macdOscilBefore < 0 && 0 < macdOscilNow) {
                item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.매수;
                return;
            } // macd 가 signal을 하향돌파 했으면
            if (macdOscilBefore > 0 && 0 > macdOscilNow) {
                item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.매도;
                return;
            }

            if (macdOscilBefore2 > 0
                && macdOscilBefore2 < macdOscilBefore
                && macdOscilBefore < macdOscilNow) {
                item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.매수;
                return;
            }
            if (macdOscilBefore2 > macdOscilBefore
                && macdOscilBefore > macdOscilNow
                && macdOscilNow > 0) {
                item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.중립;
                return;
            }
            if (macdOscilBefore2 < 0
                && macdOscilBefore2 > macdOscilBefore
                && macdOscilBefore > macdOscilNow) {
                item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.매도;
                return;
            }
            
            if (macdOscilBefore2 < macdOscilBefore
                && macdOscilBefore < macdOscilNow
                && macdOscilNow < 0) {
                item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.중립;
                return;
            }

            item_[(int)EVALUATION_ITEM.MACD] = STOCK_EVALUATION.중립;
            return;
        }               
    }
}