using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using NLog;

namespace Conduktor
{
    static class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
#if (!DEBUG)
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new MainService() 
			};
            ServiceBase.Run(ServicesToRun);
#else
                MainService service = new MainService();
                service.Start();
#endif
            }
            catch (Exception ex)
            {
                logger.ErrorException("Unhandled exception in MainService.", ex);
            }
        }
    }
}
