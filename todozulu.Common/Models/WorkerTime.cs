using System;
using System.Collections.Generic;
using System.Text;

namespace homework.Common.Models
{
    public class WorkerTime
    {
        public int WorkerId { get; set; }
        public DateTime DateOfReport { get; set; }

        public int TypeOfReport { get; set; }

        public bool Consolidated { get; set; }



    }
}
