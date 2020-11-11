namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_AGVCharge
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public int? AGVNum { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? StartTime { get; set; }

        public int? JobIndex { get; set; }
    }
}
