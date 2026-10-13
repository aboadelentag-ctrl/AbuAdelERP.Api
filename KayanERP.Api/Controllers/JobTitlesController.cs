using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobTitlesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobTitlesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/jobtitles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobTitleDto>>> GetJobTitles()
        {
            var list = await _context.JobTitles
                .AsNoTracking()
                .Where(j => j.Active)
                .OrderBy(j => j.JobGrade)
                .ThenBy(j => j.TitleName)
                .Select(j => new JobTitleDto
                {
                    Id = j.Id,
                    TitleName = j.TitleName,
                    HourlyRate = j.HourlyRate,
                    JobGrade = j.JobGrade,
                    Active = j.Active
                })
                .ToListAsync();

            return Ok(list);
        }

        // POST: api/jobtitles (إضافة مسمى وظيفي جديد سريعاً)
        [HttpPost]
        public async Task<ActionResult<JobTitleDto>> CreateJobTitle([FromBody] CreateJobTitleRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TitleName))
            {
                return BadRequest("يرجى إدخال اسم المسمى الوظيفي.");
            }

            var jobTitle = new JobTitle
            {
                CompanyId = request.CompanyId > 0 ? request.CompanyId : 1,
                TitleName = request.TitleName.Trim(),
                HourlyRate = request.HourlyRate,
                JobGrade = request.JobGrade > 0 ? request.JobGrade : 1,
                Active = true
            };

            _context.JobTitles.Add(jobTitle);
            await _context.SaveChangesAsync();

            return Ok(new JobTitleDto
            {
                Id = jobTitle.Id,
                TitleName = jobTitle.TitleName,
                HourlyRate = jobTitle.HourlyRate,
                JobGrade = jobTitle.JobGrade,
                Active = jobTitle.Active
            });
        }

        public class JobTitleDto
        {
            public long Id { get; set; }
            public string TitleName { get; set; } = string.Empty;
            public decimal HourlyRate { get; set; }
            public int JobGrade { get; set; }
            public bool Active { get; set; }
        }

        public class CreateJobTitleRequestDto
        {
            public long CompanyId { get; set; } = 1;
            public string TitleName { get; set; } = string.Empty;
            public decimal HourlyRate { get; set; } = 25;
            public int JobGrade { get; set; } = 1;
        }
    }
}