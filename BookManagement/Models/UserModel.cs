namespace BookManagement.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
