using System.Linq;
using System.Threading.Tasks;
using demo1.Entity;

namespace demo1.Services.Interfaces.SubServices;

public interface IDuAnSecurityService
{
    Task EnsureUserHasProjectAccessAsync(DuAn entity, string requiredAction = "EDIT");
    Task<IQueryable<DuAn>> ApplyUserAccessFilterAsync(IQueryable<DuAn> query);
}
