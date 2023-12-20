using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageRate
{
    internal class ExternalActionsUtil
    {
        public static void ShowInExplorer(string path)
        {
            Process.Start("explorer.exe", "/select," + path);
        }

        public static void OpenWithDialog(string path)
        {
            Process proc = new Process();
            proc.EnableRaisingEvents = false;
            proc.StartInfo.FileName = "rundll32.exe";
            proc.StartInfo.Arguments = "shell32,OpenAs_RunDLL " + path;
            proc.Start();
        }
    }
}
