namespace MGT
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class t_Code
    {
        public int ID { get; set; }

        [StringLength(10)]
        public string codetype { get; set; }

        [StringLength(10)]
        public string name { get; set; }

        [StringLength(10)]
        public string code { get; set; }
    }
}
