namespace BookManagement.Models
{
    public class FileModel
    {
        public IFormFile formFile { get; set; }
        public string Name { get; set; }
    }
    public class FileUpdate
    {
        public IFormFile formFile { get; set; }
        public string Name { get; set; }
        public int Page { get; set; }
    }
}
