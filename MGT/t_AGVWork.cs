namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_AGVWork
    {
        public int ID { get; set; }

        public int? JobId { get; set; }

        public int? JobIndex { get; set; }

        public int? JobType { get; set; }

        [StringLength(50)]
        public string BarCode { get; set; }

        public int? Origination { get; set; }

        public int? Destination { get; set; }

        public int? Priority { get; set; }

        public int? JobStatus { get; set; }

        public int? TUType { get; set; }

        public int? VehicleNum { get; set; }

        public DateTime? IssueTime { get; set; }

        public DateTime? AssignTime { get; set; }

        public DateTime? FetchTime { get; set; }

        public int? AGVCancelFlag { get; set; }

        public int? WMSCancelFlag { get; set; }

        public int? RedirectFlag { get; set; }

        public int? RedirectPosition { get; set; }

        public int? CancelStatus { get; set; }

        [StringLength(50)]
        public string CancelTrigger { get; set; }

        public int? CancelFlag { get; set; }

        [StringLength(10)]
        public string OriginalTarget { get; set; }

        public int? ModifyProgID { get; set; }

        public DateTime? ModifyTime { get; set; }
    }
}
