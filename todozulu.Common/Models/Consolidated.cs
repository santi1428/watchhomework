using System;
using System.Collections.Generic;
using System.Text;

namespace homework.Common.Models
{
    public class Consolidated
    {
        public DateTime DateOfReport
        {
            get; set;
        }

        public double MinutesOfWork { get; set; }

        public int WorkerId { get; set; }
    }
}
