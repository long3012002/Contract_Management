using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.Data;

namespace demo1.Services.Interfaces
{
    public interface IEntityNameCacheService
    {
        /// <summary>
        /// Retrieves names for a collection of GUIDs across all primary entity tables using short-term in-memory caching.
        /// </summary>
        /// <param name="guids">List of GUIDs to resolve</param>
        /// <param name="dbContext">Database context instance</param>
        /// <returns>Dictionary mapping GUID string to Entity Name</returns>
        Task<Dictionary<string, string>> GetEntityNamesAsync(IEnumerable<Guid> guids, AppDbContext dbContext);
    }
}
