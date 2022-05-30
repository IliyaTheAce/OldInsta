using System;
using System.IO;
namespace Insta_DM_Bot_server_wpf
{
    internal static class Debug
    {
        public static void Log(string log)
        {
            var logText = "\n " + DateTime.Now + ": \n" + log;
            File.AppendAllText("./Log/Log.txt", logText);
        }
        public static void CleanTempFolder()
        {
            string tempFolderPath = Path.GetTempPath();
            string[] tempFiles = Directory.GetFiles(tempFolderPath, "*.*", SearchOption.AllDirectories);
            foreach (string file in tempFiles)
            {
                try
                {
                    FileInfo currentFile = new FileInfo(file);
                    currentFile.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error on file: {0}\r\n   {1}", file, ex.Message);
                }
            }
        }
    }
}
