using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Conduktor
{
    public partial class MainService : ServiceBase
    {
        private List<Task> currentTasks = new List<Task>();

        public MainService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is exposed to enable immediate debugging without using an installer or post build steps to install and attach to the process
        /// refer to the Main() method in Program.cs to see how the debug flag is used to start the service code outside of being run as a service
        /// </summary>
        public void Start()
        {
            LoadProcesses();

            Task.WaitAll(currentTasks.ToArray());
        }

        private void LoadProcesses()
        {

            XDocument processConfig = GetProcessesXml();

            IEnumerable<XElement> intervalProcesses = GetIntervalProcesses(processConfig);
            
            foreach (XElement processToStart in intervalProcesses)
            {
                IntervalProcessSettings settings = GetIntervalProcessSettings(processToStart);

                bool hasValidSettings = ValidateIntervalProcessSettings(settings);

                if (hasValidSettings)
                {
                    currentTasks.Add(Task.Factory.StartNew(() => StartIntervalProcess(settings)));
                }
            }

        }

        private XDocument GetProcessesXml()
        {
            string processConfigPath = string.Format("{0}\\config\\processes.xml", Directory.GetCurrentDirectory());
            return XDocument.Load(processConfigPath);
        }

        private IEnumerable<XElement> GetIntervalProcesses(XDocument processConfig)
        {
            return (from process in processConfig.Descendants("process")
                    where (string)process.Attribute("type") == "interval"
                    select process);
        }

        private IntervalProcessSettings GetIntervalProcessSettings(XElement process)
        {
            IntervalProcessSettings settings = new IntervalProcessSettings();
            XElement timer = process.Element("timer");
            XElement timeout = process.Element("timeout");

            settings.Filename = (string)process.Attribute("filename");
            settings.Arguments = (string)process.Attribute("arguments");
            settings.TimerDuration = CreateTimeSpan(timer);
            settings.KillAfter = CreateTimeSpan(timeout);

            return settings;
        }

        private bool ValidateIntervalProcessSettings(IntervalProcessSettings settings)
        {
            return true;
        }

        private TimeSpan CreateTimeSpan(XElement element)
        {
            return CreateTimeSpan((int)element.Attribute("days"), (int)element.Attribute("hours"), (int)element.Attribute("minutes"), (int)element.Attribute("seconds"), (int)element.Attribute("milliseconds"));
        }

        private TimeSpan CreateTimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            return new TimeSpan(days, hours, minutes, seconds);
        }

        private void StartIntervalProcess(IntervalProcessSettings settings)
        {
            TimerCallback timerCallback = RunIntervalProcess;
            var timer = new System.Threading.Timer(timerCallback, settings, new TimeSpan(0), settings.TimerDuration);

            Thread.Sleep(Timeout.Infinite);
        }

        private void RunIntervalProcess(Object stateInfo)
        {
            IntervalProcessSettings settings = (IntervalProcessSettings)stateInfo;
            RunProcess(settings.Filename, settings.Arguments, settings.KillAfter);
        }

        private void RunProcess(string filename, string arguments, TimeSpan killAfter)
        {
            Process processToRun = Process.Start(filename, arguments);

            bool alreadyClosed = processToRun.WaitForExit((int)(killAfter).TotalMilliseconds);

            if (!alreadyClosed)
            {
                bool closedSuccessfully = processToRun.CloseMainWindow();

                if (!closedSuccessfully)
                {
                    processToRun.Kill();
                }
            }
        }


        protected override void OnStart(string[] args)
        {
            Start();
        }
        
        protected override void OnStop()
        {
        }
    }
}
