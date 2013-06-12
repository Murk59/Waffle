﻿namespace CommandProcessing.Data.Tests
{
    using System.Data.Common;
    using System.Data.Entity;

    public class FakeDbContext : DbContext
    {
        public FakeDbContext(DbConnection connection)
            : base(connection, true)
        {
        }

        public DbSet<FakeEntity> Entities { get; set; }
    }
}