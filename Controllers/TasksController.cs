﻿using ChatBotAPI.Models;
using ChatBotAPI.Models.RequestModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatBotAPI.Controllers
{
    [Authorize]
    [Route("api/tasks")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> userManager;

        public TasksController(ApplicationDbContext context, [FromServices] UserManager<IdentityUser> userManager)
        {
            this.userManager = userManager;
            this._context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
        [FromQuery] string sortBy = "",
        [FromQuery] string order = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
        {
            ClaimsPrincipal principal = HttpContext.User as ClaimsPrincipal;
            var _user = await userManager.FindByNameAsync(principal.Identity?.Name);
            var query = _context.Tasks.Where(e => e.User.UserName == _user.UserName).Include(e => e.User).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.TaskName.Contains(search));
            }
            switch (sortBy)
            {
                case "taskName":
                    query = order == "ascending"
                        ? query.OrderBy(e => e.TaskName)
                        : query.OrderByDescending(e => e.TaskName);
                    break;
                case "taskStatus":
                    query = order == "ascending"
                        ? query.OrderBy(e => e.TaskStatus)
                        : query.OrderByDescending(e => e.TaskStatus);
                    break;
                default:
                    query = query.OrderBy(e => e.id);
                    break;
            }
            var totalCount = await query.CountAsync();
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
            var tasks = await query.ToListAsync();
            return Ok(new
            {
                Status = "Success",
                Data = tasks,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Message = "Tasks retrieved successfully!"
            });
        }

        [HttpGet("{id}")]
        [ActionName("GetByID")]
        public async Task<IActionResult> GetByID(int id)
        {
            var task = await _context.Tasks.Include(e => e.User).FirstOrDefaultAsync(x => x.id == id);
            if (task != null)
            {
                return Ok(new { Status = "Success", Data = task, Message = "Task Found !" });
            }
            return NotFound(new { Status = "Error", Message = "Not Found !" });
        }

        /// <remarks>
        /// Sample request:
        ///
        ///     POST /task
        ///     {
        ///        "taskName": "Task Name",
        ///        "taskDescription":  "Hello testing Task Description",
        ///        "taskType": "Daily",
        ///        "taskStatus" : "In Progress" ,
        ///        "taskTags" : "today,urgent hello",
        ///        "taskDate" : "26/08/2023"
        ///     }
        ///
        /// </remarks>
        /// 
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TaskItemRequest model)
        {
            if (model == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "Task Creation Failed!." });
            }
            ClaimsPrincipal principal = HttpContext.User as ClaimsPrincipal;
            var _user = await userManager.FindByNameAsync(principal.Identity?.Name);
            var task = new TaskItem()
            {
                TaskName = model.TaskName,
                TaskDescription = model.TaskDescription,
                TaskDate = model.TaskDate,
                TaskStatus = model.TaskStatus,
                TaskTags = model.TaskTags,
                TaskType = model.TaskType,
                CreatedAt = DateTime.Now,
                User = _user
            };
            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
            return Ok(new { Status = "Success", Data = task.id, Message = "Task Created !" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int? id, [FromBody] TaskItemRequest model)
        {
            if (model == null || id == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "Task Update Failed!." });
            }
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.TaskName = model.TaskName;
                task.TaskDescription = model.TaskDescription;
                task.TaskDate = model.TaskDate;
                task.TaskStatus = model.TaskStatus;
                task.TaskTags = model.TaskTags;
                task.TaskType = model.TaskType;

                await _context.SaveChangesAsync();
                return Ok(new { Status = "Success", Message = "Task Updated Sucessfully" });
            }
            return NotFound(new { Status = "Failed", Message = "Task Not Found" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "Task Creation Failed!." });
            }
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                return Ok(new { Status = "Success", Message = "Task Deleted Sucessfully" });
            }
            return NotFound(new { Status = "Failed", Message = "Task Not Found" });
        }
    }
}
