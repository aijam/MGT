namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_AGVLog
    {
        public long id { get; set; }

        [StringLength(10)]
        public string logType { get; set; }

        public int? vehicleNumber { get; set; }

        [StringLength(10)]
        public string errorCode { get; set; }

        [StringLength(100)]
        public string Description { get; set; }

        [StringLength(20)]
        public string logonIP { get; set; }

        [StringLength(20)]
        public string logonUser { get; set; }

        public DateTime? time { get; set; }

        [StringLength(20)]
        public string assignmentId { get; set; }

        [StringLength(10)]
        public string jobId { get; set; }

        [StringLength(20)]
        public string TUId { get; set; }

        [StringLength(10)]
        public string AGVNum { get; set; }

        [StringLength(50)]
        public string information { get; set; }
    }
}
