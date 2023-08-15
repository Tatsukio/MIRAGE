using System.Windows.Forms;

namespace PWKiller
{
    static class Program
    {
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PWKillerMain(args));
        }
    }
}
