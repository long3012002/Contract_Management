using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserImportResultDto> ImportUsersAsync(List<CreateUserDto> dtos);
        Task<UserImportResultDto> UpdateUserAsync(Guid id, UpdateUserDto dto);
        Task<UserDeleteResultDto> DeleteUsersAsync(List<Guid> ids);
    }
}
