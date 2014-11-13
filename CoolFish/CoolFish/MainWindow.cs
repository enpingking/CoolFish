using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CoolFish
{
    public partial class MainWindow : Form
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Injector _injector;
        private Process[] _processes;
        public MainWindow()
        {
            this._injector = new Injector();
            InitializeComponent();
        }

        private void ProcessCB_DropDown(object sender, EventArgs e)
        {
            ProcessCB.Items.Clear();
            _processes = Process.GetProcessesByName("Wow");
            if (_processes.Any())
            {
                var builder = new StringBuilder();
                foreach (var process in _processes)
                {
                    builder.Append("ProcessID: ");
                    builder.Append(process.Id);
                    try
                    {
                        var name = process.ProcessName;
                        builder.Append(" | Process name: ");
                        builder.Append(name);
                    }
                    catch (Exception)
                    {}

                    try
                    {
                        var title = process.MainWindowTitle;
                        builder.Append(" | Main Title: ");
                        builder.Append(title);
                    }
                    catch (Exception)
                    {}
                    ProcessCB.Items.Add(builder.ToString());
                    builder.Clear();
                }
            }
        }

        private void AttachBTN_Click(object sender, EventArgs e)
        {
            var index = ProcessCB.SelectedIndex;

            if (index < 0 || index >= _processes.Length)
            {
                Logger.Warn("Please select a valid process from the dropdown");
                return;
            }

            var process = _processes[index];
            if (process.HasExited)
            {
                Logger.Warn("The process you have selected has exited. Please select another.");
                return;
            }

            try
            {
                Logger.Info("Attempting to inject into process: " + process.Id);
                Task.Run(() => _injector.Inject(process));
            }
            catch (Exception ex)
            {
               Logger.Error("Exception thrown while injecting to the selected process", ex);
            }
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            var configuration = new LoggingConfiguration();

            var target = new RichTextBoxTarget { AutoScroll = true, ControlName = OutputRTB.Name, FormName = this.Name, Layout = @"[${date:format=h\:mm\:ss.ffff tt}] [${level:uppercase=true}] ${message}" };
            configuration.AddTarget("MainWindow", target);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, target));
            LogManager.Configuration = configuration;
            LogManager.ReconfigExistingLoggers();
            Logger.Info("CoolFish Version: "+ Constants.Version.Value);
        }
    }
}
