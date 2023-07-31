using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insta_DM_Bot_server_wpf
{
    [System.Serializable]
    public class Xpath
    {
        public Xpath()
        {
            username =  new List<string>();
            password = new List<string>();
            ErrorText = new List<string>();
            saveInfo = new List<string>();
            notification = new List<string>();
            newDirect = new List<string>();
            targetInput = new List<string>();
            selectUser = new List<string>();
            NextButtom = new List<string>();
            TextArea = new List<string>();
            allowCookies = new List<string>();
        }
        public List<string> username;
        public List<string> password;
        public List<string> ErrorText;
        public List<string> saveInfo;
        public List<string> notification;
        public List<string> newDirect;
        public List<string> targetInput;
        public List<string> selectUser;
        public List<string> NextButtom;
        public List<string> TextArea;
        public List<string> allowCookies;

    }
}
