using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace homework.Functions.Entities
{
    public class ConsolidatedEntity : TableEntity
    {
        public DateTime DateOfReport
        {
            get; set;
        }

        public double MinutesOfWork { get; set; }

        public int WorkerId { get; set; }
    }
}
