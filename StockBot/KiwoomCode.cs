namespace KiwoomCode
{
    public enum Log
    {
        API조회,      // 조회창 출력
        API결과,
        StockAPI콜백, // StockAPI콜백 출력
        주식봇,
        에러,         // 에러창 출력
    }

    class KOAErrorCode
    {
        public const int OP_ERR_NONE = 0;     //"정상처리"
        public const int OP_ERR_LOGIN = -100;  //"사용자정보교환에 실패하였습니다. 잠시후 다시 시작하여 주십시오."
        public const int OP_ERR_CONNECT = -101;  //"서버 접속 실패"
        public const int OP_ERR_VERSION = -102;  //"버전처리가 실패하였습니다.
        public const int OP_ERR_SISE_OVERFLOW = -200;  //”시세조회 과부하”
        public const int OP_ERR_RQ_STRUCT_FAIL = -201;  //”REQUEST_INPUT_st Failed”
        public const int OP_ERR_RQ_STRING_FAIL = -202;  //”요청 전문 작성 실패”
        public const int OP_ERR_ORD_WRONG_INPUT = -300;  //”주문 입력값 오류”
        public const int OP_ERR_ORD_WRONG_ACCNO = -301;  //”계좌비밀번호를 입력하십시오.”
        public const int OP_ERR_OTHER_ACC_USE = -302;  //”타인계좌는 사용할 수 없습니다.
        public const int OP_ERR_MIS_2BILL_EXC = -303;  //”주문가격이 20억원을 초과합니다.”
        public const int OP_ERR_MIS_5BILL_EXC = -304;  //”주문가격은 50억원을 초과할 수 없습니다.”
        public const int OP_ERR_MIS_1PER_EXC = -305;  //”주문수량이 총발행주수의 1%를 초과합니다.”
        public const int OP_ERR_MID_3PER_EXC = -306;  //”주문수량은 총발행주수의 3%를 초과할 수 없습니다.”
    }


    public class KOACode
    {

        /// <summary>
        /// 주문코드 클래스
        /// </summary>
        public struct OrderType
        {
            private string Name;
            private int Code;

            public OrderType(int nCode, string strName)
            {
                this.Name = strName;
                this.Code = nCode;
            }

            public string name
            {
                get {
                    return this.Name;
                }
            }

            public int code
            {
                get {
                    return this.Code;
                }
            }
        }

        public readonly static OrderType[] orderType = new OrderType[6];


        /// <summary>
        /// 호가구분 클래스
        /// </summary>
        public struct HogaGb
        {
            private string Name;
            private string Code;

            public HogaGb(string strCode, string strName)
            {
                this.Code = strCode;
                this.Name = strName;
            }

            public string name
            {
                get {
                    return this.Name;
                }
            }

            public string code
            {
                get {
                    return this.Code;
                }
            }
        }

        public readonly static HogaGb[] hogaGb = new HogaGb[13];

        public struct MarketCode
        {
            private string Name;
            private string Code;

            public MarketCode(string strCode, string strName)
            {
                this.Code = strCode;
                this.Name = strName;
            }

            public string name
            {
                get {
                    return this.Name;
                }
            }

            public string code
            {
                get {
                    return this.Code;
                }
            }
        }

        public readonly static MarketCode[] marketCode = new MarketCode[9];

        static KOACode()
        {
            // 주문타입 설정
            orderType[0] = new OrderType(1, "신규매수");
            orderType[1] = new OrderType(2, "신규매도");
            orderType[2] = new OrderType(3, "매수취소");
            orderType[3] = new OrderType(4, "매도취소");
            orderType[4] = new OrderType(5, "매수정정");
            orderType[5] = new OrderType(6, "매도정정");

            // 호가타입 설정
            hogaGb[0] = new HogaGb("00", "지정가");
            hogaGb[1] = new HogaGb("03", "시장가");
            hogaGb[2] = new HogaGb("05", "조건부지정가");
            hogaGb[3] = new HogaGb("06", "최유리지정가");
            hogaGb[4] = new HogaGb("07", "최우선지정가");
            hogaGb[5] = new HogaGb("10", "지정가IOC");
            hogaGb[6] = new HogaGb("13", "시장가IOC");
            hogaGb[7] = new HogaGb("16", "최유리IOC");
            hogaGb[8] = new HogaGb("20", "지정가FOK");
            hogaGb[9] = new HogaGb("23", "시장가FOK");
            hogaGb[10] = new HogaGb("26", "최유리FOK");
            hogaGb[11] = new HogaGb("61", "시간외단일가매매");
            hogaGb[12] = new HogaGb("81", "시간외종가");

            // 마켓코드 설정
            marketCode[0] = new MarketCode("0", "장내");
            marketCode[1] = new MarketCode("3", "ELW");
            marketCode[2] = new MarketCode("4", "뮤추얼펀드");
            marketCode[3] = new MarketCode("5", "신주인수권");
            marketCode[4] = new MarketCode("6", "리츠");
            marketCode[5] = new MarketCode("8", "ETF");
            marketCode[6] = new MarketCode("9", "하이일드펀드");
            marketCode[7] = new MarketCode("10", "코스닥");
            marketCode[8] = new MarketCode("30", "제3시장");
        }
    }


    class Error
    {
        private static string errorMessage;

        Error()
        {
            errorMessage = "";
        }

        ~Error()
        {
            errorMessage = "";
        }

        public static string GetErrorMessage()
        {
            return errorMessage;
        }

        public static bool IsError(int nErrorCode)
        {
            bool bRet = false;

            switch (nErrorCode) {
                case KOAErrorCode.OP_ERR_NONE:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "정상처리";
                    bRet = true;
                    break;
                case KOAErrorCode.OP_ERR_LOGIN:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "사용자정보교환에 실패하였습니다. 잠시 후 다시 시작하여 주십시오.";
                    break;
                case KOAErrorCode.OP_ERR_CONNECT:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "서버 접속 실패";
                    break;
                case KOAErrorCode.OP_ERR_VERSION:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "버전처리가 실패하였습니다";
                    break;
                case KOAErrorCode.OP_ERR_SISE_OVERFLOW:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "시세조회 과부하";
                    break;
                case KOAErrorCode.OP_ERR_RQ_STRUCT_FAIL:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "REQUEST_INPUT_st Failed";
                    break;
                case KOAErrorCode.OP_ERR_RQ_STRING_FAIL:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "요청 전문 작성 실패";
                    break;
                case KOAErrorCode.OP_ERR_ORD_WRONG_INPUT:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "주문 입력값 오류";
                    break;
                case KOAErrorCode.OP_ERR_ORD_WRONG_ACCNO:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "계좌비밀번호를 입력하십시오.";
                    break;
                case KOAErrorCode.OP_ERR_OTHER_ACC_USE:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "타인계좌는 사용할 수 없습니다.";
                    break;
                case KOAErrorCode.OP_ERR_MIS_2BILL_EXC:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "주문가격이 20억원을 초과합니다.";
                    break;
                case KOAErrorCode.OP_ERR_MIS_5BILL_EXC:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "주문가격은 50억원을 초과할 수 없습니다.";
                    break;
                case KOAErrorCode.OP_ERR_MIS_1PER_EXC:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "주문수량이 총발행주수의 1%를 초과합니다.";
                    break;
                case KOAErrorCode.OP_ERR_MID_3PER_EXC:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "주문수량은 총발행주수의 3%를 초과할 수 없습니다";
                    break;
                default:
                    errorMessage = "[" + nErrorCode.ToString() + "] :" + "알려지지 않은 오류입니다.";
                    break;
            }

            return bRet;
        }
    }
}
