using System.ComponentModel.DataAnnotations;

namespace ErpConsoleApp.Database.Models
{
    public class AppSetting
    {
        [Key]
        public string Key { get; set; } // e.g., "LoginPin"
        public string Value { get; set; }
    }
}