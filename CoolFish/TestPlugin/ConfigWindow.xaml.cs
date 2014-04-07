using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TestPlugin
{
    /// <summary>
    /// Interaction logic for CondigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();
        }

        private void SaveBTN_OnClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PulsesTB.Text, out TestPlugin.Pulses ))
            {
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid number of pulses!");
            }
        }
    }
}
