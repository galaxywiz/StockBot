using System;
using KiwoomCode;
using StockBot.KiwoomStock;

namespace StockBot
{
    abstract class StockStatement
    {
        // 화면번호 생산
        static private object lockObject_ = new object();
        static private int scrNum_ = 1000;

        protected virtual string getScreenNumber()
        {
            lock (lockObject_) {
                scrNum_++;
            }
            return scrNum_.ToString();
        }

        protected string screenNo_;
        protected string requestName_ { get; set; }
        public long orderTick_ { get; set; }

        public StockStatement()
        {
            orderTick_ = 0;
            screenNo_ = this.getScreenNumber();
        }

        public virtual string getKey()
        {
            return requestName_ + screenNo_;
        }

        //--------------------------------------------------------//
        // 매크로
        protected bool setParam(string columName, string value)
        {
            try {
                StockEngine.getInstance.khOpenApi().SetInputValue(columName, value);
                return true;
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[setParam:{0}, {1}] {2}\n{3}\n{4}", columName, value, execption.Message, execption.StackTrace, execption.InnerException);
            }
            return false;
        }

        protected int getRowCount(AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            try {
                int count = StockEngine.getInstance.khOpenApi().GetRepeatCnt(apiEvent.sTrCode, apiEvent.sRQName);
                return count;
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[getRowCount:{0}] {1}\n{2}\n{3}", apiEvent.sRQName, execption.Message, execption.StackTrace, execption.InnerException);
            }
            return 0;
        }

        protected string getData(string columName, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent, int index)
        {
            try {
                string data = StockEngine.getInstance.khOpenApi().CommGetData(apiEvent.sTrCode, "", apiEvent.sRQName, index, columName).Trim();
                return data;
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[getData:{0}] {1}\n{2}\n{3}", apiEvent.sRQName, execption.Message, execption.StackTrace, execption.InnerException);
            }
            return "";
        }

        //--------------------------------------------------------//
        // 상속받은 전표에서 구현해야 하는 부분

        // 처음 파라메터 입력부
        protected abstract bool setInput();

        // 주식 모듈로 명령을 내리는 부분
        public abstract void request();

        // 주식 모듈로 결과를 받는 부분
        public abstract void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent);
    }

    class OrderStockStatement : StockStatement
    {
        protected string requestCode_
        {
            get;
            set;
        }
        //--------------------------------------------------------//
        // 처리 명령어
        protected override bool setInput()
        {
            return false;
        }

        public override void request()
        {
            orderTick_ = DateTime.Now.Ticks;
            if (this.setInput() == false) {
                return;
            }

            try {
                int nRet = StockEngine.getInstance.khOpenApi().CommRqData(requestName_, requestCode_, 0, screenNo_);

                Log level = Log.API조회;
                if (!Error.IsError(nRet)) {
                    level = Log.에러;
                }
                Logger.getInstance.print(level, "주문 [{0}:{1}]\t\t결과 메시지 : {2}", requestCode_, requestName_, Error.GetErrorMessage());
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[request:{0}] {1}\n{2}\n{3}", this.ToString(), execption.Message, execption.StackTrace, execption.InnerException);
            }
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            Logger.getInstance.print(Log.에러, "{0} 주문 클래스 receive 구현이 안되어 있음", base.ToString());
        }
    }

    //--------------------------------------------------------//
    // 각 주문 명령 클래스들
    // 계좌 관련, 주식 리스트(각 리스트의 내부 데이터말고)만 로드
    class AccountStockStatement : OrderStockStatement
    {
        public AccountStockStatement()
        {
            requestName_ = "계좌수익률요청";
            requestCode_ = "OPT10085";
        }

        protected override bool setInput()
        {
            return this.setParam("계좌번호", StockEngine.accountNumber());
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            int count = this.getRowCount(apiEvent);
            for (int i = 0; i < count; i++) {
                try {
                    int code = int.Parse(this.getData("종목코드", apiEvent, i).Trim());
                    string name = this.getData("종목명", apiEvent, i);
                    int buyCount = int.Parse(this.getData("보유수량", apiEvent, i).Trim());
                    int buyPrice = int.Parse(this.getData("매입가", apiEvent, i).Trim());

                    if (buyCount == 0) {
                        StockData StockData = new StockData(code, name, StockDataValuation.HAVE_STOCK);
                        StockManager.getInstance.addStockData(StockData);
                    }
                    else {
                        BuyedStockData buyedStockData = new BuyedStockData(code, name, buyCount, buyPrice);
                        StockManager.getInstance.addStockData(buyedStockData);
                    }

                    Logger.getInstance.consolePrint("종목코드:{0} | 종목명:{1} | 현재가:{2} | 보유수량:{3} | 매입가:{4} | 당일매도손익: {5}",
                        this.getData("종목코드", apiEvent, i),
                        this.getData("종목명", apiEvent, i),
                        this.getData("현재가", apiEvent, i),
                        this.getData("보유수량", apiEvent, i),
                        this.getData("매입가", apiEvent, i),
                        this.getData("당일매도손익", apiEvent, i));
                }
                catch (AccessViolationException execption) {
                    Logger.getInstance.print(Log.에러, "[receive:{0}] {1}\n{2}\n{3}", this.ToString(), execption.Message, execption.StackTrace, execption.InnerException);
                }
            }
        }
    }

    class AccountMoneyStatement : OrderStockStatement
    {
        public AccountMoneyStatement()
        {
            requestName_ = "계좌평가현황요청";
            requestCode_ = "OPW00004";
        }

        protected override bool setInput()
        {
            if (this.setParam("계좌번호", StockEngine.accountNumber()) == false) {
                return false;
            }

            //비밀번호 = 사용안함(공백)
            if (this.setParam("비밀번호", " ") == false) {
                return false;
            }

            //상장폐지조회구분 = 0:전체, 1 : 상장폐지종목제외
            if (this.setParam("상장폐지조회구분", "0") == false) {
                return false;
            }

            //비밀번호입력매체구분 = 00
            if (this.setParam("비밀번호입력매체구분", "0000") == false) {
                return false;
            }
            return true;
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            try {
                string temp1 = this.getData("주문가능금액", apiEvent, 0).Trim();
                string temp2 = this.getData("예수금", apiEvent, 0).Trim();
                int money = 0;
                if (temp1.Length != 0) {
                    money = int.Parse(temp1);
                }
                else if (temp2.Length != 0) {
                    money = int.Parse(temp2);
                }
                Program.stockBotDlg_.updateAccountMoney(money);

                Logger.getInstance.consolePrint("주문 가능금액 {0} | 예수금 {1} ", temp1, temp2);
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[receive:{0}] {1}\n{2}\n{3}", this.ToString(), execption.Message, execption.StackTrace, execption.InnerException);
            }
        }
    }

    //--------------------------------------------------------//
    // 계좌 로딩 클래스 정의 데이터 받는 부분이 같음
    class StockInfoLoadStatement : OrderStockStatement
    {
        protected bool isForbiddenStock(string name)
        {
            string[] forbiddenName = {
                "TIGER", "KODEX", "레버리지", "ETN", "ARIRANG", "인버스", "선물", "옵션",
            };

            foreach (string forbidden in forbiddenName) {
                if (name.Contains(forbidden)) {
                    return true;
                }
            }

            return false;
        }

        protected virtual StockDataValuation infoLevel()
        {
            return 0;
        }

        // watcing 주식 데이터 가지고 오는 공용 함수
        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            int count = this.getRowCount(apiEvent);
            for (int i = 0; i < count; i++) {
                try {
                    string codeStr = this.getData("종목코드", apiEvent, i).Trim();
                    int code = 0;
                    if (Int32.TryParse(codeStr, out code) == false) {
                        continue;
                    }
                    string name = this.getData("종목명", apiEvent, i);

                    if (this.isForbiddenStock(name)) {
                        continue;
                    }
                    StockData stockData = new StockData(code, name, this.infoLevel());
                    StockManager.getInstance.addStockData(stockData);
                }
                catch (AccessViolationException execption) {
                    Logger.getInstance.print(Log.에러, "[receive:{0}] {1}\n{2}\n{3}", this.ToString(), execption.Message, execption.StackTrace, execption.InnerException);
                    return;
                }
            }
        }
    }

    //--------------------------------------------------------//
    // 주식 목록을 불러오는 명령어들

    class NewHighPriceStock : StockInfoLoadStatement
    {
        public NewHighPriceStock()
        {
            requestName_ = "신고저가요청";
            requestCode_ = "OPT10016";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //신고저구분 = 1:신고가, 2 : 신저가
            if (this.setParam("신고저구분", "1") == false) {
                return false;
            }

            //고저종구분 = 1:고저기준, 2 : 종가기준
            if (this.setParam("고저종구분", "1") == false) {
                return false;
            }

            //종목조건 = 0:전체조회, 1 : 관리종목제외, 3 : 우선주제외, 5 : 증100제외, 6 : 증100만보기, 7 : 증40만보기, 8 : 증30만보기
            if (this.setParam("종목조건", "0") == false) {
                return false;
            }

            //거래량구분 = 00000:전체조회, 00010 : 만주이상, 00050 : 5만주이상, 00100 : 10만주이상, 00150 : 15만주이상, 00200 : 20만주이상, 00300 : 30만주이상, 00500 : 50만주이상, 01000 : 백만주이상
            if (this.setParam("거래량구분", "00100") == false) {
                return false;
            }

            //신용조건 = 0:전체조회, 1 : 신용융자A군, 2 : 신용융자B군, 3 : 신용융자C군, 4 : 신용융자D군, 9 : 신용융자전체
            if (this.setParam("신용조건", "0") == false) {
                return false;
            }

            //상하한포함 = 0:미포함, 1 : 포함
            if (this.setParam("상하한포함", "1") == false) {
                return false;
            }

            //기간 = 5:5일, 10 : 10일, 20 : 20일, 60 : 60일, 250 : 250일, 250일까지 입력가능
            if (this.setParam("기간", "250") == false) {
                return false;
            }
            return true;
        }
    }

    class ApproachHighPriceTradingStock : StockInfoLoadStatement
    {
        public ApproachHighPriceTradingStock()
        {
            requestName_ = "고저가근접요청";
            requestCode_ = "OPT10018";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //고저구분 = 1:고가, 2 : 저가
            if (this.setParam("고저구분", "1") == false) {
                return false;
            }

            //근접율 = 05:0.5 10 : 1.0, 15 : 1.5, 20 : 2.0. 25 : 2.5, 30 : 3.0
            if (this.setParam("근접율", "2.0") == false) {
                return false;
            }

            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //거래량구분 = 00000:전체조회, 00010 : 만주이상, 00050 : 5만주이상, 00100 : 10만주이상, 00150 : 15만주이상, 00200 : 20만주이상, 00300 : 30만주이상, 00500 : 50만주이상, 01000 : 백만주이상
            if (this.setParam("거래량구분", "00100") == false) {
                return false;
            }

            //종목조건 = 0:전체조회, 1 : 관리종목제외, 3 : 우선주제외, 5 : 증100제외, 6 : 증100만보기, 7 : 증40만보기, 8 : 증30만보기
            if (this.setParam("종목조건", "1") == false) {
                return false;
            }

            //신용조건 = 0:전체조회, 1 : 신용융자A군, 2 : 신용융자B군, 3 : 신용융자C군, 4 : 신용융자D군, 9 : 신용융자전체
            if (this.setParam("신용조건", "0") == false) {
                return false;
            }
            return true;
        }
    }

    class SuddenlyHighTradingStock : StockInfoLoadStatement
    {
        public SuddenlyHighTradingStock()
        {
            requestName_ = "가격급등락요청";
            requestCode_ = "OPT10019";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥, 201 : 코스피200
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //등락구분 = 1:급등, 2 : 급락
            if (this.setParam("등락구분", "1") == false) {
                return false;
            }

            //시간구분 = 1:분전, 2 : 일전
            if (this.setParam("시간구분", "2") == false) {
                return false;
            }

            //시간 = 분 혹은 일입력
            if (this.setParam("시간", "일") == false) {
                return false;
            }

            //거래량구분 = 00000:전체조회, 00010 : 만주이상, 00050 : 5만주이상, 00100 : 10만주이상, 00150 : 15만주이상, 00200 : 20만주이상, 00300 : 30만주이상, 00500 : 50만주이상, 01000 : 백만주이상
            if (this.setParam("거래량구분", "00200") == false) {
                return false;
            }

            //종목조건 = 0:전체조회, 1 : 관리종목제외, 3 : 우선주제외, 5 : 증100제외, 6 : 증100만보기, 7 : 증40만보기, 8 : 증30만보기
            if (this.setParam("종목조건", "1") == false) {
                return false;
            }

            //신용조건 = 0:전체조회, 1 : 신용융자A군, 2 : 신용융자B군, 3 : 신용융자C군, 4 : 신용융자D군, 9 : 신용융자전체
            if (this.setParam("신용조건", "0") == false) {
                return false;
            }

            //가격조건 = 0:전체조회, 1 : 1천원미만, 2 : 1천원~2천원, 3 : 2천원~3천원, 4 : 5천원~1만원, 5 : 1만원이상, 8 : 1천원이상
            if (this.setParam("가격조건", "0") == false) {
                return false;
            }

            //상하한포함 = 0:미포함, 1 : 포함
            if (this.setParam("상하한포함", "0") == false) {
                return false;
            }
            return true;
        }
    }

    class YesterdayHighTradingStock : StockInfoLoadStatement
    {
        public YesterdayHighTradingStock()
        {
            requestName_ = "전일대비등락률상위요청";
            requestCode_ = "OPT10027";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //정렬구분 = 1:상승률, 2 : 상승폭, 3 : 하락률, 4 : 하락폭
            if (this.setParam("정렬구분", "1") == false) {
                return false;
            }

            //거래량조건 = 0000:전체조회, 0010 : 만주이상, 0050 : 5만주이상, 0100 : 10만주이상, 0150 : 15만주이상, 0200 : 20만주이상, 0300 : 30만주이상, 0500 : 50만주이상, 1000 : 백만주이상
            if (this.setParam("거래량조건", "0100") == false) {
                return false;
            }

            //종목조건 = 0:전체조회, 1 : 관리종목제외, 4 : 우선주 + 관리주제외, 3 : 우선주제외, 5 : 증100제외, 6 : 증100만보기, 7 : 증40만보기, 8 : 증30만보기, 9 : 증20만보기, 11 : 정리매매종목제외
            if (this.setParam("종목조건", "1") == false) {
                return false;
            }

            //신용조건 = 0:전체조회, 1 : 신용융자A군, 2 : 신용융자B군, 3 : 신용융자C군, 4 : 신용융자D군, 9 : 신용융자전체
            if (this.setParam("신용조건", "0") == false) {
                return false;
            }

            //상하한포함 = 0:불 포함, 1 : 포함
            if (this.setParam("상하한포함", "0") == false) {
                return false;
            }

            //가격조건 = 0:전체조회, 1 : 1천원미만, 2 : 1천원~2천원, 3 : 2천원~5천원, 4 : 5천원~1만원, 5 : 1만원이상, 8 : 1천원이상
            if (this.setParam("가격조건", "0") == false) {
                return false;
            }

            //거래대금조건 = 0:전체조회, 3 : 3천만원이상, 5 : 5천만원이상, 10 : 1억원이상, 30 : 3억원이상, 50 : 5억원이상, 100 : 10억원이상, 300 : 30억원이상, 500 : 50억원이상, 1000 : 100억원이상, 3000 : 300억원이상, 5000 : 500억원이상
            if (this.setParam("거래대금조건", "10") == false) {
                return false;
            }
            return true;
        }
    }

    class YesterdayTopTradingStock : StockInfoLoadStatement
    {
        public YesterdayTopTradingStock()
        {
            requestName_ = "전일거래량상위요청";
            requestCode_ = "OPT10031";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //조회구분 = 1:전일거래량 상위100종목, 2 : 전일거래대금 상위100종목
            if (this.setParam("조회구분", "1") == false) {
                return false;
            }

            //순위시작 = 0 ~100 값 중에  조회를 원하는 순위 시작값
            if (this.setParam("순위시작", "0") == false) {
                return false;
            }

            //순위끝 = 0 ~100 값 중에  조회를 원하는 순위 끝값
            if (this.setParam("순위끝", "50") == false) {
                return false;
            }
            return true;
        }
    }

    class TodayTopTradingStock : StockInfoLoadStatement
    {
        public TodayTopTradingStock()
        {
            requestName_ = "당일거래량상위요청";
            requestCode_ = "OPT10030";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //조회구분 = 1:전일거래량 상위100종목, 2 : 전일거래대금 상위100종목
            if (this.setParam("조회구분", "1") == false) {
                return false;
            }

            //순위시작 = 0 ~100 값 중에  조회를 원하는 순위 시작값
            if (this.setParam("순위시작", "0") == false) {
                return false;
            }

            //순위끝 = 0 ~100 값 중에  조회를 원하는 순위 끝값
            if (this.setParam("순위끝", "50") == false) {
                return false;
            }
            return true;
        }
    }

    class ForeignerTradingSotck : StockInfoLoadStatement
    {
        public ForeignerTradingSotck()
        {
            requestName_ = "외인연속순매매상위요청";
            requestCode_ = "OPT10035";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            //시장구분 = 000:전체, 001 : 코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }

            //매매구분 = 1:연속순매도, 2 : 연속순매수
            if (this.setParam("매매구분", "2") == false) {
                return false;
            }

            //기간 = 0:당일, 1 : 전일, 5 : 5일, 10; 10일, 20:20일, 60 : 60일
            if (this.setParam("기간", "3") == false) {
                return false;
            }
            return true;
        }
    }

    class AgencyTradingStock : StockInfoLoadStatement
    {
        public AgencyTradingStock()
        {
            requestName_ = "기관 일별기관매매종목요청";
            requestCode_ = "OPT10044";
        }
        protected override StockDataValuation infoLevel()
        {
            return StockDataValuation.LEVEL_3;
        }

        protected override bool setInput()
        {
            DateTime now = DateTime.Now;
            DateTime start = now.AddDays(-5);
            //시작일자 = YYYYMMDD(20160101 연도4자리, 월 2자리, 일 2자리 형식)
            string startDay = start.ToString("yyyyMMdd");
            if (this.setParam("시작일자", startDay) == false) {
                return false;
            }

            //종료일자 = YYYYMMDD(20160101 연도4자리, 월 2자리, 일 2자리 형식)
            DateTime end = now.AddDays(-1);
            string endDay = end.ToString("yyyyMMdd");
            if (this.setParam("종료일자", endDay) == false) {
                return false;
            }

            //매매구분 = 0:전체, 1 : 순매도, 2 : 순매수
            if (this.setParam("매매구분", "2") == false) {
                return false;
            }

            //시장구분 = 001:코스피, 101 : 코스닥
            if (this.setParam("시장구분", "000") == false) {
                return false;
            }
            return true;
        }
    }

    //--------------------------------------------------------//
    // 주식 데이터 정보(차트 정보)
    class StockInfoStatement : OrderStockStatement
    {
        protected int stockCode_;
        protected PRICE_TYPE priceType_;

        protected string stockCode()
        {
            return string.Format("{0:D6}", stockCode_);
        }

        public override void request()
        {
            orderTick_ = DateTime.Now.Ticks;
            this.setInput();
            int nRet = StockEngine.getInstance.khOpenApi().CommRqData(requestName_, requestCode_, 0, screenNo_);
            Logger.getInstance.consolePrint("주문 [{0}:{1}]\t\t결과 메시지 : {2}", requestCode_, requestName_, Error.GetErrorMessage());
        }

        protected virtual string dateTime(int index, object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            string dateTime = this.getData("체결시간", apiEvent, index);
            return dateTime;
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            try {
                StockData stockData = StockManager.getInstance.stockData(stockCode_);
                if (stockData == null) {
                    return;
                }

                lock (stockData.dataLock_) {
                    stockData.clearPrice(priceType_);

                    int count = this.getRowCount(apiEvent);
                    for (int i = 0; i < count; i++) {
                        string dateTime = this.dateTime(i, sender, apiEvent);

                        int price = int.Parse(this.getData("현재가", apiEvent, i));
                        int startPrice = int.Parse(this.getData("시가", apiEvent, i));
                        int highPrice = int.Parse(this.getData("고가", apiEvent, i));
                        int lowPrice = int.Parse(this.getData("저가", apiEvent, i));

                        PriceData priceData = new PriceData(dateTime, price, startPrice, highPrice, lowPrice);
                        stockData.updatePrice(priceType_, i, priceData);
                    }
                }

                stockData.calculatePrice(priceType_);
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[receive:{0}] {1}\n{2}\n{3}", this.ToString(), execption.Message, execption.StackTrace, execption.InnerException);
            }
        }
    }

    //--------------------------------------------------------//
    // 주식 종목의 차트 조회
    class HistoryTheTicksStock : StockInfoStatement
    {
        public HistoryTheTicksStock(int stockCode)
        {
            stockCode_ = stockCode;
            requestName_ = "주식틱차트조회요청";
            requestCode_ = "OPT10079";
            priceType_ = PRICE_TYPE.TICK;
        }

        protected override bool setInput()
        {
            if (this.setParam("종목코드", this.stockCode()) == false) {
                return false;
            }
            if (this.setParam("틱범위", "1") == false) {
                return false;
            }
            if (this.setParam("수정주가구분", "1") == false) {
                return false;
            }
            return true;
        }

        protected override string dateTime(int index, object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            string date = this.getData("체결시간", apiEvent, index);
            string dd = date.Substring(date.Length - 8, 2);
            string min = date.Substring(date.Length - 6, 6);
            string dateTime = string.Format("{0}d {1}t#{2}", dd, min, index);
            return dateTime;
        }
    }

    class HistoryTheMinsStock : StockInfoStatement
    {
        public HistoryTheMinsStock(int stockCode)
        {
            stockCode_ = stockCode;
            requestName_ = "주식분봉차트조회요청";
            requestCode_ = "OPT10080";
            priceType_ = PRICE_TYPE.MIN;
        }

        protected override bool setInput()
        {
            if (this.setParam("종목코드", this.stockCode()) == false) {
                return false;
            }
            if (this.setParam("틱범위", "1") == false) {
                return false;
            }
            if (this.setParam("수정주가구분", "1") == false) {
                return false;
            }
            return true;
        }

        protected override string dateTime(int index, object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            string date = this.getData("체결시간", apiEvent, index);
            string dd = date.Substring(date.Length - 8, 2);
            string min = date.Substring(date.Length - 6, 6);
            string dateTime = string.Format("{0}d {1}m#{2}", dd, min, index);
            return dateTime;
        }
    }

    class HistoryTheDaysStock : StockInfoStatement
    {
        public HistoryTheDaysStock(int stockCode)
        {
            stockCode_ = stockCode;
            requestName_ = "주식일봉차트조회";
            requestCode_ = "OPT10081";
            priceType_ = PRICE_TYPE.DAY;
        }

        protected override bool setInput()
        {
            if (this.setParam("종목코드", this.stockCode()) == false) {
                return false;
            }
            if (this.setParam("기준일자", DateTime.Now.ToString("yyyyMMdd")) == false) {
                return false;
            }
            if (this.setParam("수정주가구분", "1") == false) {
                return false;
            }
            return true;
        }

        protected override string dateTime(int index, object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            string date = this.getData("일자", apiEvent, index);
            string mm = date.Substring(date.Length - 4, 2);
            string dd = date.Substring(date.Length - 2, 2);
            string dateTime = string.Format("{0}m {1}d#{2}", mm, dd, index);
            return dateTime;
        }
    }

    //--------------------------------------------------------//
    // 주식 주문 
    class TradingStatement : StockStatement
    {
        // 스크린번호를 주문에 대해서는 따로 대역대를 가지고 간다.

        // =================================================
        // 매매구분 취득
        // (1:신규매수, 2:신규매도 3:매수취소, 
        // 4:매도취소, 5:매수정정, 6:매도정정)
        protected KOACode.OrderType dealingType_;      //매매 구분

        // =================================================
        // 거래구분 취득
        // 0:지정가, 3:시장가, 5:조건부지정가, 6:최유리지정가, 7:최우선지정가,
        // 10:지정가IOC, 13:시장가IOC, 16:최유리IOC, 20:지정가FOK, 23:시장가FOK,
        // 26:최유리FOK, 61:장개시전시간외, 62:시간외단일가매매, 81:시간외종가
        protected KOACode.HogaGb hogaType_;         //거래 구분 (hoga/호가) 

        // 이하 입력 값들
        public int tradingCount_ { get; set; }          //주문 수량
        protected int tradingPrice_;                    //주문 가격
        public int stockCode_ { get; set; }             //주식 코드
        public string orderNumber_;                     //주문 번호

        public TradingStatement()
        {
            orderNumber_ = "";
        }

        private string stockCode()
        {
            return string.Format("{0:D6}", stockCode_);
        }

        public string getScreenNo()
        {
            return screenNo_;
        }

        public override string getKey()
        {
            return stockCode_.ToString();
        }

        protected override bool setInput()
        {
            return true;
        }

        public override void request()
        {
            orderTick_ = DateTime.Now.Ticks;

            if (tradingCount_ <= 0) {
                return;
            }

            if (this.setInput() == false) {
                return;
            }

            int nRet = StockEngine.getInstance.khOpenApi().SendOrder(
                                        requestName_,
                                        screenNo_,
                                        StockEngine.accountNumber(),
                                        dealingType_.code,
                                        this.stockCode(),
                                        tradingCount_,
                                        tradingPrice_,
                                        hogaType_.code,
                                        orderNumber_);
            if (Error.IsError(nRet)) {
                Logger.getInstance.print(Log.API조회, "주식주문:{0}, 주식 코드:{1}, 갯수:{2}, 주문가:{3}, in 계좌:{4}",
                    Error.GetErrorMessage(), this.stockCode(), tradingCount_, tradingPrice_, StockEngine.accountNumber());
            }
            else {
                Logger.getInstance.print(Log.에러, "주식주문 : " + Error.GetErrorMessage());
            }
            Logger.getInstance.print(Log.API조회, "주문 완료 :{0}, 번호:{1}, 주문:{2}, 호가:{3}, 주식:{4}, 갯수:{5}, 주문가:{6}, 계좌:{7}"
                , requestName_, screenNo_, dealingType_.name, hogaType_.name, this.stockCode(), tradingCount_, tradingPrice_, StockEngine.accountNumber());
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            try {
                string orderNumber = StockEngine.getInstance.khOpenApi().GetCommData(apiEvent.sTrCode, "", 0, "").Trim();
                //주문 번호에 대한 객체를 만들어서 처리해야 할듯;;;
                long longOrderNumber = 0;
                bool canConvert = long.TryParse(orderNumber, out longOrderNumber);

                if (canConvert == true) {
                    Logger.getInstance.print(Log.API결과, "주문 번호 : {0}", orderNumber);
                }
                else {
                    Logger.getInstance.print(Log.에러, "잘못된 원주문번호 입니다");
                }
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[receive:{0}] {1}\n{2}\n{3}", this.ToString(), execption.Message, execption.StackTrace, execption.InnerException);
            }
        }
    }

    class BuyStock : TradingStatement
    {
        public BuyStock(int stockCode, int tradingCount, int tradingPrice)
        {
            // 거래구분 취득
            // 0:지정가, 3:시장가, 5:조건부지정가, 6:최유리지정가, 7:최우선지정가,
            // 10:지정가IOC, 13:시장가IOC, 16:최유리IOC, 20:지정가FOK, 23:시장가FOK,
            // 26:최유리FOK, 61:장개시전시간외, 62:시간외단일가매매, 81:시간외종가
            requestName_ = "BUY_STOCK";
            stockCode_ = stockCode;
            tradingCount_ = tradingCount;
            tradingPrice_ = tradingPrice;
        }

        const bool staticPrice = false;
        protected override bool setInput()
        {
            dealingType_ = KOACode.orderType[0];           //신규매수
            if (staticPrice || tradingPrice_ != 0) {
                hogaType_ = KOACode.hogaGb[0];             //지정가
            }
            else {
                hogaType_ = KOACode.hogaGb[1];             //시장가
                tradingPrice_ = 0;                         //시장가
            }
            Logger.getInstance.print(Log.API조회, "주식{0} 구입{1} 구입타입 {2} : 갯수 {3} x 가격 {4}"
                , stockCode_, dealingType_.name, hogaType_.name, tradingCount_, tradingPrice_);

            int price = tradingPrice_;
            if (price == 0) {
                StockData stockData = StockManager.getInstance.stockData(stockCode_);
                if (stockData == null) {
                    return false;
                }
                price = stockData.nowPrice(PRICE_TYPE.MIN);
            }
            return true;
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            base.receive(sender, apiEvent);
        }
    }

    class SellStock : TradingStatement
    {
        public SellStock(BuyedStockData ownerStock)
        {
            // 거래구분 취득
            // 0:지정가, 3:시장가, 5:조건부지정가, 6:최유리지정가, 7:최우선지정가,
            // 10:지정가IOC, 13:시장가IOC, 16:최유리IOC, 20:지정가FOK, 23:시장가FOK,
            // 26:최유리FOK, 61:장개시전시간외, 62:시간외단일가매매, 81:시간외종가
            requestName_ = "SELL_STOCK";
            stockCode_ = ownerStock.code_;
            tradingCount_ = ownerStock.buyCount_;
            tradingPrice_ = ownerStock.nowPrice(PRICE_TYPE.MIN);
        }

        const bool staticPrice = false;
        protected override bool setInput()
        {
            dealingType_ = KOACode.orderType[1];            //신규매도
            if (staticPrice || tradingPrice_ != 0) {
                hogaType_ = KOACode.hogaGb[0];              //지정가
            }
            else {
                hogaType_ = KOACode.hogaGb[1];              //시장가
                tradingPrice_ = 0;                          //시장가
            }
            Logger.getInstance.print(Log.API결과, "주식{0} 판매{1} 판매타입 {2} : 갯수 {3} x 가격 {4}"
               , stockCode_, dealingType_.name, hogaType_.name, tradingCount_, tradingPrice_);

            return true;
        }

        public override void receive(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            base.receive(sender, apiEvent);
        }
    }
}
