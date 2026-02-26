using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Models;

namespace OnlineLibrary.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<Genre> Genres { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Настраиваем связь "Аренда" (Один пользователь -> Много взятых книг)
        modelBuilder.Entity<Book>()
            .HasOne(b => b.User)
            .WithMany() // У пользователя нет отдельного списка "BorrowedBooks", поэтому оставляем пустым
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.SetNull); // Если юзер удален, книга просто освобождается

        // 2. Настраиваем связь "Избранное" (Многие-ко-многим)
        modelBuilder.Entity<User>()
            .HasMany(u => u.FavoriteBooks)
            .WithMany(b => b.FavoritedByUsers)
            .UsingEntity(j => j.ToTable("UserFavoriteBooks")); // Явно называем таблицу-мостик
    }
}