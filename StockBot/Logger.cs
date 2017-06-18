using KiwoomCode;
using System;
using System.IO;
using System.Windows.Forms;

namespace StockBot
{
    class Logger : SingleTon<Logger>
    {
        private ListView listView_ = null;
        private TextWriter logFile_ = null;
        private string fileName_ = "";
        private bool logActive_;

        public void close()
        {
            logActive_ = false;
            logFile_.Close();
        }

        public void setup(ListView listView)
        {
            listView_ = listView;

            listView_.BeginUpdate();
            listView_.View = View.Details;

            listView_.Columns.Add("로그 종류");
            listView_.Columns.Add("시간");
            listView_.Columns.Add("메시지");

            listView_.Columns[0].Width = -2;
            listView_.Columns[1].Width = 100;
            listView_.Columns[2].Width = -2;

            listView_.EndUpdate();

            string logPath = Application.StartupPath + "\\log\\";
            DirectoryInfo di = new DirectoryInfo(logPath);
            if (di.Exists == false) {
                di.Create();
            }

            fileName_ = logPath + string.Format("log_{0}.txt", DateTime.Now.ToString("yyyyMMddHHmmss"));
            logFile_ = new StreamWriter(fileName_);
            logActive_ = true;
        }

        public string getFileName()
        {
            return fileName_;
        }

        void fileWrite(Log type, string log)
        {
            string date = DateTime.Now.ToString();
            logFile_.WriteLine(string.Format("{0},{1},{2}", date, type.ToString(), log));
            logFile_.Flush();
        }

        void printLog(Log type, string log)
        {
            string date = DateTime.Now.ToString();

            ListViewItem lvi = new ListViewItem(type.ToString());
            lvi.SubItems.Add(date);
            lvi.SubItems.Add(log);
            listView_.BeginUpdate();

            listView_.Items.Add(lvi);
            listView_.EnsureVisible(listView_.Items.Count - 1);

            listView_.EndUpdate();

            fileWrite(type, log);
        }

        // 로그를 출력합니다.
        public void print(Log type, string format, params Object[] args)
        {
            if (logActive_ == false) {
                this.consolePrint(format, args);
                return;
            }
            // API조회 로그가 너무 많아서 분석에 방해됨.
            if (type == Log.API조회) {
                this.consolePrint(format, args);
                return;
            }

            string message = String.Format(format, args);

            //파일로 기록 남기고, list출력
            this.consolePrint(format, args);
            fileWrite(type, message);

            if (listView_.InvokeRequired) {
                listView_.BeginInvoke(new Action(() => printLog(type, message)));
            }
            else {
                printLog(type, message);
            }
        }

        public void consolePrint(string format, params Object[] args)
        {
            string message = String.Format(format, args);
            Console.WriteLine("{0}\t{1}", DateTime.Now.ToString(), message);
        }
    }
}
