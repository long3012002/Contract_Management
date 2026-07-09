using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class DieuChinhDuAnDto : IHasId
{
    public Guid Id { get; set; }
    public Guid DuAnId { get; set; }
    public decimal GiaTriDieuChinh { get; set; }
    public string LyDoDieuChinh { get; set; } = string.Empty;
    public DateTime NgayDieuChinh { get; set; }
}

public class CreateDieuChinhDuAnDto
{
    [Required]
    public decimal GiaTriDieuChinh { get; set; }

    [Required]
    [StringLength(1000)]
    public string LyDoDieuChinh { get; set; } = string.Empty;
}
