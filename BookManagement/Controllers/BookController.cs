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
        [HttpPut("modify")]
        public async Task<IActionResult> ModifyBookAsync([FromBody] BookModify bookModify)
        {
            if (bookModify.bookModel == null) return BadRequest("Book cannot be null.");

            var book = _context.Books.FirstOrDefault(b => b.Name == bookModify.bookModel.Name);

            if (book == null) return BadRequest("Book not found.");

            book.Name = bookModify.bookModel.Name;
            book.Artist = bookModify.bookModel.Artist;
            book.Type = bookModify.bookModel.Type;
            _context.Update(book);
            await _context.SaveChangesAsync();

            ModifyHistory modifyHistory = new ModifyHistory
            {
                BookId = book.Id,
                ModifiedBy = int.Parse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                ModifiedAt = DateTime.Now,
                ChangeDescription = bookModify.description
            };

            _context.ModifyHistories.Add(modifyHistory);
            await _context.SaveChangesAsync();

            return Ok(bookModify);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete]
        public async Task<IActionResult> DeleteBookAsync(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return BadRequest("Book not found");
            }
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return Ok("Deleted");
        }
    }
}
