using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IPartnerService : ICrudService<PartnerDto, CreatePartnerDto, UpdatePartnerDto>
{
}
