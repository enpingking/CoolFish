using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace CoolFish
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Updater.Update().Wait();
                Application.Run(new MainWindow());

            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error has occurred. Please send a screenshot to the developer \n" + ex);
            }
        }
    }
}
