using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBot
{
    abstract class Calculater
    {
        public abstract void calculate(ref List<PriceData> priceTable);
    }

    class AvgCalculater : Calculater
    {
        public override void calculate(ref List<PriceData> priceTable)
        {
            int maxIndex = priceTable.Count;
            int[] avgMax = { 5, 10, 20, 50, 100, 200 };  //평균 기준
            int avgIndex = 0;

            foreach (int avgTime in avgMax) {
                // ***단순 macd 구하기
                for (int index = 0; index < maxIndex; ++index) {
                    int sumIndexMax = index + avgTime;
                    if (sumIndexMax > maxIndex) {
                        break;
                    }
                    double sum = 0;
                    for (int sumIndex = index; sumIndex < sumIndexMax; ++sumIndex) {
                        sum += priceTable[sumIndex].price_;
                    }
                    PriceData priceData = priceTable[index];
                    if (avgTime == 0.0f) {
                        continue;
                    }
                    priceData.calc_[(int)EVALUATION_DATA.SMA_START + avgIndex] = sum / (double)avgTime;
                }

                // *** 지수 macd구하기
                // EMA(지수이동평균) = 전일지수이동평균 +{c×(금일종가지수-전일지수이동평균)}
                // ※ 단, 0 < c < 1(9일의 경우 0.2, 12일의 경우 0.15, 26일의 경우엔 가중치 0.075 사용)
                //   c = 2 / (n + 1)
                if ((avgMax[avgIndex] + 1) == 0) {
                    continue;
                }
                double multiplier = 2.0f / (double)(avgMax[avgIndex] + 1);
                multiplier = Math.Min(0.999999f, Math.Max(multiplier, 0.000001f));

                int startIdx = maxIndex - avgMax[(int)avgIndex];
                startIdx--;
                double beforeEma = 0.0f;
                for (int index = startIdx; index >= 0; --index) {
                    double ema = 0.0f;
                    if (index == startIdx) {
                        ema = priceTable[index].calc_[(int)EVALUATION_DATA.SMA_START + avgIndex];
                    }
                    else {
                        double close = priceTable[index].price_;
                        ema = multiplier * (close - beforeEma) + beforeEma;
                    }
                    PriceData priceData = priceTable[index];
                    priceData.calc_[(int)EVALUATION_DATA.EMA_START + avgIndex] = ema;

                    beforeEma = ema;
                }
                ++avgIndex;
            }
        }
    }

    class BollingerCalculater : Calculater
    {
        public override void calculate(ref List<PriceData> priceTable)
        {
            int maxIndex = priceTable.Count;
            int[] avgMax = { 5, 10, 20, 50, 100, 200 };  //평균 기준

            for (int index = 0; index < maxIndex; ++index) {
                int sumIndexMax = index + avgMax[(int)AVG_SAMPLEING.AVG_MAX - 1]; //120 이전 부터 그리기
                if (sumIndexMax > maxIndex) {
                    break;
                }
                int BOLLIGER_TERM = 20;
                double mean = 0.0f;
                for (int day = index; day < BOLLIGER_TERM + index; ++day) {
                    mean += priceTable[day].price_;
                }
                mean = mean / BOLLIGER_TERM;

                double sumDeviation = 0.0f;
                for (int day = index; day < BOLLIGER_TERM + index; ++day) {
                    sumDeviation = sumDeviation + (priceTable[day].price_ - mean) * (priceTable[day].price_ - mean);
                }
                sumDeviation = Math.Sqrt(sumDeviation / BOLLIGER_TERM);
                double BOLLIGER_CONSTANT = 2.0f;

                PriceData priceData = priceTable[index];
                double lower = mean - (sumDeviation * BOLLIGER_CONSTANT);
                double upper = mean + (sumDeviation * BOLLIGER_CONSTANT);

                priceData.calc_[(int)EVALUATION_DATA.BOLLINGER_LOWER] = lower;
                priceData.calc_[(int)EVALUATION_DATA.BOLLINGER_CENTER] = (double)(upper + lower) / 2;
                priceData.calc_[(int)EVALUATION_DATA.BOLLINGER_UPPER] = upper;
            }
        }
    }

    class MacdCalculater : Calculater
    {
        //http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_average_convergence_divergence_macd
        private double average(List<PriceData> priceTable, int startIndex, int count)
        {
            double sum = 0.0f;
            int maxIndex = startIndex - count;
            if (maxIndex < 0) {
                return sum;
            }
            if (startIndex < 0) {
                return sum;
            }

            for (int index = startIndex; index > maxIndex; --index) {
                sum += priceTable[index].price_;
            }
            if (count == 0) {
                return 0;
            }
            return sum / count;
        }

        private double macdSum(List<PriceData> priceTable, int startIndex, int count)
        {
            double sum = 0.0f;
            int maxIdx = Math.Min(startIndex + count - 1, priceTable.Count);
            for (int idx = maxIdx; idx >= startIndex; --idx) {
                sum += priceTable[idx].calc_[(int)EVALUATION_DATA.MACD];
            }
            return sum;
        }

        public override void calculate(ref List<PriceData> priceTable)
        {
            // MACD 설명 사이트
            // http://investexcel.net/how-to-calculate-macd-in-excel/
            // 공식이 좀 만만치 않군...
            int[] MACD_DAY = { 12, 26, 9 }; //중기 투자
            Hashtable ema12day = new Hashtable();
            Hashtable ema26day = new Hashtable();

            //계산하는 가장 옛날 시간을 잡아야 함.
            int CALC_SIZE = 0;
            for (int i = priceTable.Count - 1; i >= 0; --i) {
                if (priceTable[i].price_ != 0) {
                    CALC_SIZE = i;
                    break;
                }
            }

            //-----------------------------------------------------//
            // macd 12라인 계산
            int days = MACD_DAY[0];
            int startday = CALC_SIZE;
            int startIdx = startday - days;
            if (startIdx < 1) {
                Logger.getInstance.consolePrint("! macd 초기화 문제 샘플링 자체 없음");
                return;
            }

            // 첫 스타트 지점 계산
            ema12day[startIdx + 1] = this.average(priceTable, startday, days);

            // 이를 기반으로 후기 내용 계산
            for (int i = startIdx; i >= 0; --i) {
                PriceData priceDataModify = priceTable[i];
                ema12day[i] = (((double)priceDataModify.price_ * (double)(2.0f / (double)(days + 1))) + ((double)ema12day[i + 1] * (1.0f - ((double)(2.0f / (double)(days + 1))))));
            }

            //-----------------------------------------------------//
            // macd 26라인, macd 값 계산
            days = MACD_DAY[1];
            startIdx = startday - MACD_DAY[1] + 1;
            int macdStartIdx = startIdx;
            if (startIdx < 1) {
                Logger.getInstance.consolePrint("! macd 초기화 문제, 문제 있음");
                return;
            }

            // 첫 스타트 지점 계산
            ema26day[startIdx] = this.average(priceTable, startday, days);
            PriceData priceData = priceTable[startIdx];
            priceData.calc_[(int)EVALUATION_DATA.MACD] = (double)ema12day[startIdx] - (double)ema26day[startIdx];

            // 이를 기반으로 후기 내용 계산
            startIdx--;
            for (int i = startIdx; i >= 0; --i) {
                PriceData priceDataModify = priceTable[i];
                ema26day[i] = (((double)priceDataModify.price_ * (double)(2.0f / (double)(days + 1))) + (((double)ema26day[i + 1] * (1.0f - (double)(2.0f / (double)(days + 1))))));
                priceDataModify.calc_[(int)EVALUATION_DATA.MACD] = (double)ema12day[i] - (double)ema26day[i];
            }

            //-----------------------------------------------------//
            // macd 9라인 계산
            days = MACD_DAY[2];
            startIdx = macdStartIdx - days + 1;
            if (startIdx < 1) {
                Logger.getInstance.consolePrint("! macd 초기화 문제, macd9 초기화 에러");
                return;
            }
            priceData = priceTable[startIdx];
            priceData.calc_[(int)EVALUATION_DATA.MACD_SIGNAL] = this.macdSum(priceTable, startIdx, days) / days;
            priceData.calc_[(int)EVALUATION_DATA.MACD_OSCIL] = priceData.calc_[(int)EVALUATION_DATA.MACD] - priceData.calc_[(int)EVALUATION_DATA.MACD_SIGNAL];

            startIdx--;
            for (int i = startIdx; i >= 0; --i) {
                PriceData priceDataModify = priceTable[i];
                PriceData priceDataNext = priceTable[i + 1];

                priceDataModify.calc_[(int)EVALUATION_DATA.MACD_SIGNAL] = (priceDataModify.calc_[(int)EVALUATION_DATA.MACD] * (double)(2.0f / (double)(days + 1)) + priceDataNext.calc_[(int)EVALUATION_DATA.MACD_SIGNAL] * (double)(1 - (double)(2.0f / (double)(days + 1))));
                priceDataModify.calc_[(int)EVALUATION_DATA.MACD_OSCIL] = priceDataModify.calc_[(int)EVALUATION_DATA.MACD] - priceDataModify.calc_[(int)EVALUATION_DATA.MACD_SIGNAL];
            }
        }
    }
}
