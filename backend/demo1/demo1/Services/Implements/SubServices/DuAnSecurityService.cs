using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Services.Interfaces.SubServices;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements.SubServices;

public class DuAnSecurityService : IDuAnSecurityService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DuAnSecurityService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task EnsureUserHasProjectAccessAsync(DuAn entity, string requiredAction = "EDIT")
    {
        var currentUsername = _currentUserService.GetUsername();
        if (string.IsNullOrEmpty(currentUsername))
        {
            throw new UnauthorizedAccessException("Người dùng không hợp lệ hoặc chưa đăng nhập.");
        }

        var currentUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Người dùng không hợp lệ hoặc chưa đăng nhập.");
        }

        if (currentUser.IsSystemAdmin || entity.CreatedByUserId == currentUser.Id || entity.ChuDuAnId == currentUser.Id)
        {
            return;
        }

        var validActions = (requiredAction == "VIEW")
            ? new[] { "VIEW", "EDIT", "CREATE", "DELETE", "ADMIN" }
            : new[] { "EDIT", "ADMIN" };

        var validFeatureCodes = new[] { "DU_AN", "DUAN", "PROJECT", "PROJECTS", "" };

        var hasPermission = await _dbContext.UserPermissions
            .AsNoTracking()
            .Include(up => up.Permission)
            .AnyAsync(up =>
                up.UserId == currentUser.Id &&
                (up.DuAnId == entity.Id || up.EntityId == entity.Id.ToString()) &&
                validFeatureCodes.Contains(up.FeatureCode) &&
                up.Permission != null && validActions.Contains(up.Permission.Code));

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác trên dự án này.");
        }
    }

    public async Task<IQueryable<DuAn>> ApplyUserAccessFilterAsync(IQueryable<DuAn> query)
    {
        var currentUsername = _currentUserService.GetUsername();
        var currentUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (currentUser != null && !currentUser.IsSystemAdmin)
        {
            int? callerLevel = null;
            if (currentUser.IdChucVu.HasValue)
            {
                var callerCv = await _dbContext.ChucVus.AsNoTracking().FirstOrDefaultAsync(cv => cv.Id == currentUser.IdChucVu.Value);
                callerLevel = callerCv?.Level;
            }

            query = query.Where(da => da.CreatedByUserId == currentUser.Id
                || da.ChuDuAnId == currentUser.Id
                || _dbContext.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == da.Id)
                || (callerLevel.HasValue && (
                    (da.CreatedByUserId.HasValue && _dbContext.Users.Any(u => u.Id == da.CreatedByUserId.Value && !u.IsSystemAdmin && _dbContext.ChucVus.Any(cv => cv.Id == u.IdChucVu && cv.Level > callerLevel.Value))) ||
                    (da.ChuDuAnId.HasValue && _dbContext.Users.Any(u => u.Id == da.ChuDuAnId.Value && !u.IsSystemAdmin && _dbContext.ChucVus.Any(cv => cv.Id == u.IdChucVu && cv.Level > callerLevel.Value)))
                ))
            );
        }

        return query;
    }
}
