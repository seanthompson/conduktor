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
using NLog;

namespace Conduktor
{
    public partial class MainService : ServiceBase
    {
        private static List<Task> currentTasks = new List<Task>();
        private static Logger logger = LogManager.GetCurrentClassLogger();

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

            if (currentTasks.Count() > 0)
                Task.WaitAll(currentTasks.ToArray());
        }

        private void LoadProcesses()
        {
            try
            {
                IEnumerable<XElement> intervalProcesses = GetIntervalProcesses();

                if (intervalProcesses != null)
                {
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
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error loading processes", ex);
            }
        }

        private IEnumerable<XElement> GetIntervalProcesses()
        {
            IEnumerable<XElement> intervalProcesses = null;
            XDocument processConfig = GetProcessesXml();

            try
            {
                intervalProcesses =
                    (from process in processConfig.Descendants("process")
                     where (string)process.Attribute("type") == "interval"
                     select process);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error getting interval processes.", ex);
            }

            return intervalProcesses;
        }

        private XDocument GetProcessesXml()
        {
            XDocument processesXml = null;

            try
            {

                string processConfigPath = string.Format("{0}\\config\\processes.xml", Directory.GetCurrentDirectory());
                processesXml = XDocument.Load(processConfigPath);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error getting processes XML.", ex);
            }

            return processesXml;
        }

        private IntervalProcessSettings GetIntervalProcessSettings(XElement process)
        {
            IntervalProcessSettings settings = new IntervalProcessSettings();

            try
            {
                XElement timer = process.Element("timer");
                XElement timeout = process.Element("timeout");

                settings.Filename = (string)process.Attribute("filename");
                settings.Arguments = (string)process.Attribute("arguments");
                settings.TimerDuration = CreateTimeSpan(timer);
                settings.KillAfter = CreateTimeSpan(timeout);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error getting interval process settings.", ex);
            }

            return settings;
        }

        private bool ValidateIntervalProcessSettings(IntervalProcessSettings settings)
        {
            bool hasValidSettings = true;

            try
            {
                if (!File.Exists(settings.Filename))
                {
                    hasValidSettings = false;
                }
                
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error validating interval process settings.", ex);
                hasValidSettings = false;
            }

            return hasValidSettings;
        }

        private TimeSpan CreateTimeSpan(XElement element)
        {
            int days, hours, minutes, seconds, milliseconds = 0;

            days = element.Attribute("days") != null ? (int)element.Attribute("days") : 0;
            hours = element.Attribute("hours") != null ? (int)element.Attribute("hours") : 0;
            minutes = element.Attribute("minutes") != null ? (int)element.Attribute("minutes") : 0;
            seconds = element.Attribute("seconds") != null ? (int)element.Attribute("seconds") : 0;
            milliseconds = element.Attribute("milliseconds") != null ? (int)element.Attribute("milliseconds") : 0;

            return new TimeSpan(days, hours, minutes, seconds, milliseconds);
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
            Process processToRun = null;

            try
            {
                processToRun = Process.Start(filename, arguments);

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
            catch (Exception ex)
            {
                logger.ErrorException("Error running process.", ex);
            }
            finally
            {
                processToRun.Dispose();
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
