using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace demo1.DTOs
{
    /// <summary>
    /// Cấu hình trả về cho Frontend React để khởi tạo ONLYOFFICE DocumentEditor.
    /// </summary>
    public class OnlyOfficeConfigDto
    {
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = "word";

        [JsonPropertyName("document")]
        public DocumentInfo Document { get; set; } = new();

        [JsonPropertyName("editorConfig")]
        public EditorConfigInfo EditorConfig { get; set; } = new();

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    public class DocumentInfo
    {
        [JsonPropertyName("fileType")]
        public string FileType { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("permissions")]
        public DocumentPermissions Permissions { get; set; } = new();
    }

    public class DocumentPermissions
    {
        [JsonPropertyName("comment")]
        public bool Comment { get; set; } = true;

        [JsonPropertyName("copy")]
        public bool Copy { get; set; } = true;

        [JsonPropertyName("download")]
        public bool Download { get; set; } = true;

        [JsonPropertyName("edit")]
        public bool Edit { get; set; } = true;

        [JsonPropertyName("print")]
        public bool Print { get; set; } = true;

        [JsonPropertyName("review")]
        public bool Review { get; set; } = true;
    }

    public class EditorConfigInfo
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "view"; // "view" hoặc "edit"

        [JsonPropertyName("lang")]
        public string Lang { get; set; } = "vi";

        [JsonPropertyName("callbackUrl")]
        public string CallbackUrl { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserInfo User { get; set; } = new();

        [JsonPropertyName("customization")]
        public CustomizationInfo Customization { get; set; } = new();
    }

    public class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class CustomizationInfo
    {
        [JsonPropertyName("autosave")]
        public bool Autosave { get; set; } = true;

        [JsonPropertyName("forcesave")]
        public bool Forcesave { get; set; } = true;

        [JsonPropertyName("chat")]
        public bool Chat { get; set; } = false;

        [JsonPropertyName("comments")]
        public bool Comments { get; set; } = true;

        [JsonPropertyName("compactHeader")]
        public bool CompactHeader { get; set; } = false;
    }

    /// <summary>
    /// DTO nhận thông báo Callback từ ONLYOFFICE Document Server.
    /// </summary>
    public class OnlyOfficeCallbackDto
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("changesurl")]
        public string? ChangesUrl { get; set; }

        [JsonPropertyName("users")]
        public List<string>? Users { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    /// <summary>
    /// DTO thông tin phiên bản của tệp tin đính kèm.
    /// </summary>
    public class FileVersionDto
    {
        public Guid Id { get; set; }
        public Guid FileAttachmentId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
