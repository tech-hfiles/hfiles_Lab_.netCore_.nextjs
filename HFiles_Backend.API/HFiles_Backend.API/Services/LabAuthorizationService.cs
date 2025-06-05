using System.Security.Claims;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HFiles_Backend.API.Services
{
    public class LabAuthorizationService
    {
        private readonly AppDbContext _context;

        public LabAuthorizationService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<bool> IsLabAuthorized(int labId, ClaimsPrincipal user)
        {
            var labIdClaim = user.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (labIdClaim == null || !int.TryParse(labIdClaim.Value, out int userLabId))
                return false;

            var loggedInLab = await _context.LabSignups.FirstOrDefaultAsync(l => l.Id == userLabId);
            if (loggedInLab == null) return false;

            int mainLabId = loggedInLab.LabReference == 0 ? userLabId : loggedInLab.LabReference;

            var branchIds = await _context.LabSignups
                .Where(l => l.LabReference == mainLabId)
                .Select(l => l.Id)
                .ToListAsync();

            branchIds.Add(mainLabId); 

            return branchIds.Contains(labId);
        }
    }

}
