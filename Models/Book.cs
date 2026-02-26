namespace OnlineLibrary.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Для логики "взять книгу" (1 пользователь на 1 книгу)
    public int? UserId { get; set; } 
    public User? User { get; set; }

    // Твои связи Many-to-Many
    public List<Author> Authors { get; set; } = new();
    public List<Genre> Genres { get; set; } = new();
    
    public List<User> FavoritedByUsers { get; set; } = new();
}