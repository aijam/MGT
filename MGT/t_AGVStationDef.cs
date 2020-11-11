namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_AGVStationDef
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int StationID { get; set; }

        [StringLength(20)]
        public string StationName { get; set; }

        [StringLength(20)]
        public string Type { get; set; }

        public int? GroupId { get; set; }

        public int? Priority { get; set; }

        public bool? is_Device { get; set; }

        public bool? allow_deposit { get; set; }
    }
}
