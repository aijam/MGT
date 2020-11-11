namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_AGVWorkHist
    {
        public long Id { get; set; }

        [StringLength(50)]
        public string JobId { get; set; }

        [StringLength(50)]
        public string JobIndex { get; set; }

        [StringLength(50)]
        public string JobType { get; set; }

        [StringLength(50)]
        public string BarCode { get; set; }

        [StringLength(50)]
        public string Origination { get; set; }

        [StringLength(50)]
        public string Destination { get; set; }

        [StringLength(50)]
        public string Priority { get; set; }

        [StringLength(50)]
        public string JobStatus { get; set; }

        [StringLength(50)]
        public string TUType { get; set; }

        [StringLength(50)]
        public string VehicleNum { get; set; }

        [StringLength(50)]
        public string IssueTime { get; set; }

        [StringLength(50)]
        public string AssignTime { get; set; }

        [StringLength(50)]
        public string FetchTime { get; set; }

        [StringLength(50)]
        public string AGVCancelFlag { get; set; }

        [StringLength(50)]
        public string WMSCancelFlag { get; set; }

        [StringLength(50)]
        public string RedirectFlag { get; set; }

        [StringLength(50)]
        public string RedirectPosition { get; set; }

        [StringLength(50)]
        public string CancelStatus { get; set; }

        [StringLength(50)]
        public string CancelTrigger { get; set; }

        [StringLength(50)]
        public string CancelFlag { get; set; }

        public DateTime? FinishTime { get; set; }
    }
}
