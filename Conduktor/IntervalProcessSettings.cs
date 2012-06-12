using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Conduktor
{
    public class IntervalProcessSettings
    {
        public string Filename { get; set; }
        public string Arguments { get; set; }
        public TimeSpan TimerDuration { get; set; }
        public TimeSpan KillAfter { get; set; }
    }
}
