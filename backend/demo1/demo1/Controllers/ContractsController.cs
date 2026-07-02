using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/contracts")]
public class ContractsController : CrudControllerBase<ContractDto, CreateContractDto, UpdateContractDto>
{
    public ContractsController(IContractService service) : base(service)
    {
    }
}
