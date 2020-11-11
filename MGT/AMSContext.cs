namespace MGT
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class AMSContext : DbContext
    {
        public AMSContext()
            : base("name=AMSContext")
        {
        }

        public virtual DbSet<t_Station> t_Station { get; set; }
        public virtual DbSet<t_AGVCharge> t_AGVCharge { get; set; }
        public virtual DbSet<t_AGVLog> t_AGVLog { get; set; }
        public virtual DbSet<t_AGVPath> t_AGVPath { get; set; }
        public virtual DbSet<t_AGVStationDef> t_AGVStationDef { get; set; }
        public virtual DbSet<t_AGVWork> t_AGVWork { get; set; }
        public virtual DbSet<t_AGVWork2> t_AGVWork2 { get; set; }
        public virtual DbSet<t_AGVWorkHist> t_AGVWorkHist { get; set; }
        public virtual DbSet<t_Code> t_Code { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<t_Station>()
                .Property(e => e.MaterialType)
                .IsUnicode(false);

            modelBuilder.Entity<t_Station>()
                .Property(e => e.OPCaddress)
                .IsUnicode(false);

            modelBuilder.Entity<t_AGVLog>()
                .Property(e => e.AGVNum)
                .IsFixedLength();

            modelBuilder.Entity<t_Code>()
                .Property(e => e.codetype)
                .IsFixedLength();

            modelBuilder.Entity<t_Code>()
                .Property(e => e.name)
                .IsFixedLength();
        }
    }
}
