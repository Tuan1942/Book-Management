using Microsoft.EntityFrameworkCore;
using System;

namespace BookManagement.Contexts
{
    public class BMContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<ModifyHistory> ModifyHistories { get; set; }
        public DbSet<UserBookmark> UserBookmarks { get; set; }

        public BMContext(DbContextOptions<BMContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);

                entity.HasMany(e => e.UserRoles)
                      .WithOne(ur => ur.User)
                      .HasForeignKey(ur => ur.UserId);

                entity.HasMany(e => e.ModifyHistories)
                      .WithOne(mh => mh.User)
                      .HasForeignKey(mh => mh.ModifiedBy);

                entity.HasMany(e => e.UserBookmarks)
                      .WithOne(ub => ub.User)
                      .HasForeignKey(ub => ub.UserId);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

                entity.HasMany(e => e.UserRoles)
                      .WithOne(ur => ur.Role)
                      .HasForeignKey(ur => ur.RoleId);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId);
            });

            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Artist).HasMaxLength(100);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.CreateAt).IsRequired();

                entity.HasMany(e => e.ModifyHistories)
                      .WithOne(mh => mh.Book)
                      .HasForeignKey(mh => mh.BookId);

                entity.HasMany(e => e.UserBookmarks)
                      .WithOne(ub => ub.Book)
                      .HasForeignKey(ub => ub.BookId);
            });

            modelBuilder.Entity<ModifyHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ModifiedAt).IsRequired();
                entity.Property(e => e.ChangeDescription).IsRequired().HasMaxLength(500);

                entity.HasOne(mh => mh.Book)
                      .WithMany(b => b.ModifyHistories)
                      .HasForeignKey(mh => mh.BookId);
            });

            modelBuilder.Entity<UserBookmark>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Page);

                entity.HasOne(ub => ub.User)
                      .WithMany(u => u.UserBookmarks)
                      .HasForeignKey(ub => ub.UserId);

                entity.HasOne(ub => ub.Book)
                      .WithMany(b => b.UserBookmarks)
                      .HasForeignKey(ub => ub.BookId);
            });
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "User" }
    );
        }
    }
    #region User
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<ModifyHistory> ModifyHistories { get; set; }
        public ICollection<UserBookmark> UserBookmarks { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
    #endregion
    #region Book
    public class Book
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Type { get; set; }
        public DateTime CreateAt { get; set; }
        public ICollection<ModifyHistory> ModifyHistories { get; set; }
        public ICollection<UserBookmark> UserBookmarks { get; set; }
    }

    #endregion
    #region Link
    public class UserBookmark
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public int Page { get; set; } = 0;

        public User User { get; set; }
        public Book Book { get; set; }
    }
    public class ModifyHistory
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string? ChangeDescription { get; set; }

        public Book Book { get; set; }
        public User User { get; set; }
    }
    #endregion
}
