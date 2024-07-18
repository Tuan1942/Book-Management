using BookManagement.Contexts;
using BookManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using DocumentFormat.OpenXml.Packaging;
using BookManagement.Hubs;
using Microsoft.AspNetCore.SignalR;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BookManagement.Controllers
{
    //[Authorize(Policy = "AdminOnly")]
    [Route("[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly BMContext _context;
        private readonly string FolderPath = "Books\\";
        private delegate Task<IActionResult> UpdateFile();
        private readonly IHubContext<NotifyHub> _hubContext;
        public FileController(BMContext context, IHubContext<NotifyHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> Upload(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var book = _context.Books.FirstOrDefault(b => b.Id == id);

            if (book == null)
            {
                return BadRequest("Book does not exist!");
            }

            var bookPath = Path.Combine(FolderPath, book.Name);

            if (Directory.EnumerateFiles(bookPath).Any())
            {
                return BadRequest("Book file already exist, use update instead!");
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var filePath = Path.Combine(bookPath, Path.GetRandomFileName() + fileExtension);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            switch (fileExtension.ToLower())
            {
                case ".pdf":
                    SplitAndSavePdfPages(filePath, bookPath);
                    break;
                case ".doc":
                case ".docx":
                    break;
                case ".xlsx":
                default: return BadRequest($"File type { fileExtension } not supported.");
            }

            book.Type = fileExtension;
            _context.Update(book);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "File uploaded successfully.", Path = bookPath });
        }
        [HttpPost("raw")]
        public async Task<IActionResult> Upload1(FileModel file)
        {
            if (file == null || file.formFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var bookPath = Path.Combine(FolderPath, "Testing");

            if (Directory.EnumerateFiles(bookPath).Any())
            {
                return BadRequest("Book file already exist, use update instead!");
            }

            var fileExtension = Path.GetExtension(file.formFile.FileName);
            var filePath = Path.Combine(bookPath, "Testing" + fileExtension);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.formFile.CopyToAsync(stream);
            }

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

            if (await CheckFilePage(formFile) != 1) return BadRequest("Page must be '1'");

            System.IO.File.Delete(filePath);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            await NotifyHub.NotifyBookUpdate(bookId.ToString(), _hubContext);

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
        //public bool CheckFile(string inputFolderName)
        //{
        //    if (Directory.Exists(inputFolderName))
        //    {
        //        var fileCount = Directory.GetFiles(inputFolderName).Length;
        //        if (fileCount > 0) return true;
        //    }
        //    return false;
        //}
        private void SplitAndSavePdfPages(string filePath, string outputFolder)
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

            System.IO.File.Delete(filePath);
        }
        private void SaveWordFile(string filePath, string outputFolder)
        {
        }

        private async Task<int> CheckFilePage(IFormFile formFile)
        {
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            var extension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
            int pageCount;
            switch (extension)
            {
                case ".pdf":
                    pageCount = PdfReader.Open(tempFilePath, PdfDocumentOpenMode.Import).PageCount;
                    break;
                case ".doc":
                case ".docx":
                    pageCount = CountDocxPages(tempFilePath);
                    break;
                case ".xlsx":
                    pageCount = CountXlsxPages(tempFilePath);
                    break;
                default:
                    pageCount = 0;
                    break;
            }

            System.IO.File.Delete(tempFilePath);
            return pageCount;
        }

        private int CountDocxPages(string filePath)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
            {
                int pageCount = doc.ExtendedFilePropertiesPart.Properties.Pages.Text != null ?
                    int.Parse(doc.ExtendedFilePropertiesPart.Properties.Pages.Text) : 0;
                return pageCount;
            }
        }

        private int CountXlsxPages(string filePath)
        {
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
            {
                return doc.WorkbookPart.Workbook.Sheets.Count();
            }
        }
    }
}

