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

        // ==========================================
        // 1. استعراض كافة المستخدمين
        // ==========================================
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            try
            {
                var users = await _context.Users.OrderBy(u => u.Id).ToListAsync();
                return Ok(users);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب المستخدمين: {ex.GetBaseException().Message}");
            }
        }

        // ==========================================
        // 2. استعراض قوالب الأدوار (Roles)
        // ==========================================
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<AppRole>>> GetRoles()
        {
            try
            {
                var roles = await _context.Roles.OrderBy(r => r.Id).ToListAsync();
                return Ok(roles);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأدوار: {ex.GetBaseException().Message}");
            }
        }

        // ==========================================
        // 3. إنشاء مستخدم جديد
        // ==========================================
        [HttpPost("users")]
        public async Task<ActionResult<AppUser>> CreateUser([FromBody] AppUser newUser)
        {
            try
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
            catch (System.Exception ex)
            {
                return StatusCode(500, $"خطأ في إنشاء المستخدم: {ex.GetBaseException().Message}");
            }
        }

        // ==========================================
        // 4. تحديث بيانات مستخدم موجود
        // ==========================================
        [HttpPut("users/{id:long}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] AppUser updatedUser)
        {
            try
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
            catch (System.Exception ex)
            {
                return StatusCode(500, $"خطأ في تحديث المستخدم: {ex.GetBaseException().Message}");
            }
        }

        // ==========================================
        // 5. تحديث صلاحيات دور وظيفي
        // ==========================================
        [HttpPut("roles/{id:int}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int id, [FromBody] string permissionsCommaSeparated)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null) return NotFound("الدور غير موجود");

                role.Permissions = permissionsCommaSeparated ?? string.Empty;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"خطأ في تحديث صلاحيات الدور: {ex.GetBaseException().Message}");
            }
        }
    }
}