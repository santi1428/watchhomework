using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace homework.Functions.Entities
{
    public class WorkerTimeEntity : TableEntity
    {
        public int WorkerId { get; set; }

        public DateTime DateOfReport { get; set; }

        public int TypeOfReport { get; set; }

        public bool Consolidated { get; set; }

    }
}
