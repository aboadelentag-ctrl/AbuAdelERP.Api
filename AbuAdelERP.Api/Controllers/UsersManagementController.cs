using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersManagementController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UsersManagement/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            return await _context.Users.OrderBy(u => u.Id).ToListAsync();
        }

        // GET: api/UsersManagement/roles
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<AppRole>>> GetRoles()
        {
            return await _context.Roles.OrderBy(r => r.Id).ToListAsync();
        }

        // GET: api/UsersManagement/permissions-catalog
        [HttpGet("permissions-catalog")]
        public ActionResult<IEnumerable<PermissionItem>> GetPermissionsCatalog()
        {
            return Ok(AppPermissions.GetAllPermissions());
        }

        // POST: api/UsersManagement/users
        [HttpPost("users")]
        public async Task<ActionResult<AppUser>> CreateUser([FromBody] AppUser newUser)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == newUser.Username.ToLower()))
            {
                return BadRequest("اسم المستخدم هذا مستخدم بالفعل.");
            }

            if (string.IsNullOrWhiteSpace(newUser.PasswordHash))
            {
                newUser.PasswordHash = "123456"; // كلمة مرور افتراضية
            }

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(newUser);
        }

        // PUT: api/UsersManagement/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] AppUser updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("المستخدم غير موجود");

            user.FullName = updatedUser.FullName;
            user.Role = updatedUser.Role;
            user.DepartmentId = updatedUser.DepartmentId;
            user.DepartmentName = updatedUser.DepartmentName;
            user.IsActive = updatedUser.IsActive;
            user.CustomPermissions = updatedUser.CustomPermissions;

            if (!string.IsNullOrWhiteSpace(updatedUser.PasswordHash) && updatedUser.PasswordHash != user.PasswordHash)
            {
                user.PasswordHash = updatedUser.PasswordHash;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/UsersManagement/roles/{id}/permissions
        [HttpPut("roles/{id}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int id, [FromBody] string permissionsCommaSeparated)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound("الدور غير موجود");

            role.Permissions = permissionsCommaSeparated;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}