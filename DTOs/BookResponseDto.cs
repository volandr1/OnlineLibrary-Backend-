namespace OnlineLibrary.DTOs;

public class BookResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? UserId { get; set; }
    
    public List<string> Authors { get; set; } = new();
    public List<string> Genres { get; set; } = new();
}