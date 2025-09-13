using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PWKiller
{
    public partial class PWKillerMain : Form
    {
        public PWKillerMain(string[] args)
        {
            InitializeComponent();
            if (args.Contains("-SSSOffAfterPWExit"))
            {
                CheckPW();
            }
            if (args.Contains("-KillAll"))
            {
                ParaWorldExited();
            }
            else
            {
                if (Locale.Load())
                {
                    PWKillerTitleText.Text = Locale.PWKillerTitleText;
                    PWKillerButtonText.Text = Locale.PWKillerButtonText;
                }
            }
        }

        void MainButton_Click(object sender, EventArgs e)
        {
            KillPW();
            Process me = Process.GetCurrentProcess();
            me.Kill();
        }

        public static void CheckPW()
        {
            System.Threading.Thread.Sleep(1000);
            while (true)
            {
                if (Process.GetProcessesByName(Places.pwProcesses[0]).Length == 0)
                {
                    if (Process.GetProcessesByName(Places.pwProcesses[1]).Length == 0)
                    {
                        break;
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
            ParaWorldExited();
        }
        public static void ParaWorldExited()
        {
            if (File.Exists(Places.toolsDir + "/CfgEditor.exe"))
            {
                CfgEditor.SetS("Root/Pest/Server/UseUslCPP 1");
            }
            KillPW();
            KillPWKiller();
        }
        public static void KillPW()
        {
            for (int i = 0; i < 3; i++)
            {
                Process[] paraworldProcesses = Process.GetProcessesByName(Places.pwProcesses[i]);
                foreach (Process paraworldProcess in paraworldProcesses)
                {
                    paraworldProcess.Kill();
                }
            }
            if (Process.GetProcessesByName("ParaWorldStatus").Length != 0)
            {
                Process PWS = Process.GetProcessesByName("ParaWorldStatus").First();
                PWS.Kill();
            }
        }
        public static void KillPWKiller()
        {
            Process me = Process.GetCurrentProcess();
            Process[] AllPWKiller = Process.GetProcessesByName("PWKiller");
            foreach (Process PWKiller in AllPWKiller)
            {
                if (PWKiller.Id != me.Id)
                {
                    PWKiller.Kill();
                }
            }
            me.Kill();
        }
    }
}
