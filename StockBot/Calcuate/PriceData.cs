using System;

namespace StockBot
{
    enum PRICE_TYPE
    {
        TICK,           // tick 가격표
        MIN,            // 분 가격표
        DAY,            // 일 가격표
        MAX,
    };

    // 평균 샘플링
    enum AVG_SAMPLEING
    {
        AVG_5,                 //최근에서 5번
        AVG_10,                //10번 ...
        AVG_20,
        AVG_50,
        AVG_100,
        AVG_200,

        AVG_MAX,
    };

    // 평가 데이터
    enum EVALUATION_DATA
    {
        SMA_5,              //단순 이동평균
        SMA_10,
        SMA_20,
        SMA_50,
        SMA_100,
        SMA_200,

        EMA_5,              //지수 이동평균
        EMA_10,
        EMA_20,
        EMA_50,
        EMA_100,
        EMA_200,

        BOLLINGER_UPPER,    //볼린저
        BOLLINGER_CENTER,   //중심선
        BOLLINGER_LOWER,

        MACD,               //MACD
        MACD_SIGNAL,
        MACD_OSCIL,

        MAX,

        SMA_START = SMA_5,
        EMA_START = EMA_5,
        AVG_END = EMA_200 + 1,
    }

    struct PriceData : ICloneable
    {
        public string date_;

        public int price_;              //현재가
        public int startPrice_;         //시가
        public int highPrice_;          //고가
        public int lowPrice_;           //저가
        public double[] calc_;

        public PriceData(string date, int price, int startPrice, int highPrice, int lowPrice)
        {
            date_ = date;

            price_ = Math.Abs(price);
            startPrice_ = Math.Abs(startPrice);
            highPrice_ = Math.Abs(highPrice);
            lowPrice_ = Math.Abs(lowPrice);

            calc_ = new double[(int)EVALUATION_DATA.MAX];
        }

        public Object Clone()
        {
            PriceData clone = new PriceData(date_, price_, startPrice_, highPrice_, lowPrice_);

            calc_.CopyTo(clone.calc_, 0);

            return clone;
        }
    };

}
