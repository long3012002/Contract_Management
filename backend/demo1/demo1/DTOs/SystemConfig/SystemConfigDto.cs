using System;
using System.Collections.Generic;

namespace demo1.DTOs.SystemConfig;

/// <summary>DTO trả về cho frontend khi GET config.</summary>
public class SystemConfigDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public string Group { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByUsername { get; set; }
}

/// <summary>Một item trong batch update request.</summary>
public class UpdateSystemConfigItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>Batch update request body.</summary>
public class UpdateSystemConfigBatchDto
{
    public List<UpdateSystemConfigItemDto> Items { get; set; } = new();
}

/// <summary>Response trả về khi update thành công.</summary>
public class UpdateSystemConfigResultDto
{
    public int UpdatedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Request body cho test email.</summary>
public class TestEmailDto
{
    public string ToEmail { get; set; } = string.Empty;
}
