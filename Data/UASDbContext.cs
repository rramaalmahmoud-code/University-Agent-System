using Microsoft.EntityFrameworkCore;
using University_Agent_System.Models;

using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Diagnostics.Metrics;

namespace University_Agent_System.Data
{
    public class UASDbContext : DbContext
    {
        public UASDbContext(DbContextOptions<UASDbContext> options)
            : base(options) { }

        // DbSet for each entity
        public DbSet<user> Users { get; set; }
        public DbSet<student> Students { get; set; }
        public DbSet<userType> UserTypes { get; set; }
        public DbSet<status> Statuses { get; set; }
   

        public DbSet<nationality> Nationalities { get; set; }
        public DbSet<country> Countries { get; set; }
        public DbSet<degree> Degrees { get; set; }
        public DbSet<agent> Agents { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<country>()
          .HasKey(c => c.countryId);
            modelBuilder.Entity<country>()
               .Property(c => c.countryId)
               .ValueGeneratedOnAdd();

            modelBuilder.Entity<user>()
           .HasKey(u => u.userId);
            modelBuilder.Entity<user>()
               .Property(u => u.userId)
               .ValueGeneratedOnAdd();

            // Define primary keys for all entities
            modelBuilder.Entity<user>()
            .HasKey(u => u.userId);
            modelBuilder.Entity<user>()// Assuming 'userId' is the PK for 'user'
            .Property(u => u.userId)
            .ValueGeneratedOnAdd();
            modelBuilder.Entity<userType>().HasKey(ut => ut.userTypeId);
            modelBuilder.Entity<userType>()// Assuming 'userId' is the PK for 'user'
           .Property(ut => ut.userTypeId)
           .ValueGeneratedOnAdd();// Assuming 'userTypeId' is the PK for 'userType'
            modelBuilder.Entity<student>().HasKey(s => s.studentId);
            modelBuilder.Entity<student>()// Assuming 'userId' is the PK for 'user'
           .Property(s => s.studentId)
           .ValueGeneratedOnAdd();// Assuming 'studentId' is the PK for 'student'
            modelBuilder.Entity<nationality>().HasKey(n => n.nationalityId);
            modelBuilder.Entity<nationality>()// Assuming 'userId' is the PK for 'user'
           .Property(n => n.nationalityId)
          .ValueGeneratedOnAdd();// Assuming 'nationalityId' is the PK for 'nationality'
            modelBuilder.Entity<agent>().HasKey(a => a.agentId);
            modelBuilder.Entity<agent>()
          .Property(a => a.agentId)
         .ValueGeneratedOnAdd();// Assuming 'agentId' is the PK for 'agent'
            modelBuilder.Entity<degree>().HasKey(d => d.degreeId); // Assuming 'academicRankId' is the PK for 'academicRank'
            modelBuilder.Entity<degree>()// Assuming 'userId' is the PK for 'user'
            .Property(d => d.degreeId)
            .ValueGeneratedOnAdd();
           
            modelBuilder.Entity<status>().HasKey(st => st.statusId); // Assuming ')' is the PK for 'status'
            modelBuilder.Entity<status>()// Assuming 'userId' is the PK for 'user'
          .Property(st => st.statusId)
           .ValueGeneratedOnAdd();
           
           
           
            // Define relationships using Fluent API

            // User - UserType (Many-to-One)
            modelBuilder.Entity<user>()
                .HasOne(u => u.UserType)
                .WithMany(ut => ut.Users)
                .HasForeignKey(u => u.userTypeId)
                 .OnDelete(DeleteBehavior.NoAction); // This will prevent cascading delete

            // Student - Nationality (Many-to-One)
            modelBuilder.Entity<student>()
                .HasOne(s => s.Nationality)
                .WithMany(n => n.Students)
                .HasForeignKey(s => s.nationalityId)
           .OnDelete(DeleteBehavior.NoAction); // This will prevent cascading delete

            // Student - AcademicRank (Many-to-One)
            modelBuilder.Entity<student>()
                .HasOne(s => s.Degree)
                .WithMany(d => d.Students)
                .HasForeignKey(s => s.degreeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Student - Program (Many-to-One)
            //modelBuilder.Entity<student>()
            //    .HasOne(s => s.Program)
            //    .WithMany(p => p.Students)
            //    .HasForeignKey(s => s.programId)
            //    .OnDelete(DeleteBehavior.NoAction);

            // Student - Agent (Many-to-One)
            modelBuilder.Entity<student>()
                .HasOne(s => s.Agent)
                .WithMany(a => a.Students)
                .HasForeignKey(s => s.agentId)
                .OnDelete(DeleteBehavior.NoAction);

            // Student - Faculty (Many-to-One)
            //modelBuilder.Entity<student>()
            //    .HasOne(s => s.Faculty)
            //    .WithMany(f => f.Students)
            //    .HasForeignKey(s => s.facultyId)
            //    .OnDelete(DeleteBehavior.NoAction);

            // Student - Semester (Many-to-One)
            //modelBuilder.Entity<student>()
            //    .HasOne(s => s.Semester)
            //    .WithMany(se => se.Students)
            //    .HasForeignKey(s => s.semesterId)
            //    .OnDelete(DeleteBehavior.NoAction);


            // Student - Status (Many-to-One)
            modelBuilder.Entity<student>()
                .HasOne(s => s.Status)
                .WithMany(st => st.Students)
                .HasForeignKey(s => s.statusId)
                .OnDelete(DeleteBehavior.NoAction);

            // Agent - User (Many-to-One)
            modelBuilder.Entity<agent>()
                .HasOne(a => a.User)
                .WithMany(u => u.Agents)
                .HasForeignKey(a => a.userId)
                .OnDelete(DeleteBehavior.NoAction);
            // Agent - Nationality (Many-to-One)
            modelBuilder.Entity<agent>()
                .HasOne(a => a.Nationality)
                .WithMany(n => n.Agents)
                .HasForeignKey(a => a.nationalityId)
                .OnDelete(DeleteBehavior.NoAction);
            //Agent-Country  (Many-to-One)

            modelBuilder.Entity<agent>()
              .HasOne(a => a.Country)
              .WithMany(c => c.Agents)
              .HasForeignKey(a => a.countryId)
              .OnDelete(DeleteBehavior.NoAction);

            //Student-Country  (Many-to-One)

            modelBuilder.Entity<student>()
              .HasOne(s => s.Country)
              .WithMany(c=> c.Students)
              .HasForeignKey(s => s.countryId)
              .OnDelete(DeleteBehavior.NoAction);

            //    modelBuilder.Entity<program>()
            // .HasOne(p => p.Faculty)
            //.WithMany(f => f.Programs)
            //.HasForeignKey(p => p.facultyId)
            //.OnDelete(DeleteBehavior.NoAction);


        }


    }
}
