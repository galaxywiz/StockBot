using KiwoomCode;
using StockBot.KiwoomStock;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace StockBot
{
    class StockEngine : SingleTon<StockEngine>
    {
        private Thread thread_ = null;
        private StockBotDlg StockBot_ = null;
        private AxKHOpenAPILib.AxKHOpenAPI khOpenApi_ = null;

        public void setup(StockBotDlg StockBot)
        {
            StockBot_ = StockBot;
            khOpenApi_ = StockBot_.openApi();
            khOpenApi_.CreateControl();

            receivePoolLock_ = new object();
            tradingPoolLock_ = new object();

            statmentOrderPool_ = new ConcurrentQueue<StockStatement>();

            // 런 함수를 쓰레드로 계속 돌려 줍니다.
            thread_ = new Thread(this.run);
            thread_.Start();
        }

        //---------------------------------------------------------------------
        // 쓰레드 실행 루프
        private bool runLoop_ = true;
        public bool runLoop()
        {
            return runLoop_;
        }

        public void shutdown()
        {
            runLoop_ = false;
        }

        //---------------------------------------------------------------------
        // 처리 부분.
        private int orderCount_ = 0;
        private void run()
        {
            orderCount_ = 0;
            // 요청과 상태 쓰레드를 동시에 가지고 간다. 
            // 분리하면 주식 모듈로의 send /recv 가 꼬일 수 있음..
            while (this.runLoop()) {
                Thread.Sleep(1);

                this.processStockOrder();
                if (isBusy() == false) {
                    StockManager.getInstance.process();
                }
                this.processOrderCleanup();
            }
        }

        private bool isBusy()
        {
            // 키움증권 api activex 에서 200개 이상의 주문을 받질 못 함...
            const int limitOrderCount = 180;

            //  lock (orderPoolLock_) {
            if (orderCount_ < limitOrderCount) {
                return false;
            }
            Logger.getInstance.consolePrint("주식 주문이 총 {0} 설정된 {1}을 초과, 잠시 대기...", orderCount_, limitOrderCount);
            return true;
        }

        private bool runNextOrderFlag_ = true;
        private void runNextOrderFlag()
        {
            runNextOrderFlag_ = true;
        }

        ConcurrentQueue<StockStatement> statmentOrderPool_ = null;
        // 매매 이외의 주문은 모두 이쪽으로 처리 receive 처리함.
        private object receivePoolLock_ = null;
        private Dictionary<string, StockStatement> statmentReceivePool_ = new Dictionary<string, StockStatement>();

        //매매 관련 주문은 다른 경로로 데이터가 흐름.
        private object tradingPoolLock_ = null;
        private Dictionary<string, TradingStatement> tradingReceivePool_ = new Dictionary<string, TradingStatement>();

        //---------------------------------------------------------------------
        // 실제 주문을 하는 곳.
        private long tickCount_ = 0;
        private void processStockOrder()
        {
            if (!this.runLoop()) {
                return;
            }
            long now = DateTime.Now.Ticks;
            if (tickCount_ + (TimeSpan.TicksPerSecond * 3) < now) {
                tickCount_ = now;
                runNextOrderFlag_ = true;       // 3초이내 응답이 없으면 강제로 flag 켜준다.
            }

            // 다음 요청이 올때까지 hold (COM 모듈 내부가 꼬이는듯 ㅡㅡ..)
            if (!runNextOrderFlag_) {
                return;
            }

            StockStatement statement = null;
            if (statmentOrderPool_.TryDequeue(out statement)) {
                statement.request();

                //이걸 recv 쪽에 받아놓고 있다가 실제 삭제는 recv
                if ((statement.GetType() != typeof(BuyStock))
                        && (statement.GetType() != typeof(SellStock))) {
                    this.addStatmentReceive(statement);
                }
                else {
                    TradingStatement tradingStat = (TradingStatement)statement;
                    this.addTradingReceive(tradingStat);
                }

                runNextOrderFlag_ = false;
            }
            // 키움증권 제한 1초에 5번만 요청 가능
            Thread.Sleep(5 / 1000);
        }

        private void processOrderCleanup()
        {
            // 주식 명령을 내린뒤, receive를 여러번 받을 수 있음. 
            // 그래서 지연 삭제를 구현함.
            long now = DateTime.Now.Ticks;
            long turn = TimeSpan.TicksPerSecond * 60;
            lock (receivePoolLock_) {
                ArrayList deleteOrder = new ArrayList();

                foreach (KeyValuePair<string, StockStatement> data in statmentReceivePool_) {
                    StockStatement stat = data.Value;
                    if (stat.orderTick_ + turn < now) {
                        deleteOrder.Add(data.Key);
                    }
                }

                foreach (string deleteKey in deleteOrder) {
                    orderCount_--;
                    statmentReceivePool_.Remove(deleteKey);
                }
            }
        }
        //---------------------------------------------------------------------
        // 주식 주문을 쌓아 두는 곳
        public void addOrder(StockStatement statement)
        {
            statmentOrderPool_.Enqueue(statement);
        }
                
        private void addStatmentReceive(StockStatement statement)
        {
            lock (receivePoolLock_) {
                statmentReceivePool_[statement.getKey()] = statement;
            }
        }

        private void addTradingReceive(TradingStatement statement)
        {
            lock (tradingPoolLock_) {
                tradingReceivePool_[statement.getKey()] = statement;
            }
        }

        private StockStatement getStockStatement(string key)
        {
            lock (receivePoolLock_) {
                foreach (KeyValuePair<string, StockStatement> data in statmentReceivePool_) {
                    if (data.Key.CompareTo(key) == 0) {
                        return data.Value;
                    }
                }
            }
            return null;
        }

        private StockStatement popStockStatement(string key)
        {
            lock (receivePoolLock_) {
                StockStatement statement = null;

                if (statmentReceivePool_.TryGetValue(key, out statement)) {
                    statmentReceivePool_.Remove(key);
                    return statement;
                }
            }
            return null;
        }

        private TradingStatement getTradingStatement(string screenNo)
        {
            lock (tradingPoolLock_) {
                foreach (KeyValuePair<string, TradingStatement> data in tradingReceivePool_) {
                    if (data.Value.getScreenNo().CompareTo(screenNo) == 0) {
                        return data.Value;
                    }
                }
            }
            return null;
        }

        private TradingStatement popTradingStatement(string key)
        {
            lock (tradingPoolLock_) {
                TradingStatement statement = null;

                if (tradingReceivePool_.TryGetValue(key, out statement)) {
                    tradingReceivePool_.Remove(key);
                    return statement;
                }
            }
            return null;
        }

        public int getReceiveStatementCount()
        {
            int tradingCount = tradingReceivePool_.Count;
            int recvCount = statmentReceivePool_.Count;
            return tradingCount + recvCount;
        }

        //---------------------------------------------------------------------
        //라이브러리 포팅 함수들
        public AxKHOpenAPILib.AxKHOpenAPI khOpenApi()
        {
            return khOpenApi_;
        }

        public bool connected()
        {
            int state = khOpenApi_.GetConnectState();
            if (state == 0) {
                return false;
            }
            return true;
        }

        public bool start()
        {
            if (connected() == true) {
                return true;
            }

            if (khOpenApi_.CommConnect() != 0) {
                Logger.getInstance.print(Log.에러, "로그인창 열기 실패");
                return false;
            }

            Logger.getInstance.print(Log.API조회, "로그인창 열기 성공");
            //ocx 콜백 함수 등록
            khOpenApi_.OnReceiveTrData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEventHandler(axKHOpenAPI_OnReceiveTrData);
            khOpenApi_.OnReceiveRealData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealDataEventHandler(axKHOpenAPI_OnReceiveRealData);
            khOpenApi_.OnReceiveMsg += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEventHandler(axKHOpenAPI_OnReceiveMsg);
            khOpenApi_.OnReceiveChejanData += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEventHandler(axKHOpenAPI_OnReceiveChejanData);
            khOpenApi_.OnEventConnect += new AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEventHandler(axKHOpenAPI_OnEventConnect);
            khOpenApi_.OnReceiveRealCondition += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealConditionEventHandler(axKHOpenAPI_OnReceiveRealCondition);
            khOpenApi_.OnReceiveTrCondition += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrConditionEventHandler(axKHOpenAPI_OnReceiveTrCondition);
            khOpenApi_.OnReceiveConditionVer += new AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveConditionVerEventHandler(axKHOpenAPI_OnReceiveConditionVer);

            return true;
        }

        public void quit()
        {
            if (connected()) {
                khOpenApi_.CommTerminate();
            }
            thread_.Join();
        }

        //---------------------------------------------------------------------
        // 주문등의 주식 관련 명령 실행
        static string accountNum_;
        public bool loadAccountInfo()
        {
            if (connected() == false) {
                return false;
            }
            StockBot_.userId().Text = khOpenApi_.GetLoginInfo("USER_ID");

            string[] accountNumber = khOpenApi_.GetLoginInfo("ACCNO").Split(';');

            foreach (string num in accountNumber) {
                if (num == "") {
                    continue;
                }
                StockBot_.account().Text = num.Trim();
                accountNum_ = num.Trim();
            }

            Logger.getInstance.print(Log.API결과, "계좌 번호 가지고 오기 성공");
            return true;
        }

        public static string accountNumber()
        {
            return accountNum_;
        }

        //---------------------------------------------------------------------
        // 주식 모듈의 콜벡 델리게이션
        public void axKHOpenAPI_OnEventConnect(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent apiEvent)
        {
            try {
                if (Error.IsError(apiEvent.nErrCode)) {
                    Logger.getInstance.print(Log.StockAPI콜백, "[로그인 처리결과] " + Error.GetErrorMessage());
                    this.loadAccountInfo();
                    Program.stockBotDlg_.buttonStart().Enabled = true;
                }
                else {
                    Logger.getInstance.print(Log.에러, "[로그인 처리결과] " + Error.GetErrorMessage());
                }
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[로그인 콜백 에러]" + execption.Message);
            }
            this.runNextOrderFlag();
        }

        // 일반 주문
        public void axKHOpenAPI_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent apiEvent)
        {
            try {
                string key = apiEvent.sRQName + apiEvent.sScrNo;

                StockStatement statement = this.getStockStatement(key);
                if (statement != null) {
                    statement.receive(sender, apiEvent);
                }
                else {
                    TradingStatement stat = this.getTradingStatement(apiEvent.sScrNo);
                    if (stat != null) {
                        stat.receive(sender, apiEvent);
                    }
                    else {
                        Logger.getInstance.print(Log.StockAPI콜백, "[이 주문표 데이터가 삭제되었음. {0}", key);
                    }
                }
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[주문 콜백 에러] {0}\n{1}\n{2}", execption.Message, execption.StackTrace, execption.InnerException);
            }
            this.runNextOrderFlag();
        }

        public void axKHOpenAPI_OnReceiveMsg(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveMsgEvent apiEvent)
        {
            // 그냥 이 주문이 잘 실행되었습니다 하는 콜백...
            //화면번호:1204 | RQName:주식분봉차트조회요청 | TRCode:OPT10080 | 메세지:[00Z310] 모의투자 조회가 완료되었습니다
            this.runNextOrderFlag();
        }

        // 체결 주문 관련
        public void axKHOpenAPI_OnReceiveChejanData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent apiEvent)
        {
            try {
                if (apiEvent.sGubun == "0") {
                    string stockName = khOpenApi_.GetChejanData(302);

                    // 주문 수량
                    string tradingAmountStr = khOpenApi_.GetChejanData(900);
                    int tradingAmount = 0;
                    if (int.TryParse(tradingAmountStr, out tradingAmount)) {
                    }

                    // 체결 수량
                    string tradingCountStr = khOpenApi_.GetChejanData(911);
                    int tradingCount = 0;
                    if (int.TryParse(tradingCountStr, out tradingCount)) {
                    }

                    // 주식 코드
                    string codeStr = khOpenApi_.GetChejanData(9001);
                    int code = 0;
                    if (Int32.TryParse(codeStr, out code)) {
                    }
                }
                else if (apiEvent.sGubun == "1") {
                    Logger.getInstance.print(Log.StockAPI콜백, "구분 : 잔고통보");
                }
                else if (apiEvent.sGubun == "3") {
                    Logger.getInstance.print(Log.StockAPI콜백, "구분 : 특이신호");
                }
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[채결 / 잔고 처리 콜백 에러] {0}\n{1}\n{2}", execption.Message, execption.StackTrace, execption.InnerException);
            }
            this.runNextOrderFlag();
        }

        public void axKHOpenAPI_OnReceiveRealData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealDataEvent apiEvent)
        {
            try {
                if (apiEvent.sRealType == "주식시세") {
                    Logger.getInstance.print(Log.StockAPI콜백, "종목코드 : {0} | 현재가 : {1:C} | 등락율 : {2} | 누적거래량 : {3:N0} ",
                            apiEvent.sRealKey,
                            Int32.Parse(khOpenApi_.GetCommRealData(apiEvent.sRealType, 10).Trim()),
                            khOpenApi_.GetCommRealData(apiEvent.sRealType, 12).Trim(),
                            Int32.Parse(khOpenApi_.GetCommRealData(apiEvent.sRealType, 13).Trim()));
                }
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[주식 데이터 콜백 에러] {0}\n{1}\n{2}", execption.Message, execption.StackTrace, execption.InnerException);
            }
            this.runNextOrderFlag();
        }

        public void axKHOpenAPI_OnReceiveRealCondition(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealConditionEvent apiEvent)
        {
            try {
                Logger.getInstance.print(Log.StockAPI콜백, "========= 조건조회 실시간 편입/이탈 ==========");
                Logger.getInstance.print(Log.StockAPI콜백, "[종목코드] : " + apiEvent.sTrCode);
                Logger.getInstance.print(Log.StockAPI콜백, "[실시간타입] : " + apiEvent.strType);
                Logger.getInstance.print(Log.StockAPI콜백, "[조건명] : " + apiEvent.strConditionName);
                Logger.getInstance.print(Log.StockAPI콜백, "[조건명 인덱스] : " + apiEvent.strConditionIndex);
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[실시간 조건 조회 콜백 에러] {0}\n{1}\n{2}", execption.Message, execption.StackTrace, execption.InnerException);
            }
            this.runNextOrderFlag();
        }

        public void axKHOpenAPI_OnReceiveTrCondition(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrConditionEvent apiEvent)
        {
            try {
                Logger.getInstance.print(Log.StockAPI콜백, "[화면번호] : " + apiEvent.sScrNo);
                Logger.getInstance.print(Log.StockAPI콜백, "[종목리스트] : " + apiEvent.strCodeList);
                Logger.getInstance.print(Log.StockAPI콜백, "[조건명] : " + apiEvent.strConditionName);
                Logger.getInstance.print(Log.StockAPI콜백, "[조건명 인덱스 ] : " + apiEvent.nIndex.ToString());
                Logger.getInstance.print(Log.StockAPI콜백, "[연속조회] : " + apiEvent.nNext.ToString());
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[컨디션 체크 콜백 에러] {0}\n{1}\n{2}", execption.Message, execption.StackTrace, execption.InnerException);
            }
            this.runNextOrderFlag();
        }

        public void axKHOpenAPI_OnReceiveConditionVer(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveConditionVerEvent apiEvent)
        {
            try {
                if (apiEvent.lRet == 1) {
                    Logger.getInstance.print(Log.StockAPI콜백, "[이벤트] 조건식 저장 성공");
                }
                else {
                    Logger.getInstance.print(Log.에러, "[이벤트] 조건식 저장 실패 : " + apiEvent.sMsg);
                }
            }
            catch (AccessViolationException execption) {
                Logger.getInstance.print(Log.에러, "[이벤트 콜백 에러] {0}\n{1}\n{2}", execption.Message, execption.StackTrace, execption.InnerException);
            }
            this.runNextOrderFlag();
        }
    }
}
