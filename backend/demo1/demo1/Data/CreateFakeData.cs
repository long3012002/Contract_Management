using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace demo1.Data;

public static class CreateFakeDataExtensions
{
    public static async Task CreateFakeDataAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (configuration.GetValue<bool>("Database:AutoMigrate") ||
            configuration.GetValue<bool>("Database:SeedSampleData"))
        {
            await context.Database.MigrateAsync();
            if (!context.Features.Any())
            {
                // 1. Seed Features
                var features = new List<Feature>
                {
                    new() { Code = "PROJECT", Name = "Quản lý dự án", Description = "Chức năng xem, thêm, sửa, xoá dự án" },
                    new() { Code = "BID_PACKAGE", Name = "Quản lý gói thầu", Description = "Chức năng xem, thêm, sửa, xoá gói thầu" },
                    new() { Code = "CONTRACT", Name = "Quản lý hợp đồng", Description = "Chức năng xem, thêm, sửa, xoá hợp đồng" },
                    new() { Code = "PARTNER", Name = "Quản lý đối tác", Description = "Chức năng xem, thêm, sửa, xoá đối tác" },
                    new() { Code = "RESOLUTION", Name = "Quản lý nghị quyết/văn bản", Description = "Chức năng xem, thêm, sửa, xoá nghị quyết" }
                };
                context.Features.AddRange(features);
                await context.SaveChangesAsync();

                // 2. Seed Roles
                var adminRole = new Role { Name = "Admin", Description = "Quyền quản trị toàn hệ thống" };
                var managerRole = new Role { Name = "Manager", Description = "Quản lý dự án, hợp đồng" };
                var staffRole = new Role { Name = "Staff", Description = "Nhân viên xem và cập nhật thông tin" };
                context.Roles.AddRange(adminRole, managerRole, staffRole);
                await context.SaveChangesAsync();

                // 3. Seed Admin User
                var adminUser = new User
                {
                    Username = "admin",
                    FullName = "System Administrator",
                    IsActive = true,
                    IsSystemAdmin = true
                };
                var normalUser = new User
                {
                    Username = "quangmd",
                    FullName = "Mai Duy Quang",
                    IsActive = true,
                    IsSystemAdmin = true
                };
                context.Users.AddRange(adminUser, normalUser);
                await context.SaveChangesAsync();

                context.UserRoles.Add(new UserRole
                {
                    UserId = normalUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();

                if (!context.Users.Any(u => u.Username == "anhld2"))
                {
                    var anhldUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "anhld2",
                        FullName = "Lê Đức Anh",
                        IsActive = true,
                        IsSystemAdmin = true,
                        IsTwoFactorEnabled = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Users.Add(anhldUser);
                    await context.SaveChangesAsync();
                }
            }

            if (configuration.GetValue<bool>("Database:SeedSampleData"))
            {
                await DatabaseSeeder.SeedAsync(context);
            }
        }
    }
}
