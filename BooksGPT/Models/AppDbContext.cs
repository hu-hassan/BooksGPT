using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Collections.Generic;
using BooksGPT.Models;

namespace BooksGPT.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<EmailVerificationsModel> EmailVerifications { get; set; }
        public DbSet<ChatHistoryModel> ChatHistory { get; set; }
        // Add other DbSets for your models here
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>()
                .ToTable("users")
                .HasIndex(u => u.Username)
                .IsUnique();
            
            modelBuilder.Entity<ChatHistoryModel>()
                .Property(e => e.UserQuestions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

            modelBuilder.Entity<ChatHistoryModel>()
                .Property(e => e.BotAnswers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

            base.OnModelCreating(modelBuilder);
        }

    }
}