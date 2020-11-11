namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_Station
    {
        public int ID { get; set; }

        public int StationNo { get; set; }

        public int Sequence { get; set; }

        [Required]
        [StringLength(255)]
        public string MaterialType { get; set; }

        public int OccupiedStatus { get; set; }

        public DateTime? ModifyTime { get; set; }

        public int ChannelNo { get; set; }

        public int? ChannelType { get; set; }

        public bool? IsSensor { get; set; }

        [StringLength(255)]
        public string OPCaddress { get; set; }

        public int? ModifyProgID { get; set; }

        public int StationType { get; set; }

        public int? AvailableStatus { get; set; }
    }
}
