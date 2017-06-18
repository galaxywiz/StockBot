using System.Windows.Forms;

namespace StockBot.DlgControl
{
    class StockWebBrowser : SingleTon<StockWebBrowser>
    {
        WebBrowser webBrowser_ = null;

        StockWebBrowser()
        {
            webBrowser_ = Program.stockBotDlg_.chart_WebBrowser();
            webBrowser_.ScriptErrorsSuppressed = true;
        }

        public void searchWeb(string stockCodeString)
        {
            string url = string.Format("http://finance.naver.com/item/coinfo.nhn?code={0}", stockCodeString);
            webBrowser_.Navigate(url);
        }
    }
}
