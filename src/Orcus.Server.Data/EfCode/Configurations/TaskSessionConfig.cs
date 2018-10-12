﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcus.Server.Data.EfClasses.Tasks;

namespace Orcus.Server.Data.EfCode.Configurations
{
    internal class TaskSessionConfig : IEntityTypeConfiguration<TaskSession>
    {
        public void Configure(EntityTypeBuilder<TaskSession> builder)
        {
            builder.HasIndex(x => new {x.CapturedTaskId, x.Name}).IsUnique();
        }
    }
}