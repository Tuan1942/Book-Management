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
            var filePath = Path.Combine(bookPath, file.formFile.FileName + fileExtension);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.formFile.CopyToAsync(stream);
            }

            return Ok(new { Message = "File uploaded successfully.", FilePath = filePath });
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

    }
}

