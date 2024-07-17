using BookManagement.Contexts;
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
        [HttpPost("newBook")]
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
                Type = bookModel.Type,
            };
            Directory.CreateDirectory(path);
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return Ok(book);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateBookAsync([FromBody] BookUpdateModel bookUpdate)
        {
            if (bookUpdate.bookModel == null) return BadRequest("Book cannot be null.");

            var book = _context.Books.FirstOrDefault(b => b.Name == bookUpdate.bookModel.Name);

            if (book == null) return BadRequest("Book not found.");

            book.Name = bookUpdate.bookModel.Name;
            book.Artist = bookUpdate.bookModel.Artist;
            book.Type = bookUpdate.bookModel.Type;
            _context.Update(book);
            await _context.SaveChangesAsync();

            ModifyHistory modifyHistory = new ModifyHistory
            {
                BookId = book.Id,
                ModifiedBy = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                ModifiedAt = DateTime.Now,
                ChangeDescription = bookUpdate.description
            };

            _context.ModifyHistories.Add(modifyHistory);
            await _context.SaveChangesAsync();

            return Ok(bookUpdate);
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
    }
}
