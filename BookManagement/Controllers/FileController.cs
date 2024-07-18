using BookManagement.Contexts;
using BookManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;

namespace BookManagement.Controllers
{
    //[Authorize(Policy = "AdminOnly")]
    [Route("[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly BMContext _context;
        private readonly string FolderPath = "Books\\";
        public FileController(BMContext context)
        {
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(FileModel file)
        {
            if (file == null || file.formFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var book = _context.Books.FirstOrDefault(b => b.Name == file.Name);

            if (book == null)
            {
                return BadRequest("Book does not exist!");
            }

            var bookPath = Path.Combine(FolderPath, book.Name);

            if (CheckFile(bookPath))
            {
                return BadRequest("Book file already exist, use update instead!");
            }

            var fileExtension = Path.GetExtension(file.formFile.FileName);
            var filePath = Path.Combine(bookPath, Path.GetTempFileName() + fileExtension);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.formFile.CopyToAsync(stream);
            }
            await SplitAndSavePdfPages(filePath, bookPath);

            book.Type = fileExtension;
            _context.Update(book);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "File uploaded successfully.", Path = bookPath });
        }

        [HttpGet("{bookId}/{pageIndex}")]
        public async Task<IActionResult> GetFilePage(int bookId, int pageIndex)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            var bookPath = Path.Combine(FolderPath, book.Name);
            if (!Directory.Exists(bookPath))
            {
                return NotFound("Book folder not found.");
            }

            var filePattern = $"{pageIndex}.*";
            var files = Directory.GetFiles(bookPath, filePattern);

            if (files.Length == 0)
            {
                return NotFound("Page not found.");
            }

            var filePath = files[0];
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = GetContentType(filePath);
            return File(memory, contentType, Path.GetFileName(filePath));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return BadRequest("Book not found");
            }
            Directory.Delete(Path.Combine(FolderPath, book.Name), true);
            return Ok($"File of {book.Name} has been deleted");
        }

        [HttpPut("{bookId}/{pageIndex}")]
        public async Task<IActionResult> UpdateFilePage(int bookId, int pageIndex, IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);

            if (book == null)
            {
                return NotFound("Book not found.");
            }

            var bookPath = Path.Combine(FolderPath, book.Name);

            if (!Directory.Exists(bookPath))
            {
                return NotFound("Book folder not found.");
            }

            var newFileExtension = Path.GetExtension(formFile.FileName);
            if (newFileExtension != book.Type) return BadRequest($"File must be the same type with book ({book.Type})");

            var filePattern = $"{pageIndex}{book.Type}";
            var files = Directory.GetFiles(bookPath, filePattern);
            var filePath = Path.Combine(bookPath, filePattern);
            System.IO.File.Delete(filePath);


            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            return Ok(new { Message = "File updated successfully.", FilePath = filePath });
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }
        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
        {
            { ".pdf", "application/pdf" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
            // Thêm các loại mime type khác nếu cần
        };
        }
        /// <summary>
        /// Check if folder has file or not
        /// </summary>
        /// <param name="inputFolderName"></param>
        /// <returns>true if folder has file, false if folder emty.</returns>
        public bool CheckFile(string inputFolderName)
        {
            if (Directory.Exists(inputFolderName))
            {
                var fileCount = Directory.GetFiles(inputFolderName).Length;
                if (fileCount > 0) return true;
            }
            return false;
        }
        private async Task SplitAndSavePdfPages(string filePath, string outputFolder)
        {
            using (var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import))
            {
                for (int pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
                {
                    var outputDocument = new PdfDocument();
                    outputDocument.AddPage(inputDocument.Pages[pageIndex]);

                    var outputFilePath = Path.Combine(outputFolder, $"{pageIndex + 1}.pdf");
                    outputDocument.Save(outputFilePath);
                }
            }

            // Delete the original file after splitting
            System.IO.File.Delete(filePath);
        }

    }
}

