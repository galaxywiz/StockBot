using System.Net;
using System.Net.Mail;

namespace StockBot.Util
{
    class Mail
    {
        //smtp 서버 설정
        SmtpClient smtp_ = new SmtpClient("smtp.gmail.com", 587);
        string fromMailAddr_ = "구글 계정";
        string passwd_ = "패스워드";
        string toMailAddr_;
        string subject_;
        string body_;
        string attacheFile_ = "";

        public Mail()
        {
            smtp_.EnableSsl = true;
            smtp_.Credentials = new NetworkCredential(fromMailAddr_, passwd_);
        }

        public void setToMailAddr(string str)
        {
            toMailAddr_ = str;
        }

        public void setSubject(string str)
        {
            subject_ = str;
        }

        public void setBody(string str)
        {
            body_ = str;
        }

        public void setAttachFile(string str)
        {
            attacheFile_ = str;
        }

        public void send()
        {
#if DEBUG
#else
            //메일 내용 쓰기
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress(fromMailAddr_);
            if (toMailAddr_.Length == 0) {
                toMailAddr_ = fromMailAddr_;
            }
            mail.To.Add(toMailAddr_);
            mail.Subject = subject_;
            mail.Body = body_;

            // 파일 첨부
            if (attacheFile_.Length != 0) {
                System.Net.Mail.Attachment attachement;
                attachement = new System.Net.Mail.Attachment(attacheFile_);
                mail.Attachments.Add(attachement);
            }

            try {
                smtp_.Send(mail);
                Logger.getInstance.print(Log.주식봇, "메일 전송");
            }
            catch (SmtpException ex) {
                Logger.getInstance.print(Log.에러, ex.Message);
            }
#endif
        }
    }
}
