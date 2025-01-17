﻿using BookManagement.Contexts;
using BookManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookManagement.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly BMContext _context;
        private readonly string FolderPath = "Books\\";
        public BookController(BMContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAllBooks()
        {
            var books = _context.Books;
            if (books == null) return NotFound("No book found.");
            List<BookModel> bookModels = new List<BookModel>();
            foreach (var book in books)
            {
                bookModels.Add(new BookModel
                {
                    Id = book.Id,
                    Name = book.Name,
                    Artist = book.Artist,
                    CreateAt = book.CreateAt,
                    Type = book.Type
                });
            }
            return Ok(bookModels);
        }

        [HttpGet("{id}")]
        public IActionResult GetBook(int id)
        {
            return Ok(_context.Books.FirstOrDefault(b => b.Id == id));
        }

        [HttpGet("search")]
        public IActionResult Search(string field, string value)
        {
            if (value == null) return BadRequest("Value cannot be emty.");
            if (field == null) return BadRequest("Field cannot be emty.");
            switch (field)
            {
                case "Name":
                    return Ok(_context.Books.Where(b => b.Name.Contains(value)));
                case "Artist":
                    return Ok(_context.Books.Where(b => b.Artist.Contains(value)));
                case "Type":
                    return Ok(_context.Books.Where(b => b.Type == value));
                default: return BadRequest("This field is not exist.");
            }
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> NewBookAsync([FromBody] BookModel bookModel)
        {
            if (_context.Books.FirstOrDefault(b => b.Name == bookModel.Name) != null) return BadRequest("Book already exist!");
            if (bookModel == null)
            {
                return BadRequest();
            }
            var path = Path.Combine(FolderPath, bookModel.Name);
            var book = new Book
            {
                Name = bookModel.Name,
                Artist = bookModel.Artist,
                CreateAt = DateTime.Now,
                CreateBy = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                Type = bookModel.Type,
            };
            Directory.CreateDirectory(path);
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return Ok(book);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookAsync(int id, BookModel bookModel)
        {
            if (bookModel == null) return BadRequest("Book cannot be null.");

            var book = _context.Books.FirstOrDefault(b => b.Id == id);

            if (book == null) return BadRequest("Book not found.");

            book.Name = bookModel.Name;
            book.Artist = bookModel.Artist;
            book.Type = bookModel.Type;
            _context.Update(book);
            await _context.SaveChangesAsync();

            var userId = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            ModifyHistory modifyHistory = new ModifyHistory
            {
                BookId = book.Id,
                ModifiedBy = userId,
                ModifiedAt = DateTime.Now,
                ChangeDescription = $"Update at {DateTime.Now} by user {userId}"
            };

            _context.ModifyHistories.Add(modifyHistory);
            await _context.SaveChangesAsync();

            return Ok(book);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookAsync(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return BadRequest("Book not found");
            }
            Directory.Delete(Path.Combine(FolderPath, book.Name), true);
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return Ok($"{book.Name} has been deleted");
        }

        [Authorize]
        [HttpPost("Bookmark/{bookId}/{page}")]
        public async Task<IActionResult> Bookmark(int bookId, int page)
        {
            if (bookId == 0 || page == 0) 
                return BadRequest("Value invalid!");
            var book = _context.Books.FirstOrDefault(book => book.Id == bookId);
            if (book == null) return BadRequest("Book not found.");
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var bookmark = _context.UserBookmarks.FirstOrDefault(b => b.BookId == bookId && b.UserId == int.Parse(userId));
            if (bookmark == null)
            {
                var nbookmark = new UserBookmark
                {
                    BookId = bookId,
                    UserId = int.Parse(userId),
                    Page = page
                };
                _context.UserBookmarks.Add(nbookmark);
                await _context.SaveChangesAsync();
            }
            else
            {
                bookmark.Page = page;
                _context.UserBookmarks.Update(bookmark);
                await _context.SaveChangesAsync();
            }
            return Ok($"Updated bookmark {book.Name} at page {page}.");
        }

        [Authorize]
        [HttpGet("Bookmark/{bookId}")]
        public async Task<IActionResult> GetBookmark(int bookId)
        {
            if (bookId == 0)
                return BadRequest("Value invalid!");
            var book = _context.Books.FirstOrDefault(book => book.Id == bookId);
            if (book == null) return BadRequest("Book not found.");
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var bookmark = _context.UserBookmarks.FirstOrDefault(b => b.BookId == bookId && b.UserId == int.Parse(userId));
            if (bookmark == null) return NotFound("No bookmark found.");
            return Ok(bookmark.Page);
        }
    }
}
