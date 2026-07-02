using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IContractService : ICrudService<ContractDto, CreateContractDto, UpdateContractDto>
{
}
