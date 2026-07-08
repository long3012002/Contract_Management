using System.Collections.Generic;

namespace demo1.DTOs
{
    public class UserImportResultDto
    {
        public string Message { get; set; } = string.Empty;
        public int AddedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<UserImportErrorDto> Errors { get; set; } = new();
    }

    public class UserImportErrorDto
    {
        public int RowIndex { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<string> ErrorMessages { get; set; } = new();
    }
}
