namespace BookManagement.Models
{
    public class FileModel
    {
        public int Id { get; set; }
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
