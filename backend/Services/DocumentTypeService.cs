using ContractManagement.Api.Data;
using ContractManagement.Api.DTOs.DocumentTypes;
using ContractManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Api.Services;

public class DocumentTypeService
{
    private readonly AppDbContext _context;

    public DocumentTypeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DocumentTypeResponseDto>> GetAllAsync()
    {
        return await _context.DocumentTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => ToResponseDto(x))
            .ToListAsync();
    }

    public async Task<DocumentTypeResponseDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.DocumentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? null : ToResponseDto(entity);
    }

    public async Task<DocumentTypeResponseDto> CreateAsync(CreateDocumentTypeDto dto)
    {
        var code = dto.Code.Trim().ToUpper();

        var exists = await _context.DocumentTypes.AnyAsync(x => x.Code == code);
        if (exists)
            throw new InvalidOperationException("Mã loại hồ sơ đã tồn tại.");

        var entity = new DocumentType
        {
            Code = code,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentTypes.Add(entity);
        await _context.SaveChangesAsync();

        return ToResponseDto(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateDocumentTypeDto dto)
    {
        var entity = await _context.DocumentTypes.FindAsync(id);
        if (entity == null) return false;

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.DocumentTypes.FindAsync(id);
        if (entity == null) return false;

        _context.DocumentTypes.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private static DocumentTypeResponseDto ToResponseDto(DocumentType entity)
    {
        return new DocumentTypeResponseDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
