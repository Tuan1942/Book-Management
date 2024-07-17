using BookManagement.Contexts;

namespace BookManagement.Models
{
    public class BookModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Type { get; set; }
        public DateTime? CreateAt { get; set; }
    }
    public class BookModify
    {
        public BookModel bookModel { get; set; }
        public string description { get; set; }
    }
    public class FileModel
    {
        public IFormFile formFile { get; set; }
        public string Name { get; set; }
    }
}