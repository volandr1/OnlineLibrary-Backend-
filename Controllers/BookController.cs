using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Data;
using OnlineLibrary.Models;
using OnlineLibrary.DTOs;
using System.Text;

namespace OnlineLibrary.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public BooksController(LibraryDbContext context)
    {
        _context = context;
    }

    // --- ПУБЛИЧНЫЕ МЕТОДЫ (Без логина) ---

    // 1. Список всех доступных книг
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetBooks()
    {
        var books = await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .ToListAsync();
        
        return Ok(MapToDto(books));
    }

    // 2. Информация по отдельной книге
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBook(int id)
    {
        var book = await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null) return NotFound("Книга не найдена.");

        return Ok(MapToDto(new List<Book> { book }).First());
    }

    // 3. Поиск книг по названию
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> SearchBooks([FromQuery] string? title)
    {
        var query = _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(b => b.Title.ToLower().Contains(title.ToLower()));
        }

        var results = await query.ToListAsync();
        return Ok(MapToDto(results));
    }

    // --- МЕТОДЫ ДЛЯ АДМИНА ---

    // 4. Добавление новой книги
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddBook(Book book)
    {
        var attachedAuthors = new List<Author>();
        foreach (var author in book.Authors)
        {
            var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.Name == author.Name);
            attachedAuthors.Add(existingAuthor ?? author);
        }
        book.Authors = attachedAuthors;

        var attachedGenres = new List<Genre>();
        foreach (var genre in book.Genres)
        {
            var existingGenre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == genre.Name);
            attachedGenres.Add(existingGenre ?? genre);
        }
        book.Genres = attachedGenres;

        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        
        return Ok(MapToDto(new List<Book> { book }).First());
    }

    // 5. Удаление книги
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null) return NotFound("Книга не найдена.");

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        return Ok($"Книга с ID {id} успешно удалена.");
    }

    // --- МЕТОДЫ ПОЛЬЗОВАТЕЛЯ (Нужен логин) ---

    // 6. Взять книгу (Аренда)
    [HttpPost("take/{bookId}")]
    public async Task<IActionResult> TakeBook(int bookId)
    {
        var userId = GetCurrentUserId();
        var book = await _context.Books.FindAsync(bookId);
        
        if (book == null) return NotFound("Книга не найдена.");
        if (book.UserId != null) return BadRequest("Книга уже занята другим пользователем.");

        book.UserId = userId; 
        await _context.SaveChangesAsync();
        return Ok($"Книга '{book.Title}' успешно выдана вам.");
    }

    // 7. Добавить в избранное
    [HttpPost("favorites/{bookId}")]
    public async Task<IActionResult> AddToFavorites(int bookId)
    {
        var userId = GetCurrentUserId();
        
        
        var user = await _context.Users
            .Include(u => u.FavoriteBooks)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        var book = await _context.Books.FindAsync(bookId);

        if (user == null || book == null) return NotFound("Пользователь или книга не найдены.");

        if (user.FavoriteBooks.Any(b => b.Id == bookId))
            return BadRequest("Книга уже в избранном.");

        user.FavoriteBooks.Add(book);
        await _context.SaveChangesAsync();

        return Ok("Добавлено в избранное.");
    }

    // 8. Удалить из избранного
    [HttpDelete("favorites/{bookId}")]
    public async Task<IActionResult> RemoveFromFavorites(int bookId)
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .Include(u => u.FavoriteBooks)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        var bookToRemove = user.FavoriteBooks.FirstOrDefault(b => b.Id == bookId);
        if (bookToRemove == null) return NotFound("Книги нет в вашем списке избранного.");

        user.FavoriteBooks.Remove(bookToRemove);
        await _context.SaveChangesAsync();

        return Ok("Книга удалена из избранного.");
    }

    // 9. Посмотреть СВОЙ список избранного
    [HttpGet("favorites")]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userId = GetCurrentUserId();
        
        var user = await _context.Users
            .Include(u => u.FavoriteBooks).ThenInclude(b => b.Authors)
            .Include(u => u.FavoriteBooks).ThenInclude(b => b.Genres)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return Ok(MapToDto(user.FavoriteBooks));
    }

    
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException();
        return int.Parse(claim.Value);
    }
    
    private IEnumerable<BookResponseDto> MapToDto(IEnumerable<Book> books)
    {
        return books.Select(b => new BookResponseDto
        {
            Id = b.Id,
            Title = b.Title,
            Description = b.Description,
            UserId = b.UserId,
            Authors = b.Authors.Select(a => a.Name).ToList(),
            Genres = b.Genres.Select(g => g.Name).ToList()
        });
    }
    
    // 10. Выгрузка всех книг в CSV формат (Только Admin)
    [Authorize(Roles = "Admin")]
    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportToCsv()
    {
        var books = await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("Id,Title,Description,Authors,Genres");

        foreach (var book in books)
        {
            var authors = string.Join("; ", book.Authors.Select(a => a.Name));
            var genres = string.Join("; ", book.Genres.Select(g => g.Name));
            var title = $"\"{book.Title.Replace("\"", "\"\"")}\"";
            var description = $"\"{book.Description.Replace("\"", "\"\"")}\"";

            builder.AppendLine($"{book.Id},{title},{description},\"{authors}\",\"{genres}\"");
        }
        
        var bom = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(builder.ToString());
        var fileData = bom.Concat(content).ToArray();

        return File(fileData, "text/csv", $"books_{DateTime.Now:yyyyMMdd}.csv");
    }
}