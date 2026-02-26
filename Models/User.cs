using System.Collections.Generic;

namespace OnlineLibrary.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Client"; 
    
    public List<Book> FavoriteBooks { get; set; } = new();
}