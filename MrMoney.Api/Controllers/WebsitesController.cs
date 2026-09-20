using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebsitesController : ControllerBase
    {
        private readonly IWebsiteRepository _repo;

        public WebsitesController(IWebsiteRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _repo.GetAllAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var website = await _repo.GetByIdAsync(id);
                if (website == null)
                {
                    return NotFound(new { message = $"Website with id '{id}' not found." });
                }
                return Ok(website);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Website website)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(website.Name))
                {
                    return BadRequest(new { message = "Website name is required." });
                }
                if (string.IsNullOrWhiteSpace(website.Url))
                {
                    return BadRequest(new { message = "Website URL is required." });
                }

                var created = await _repo.CreateAsync(website);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Website website)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(website.Name))
                {
                    return BadRequest(new { message = "Website name is required." });
                }
                if (string.IsNullOrWhiteSpace(website.Url))
                {
                    return BadRequest(new { message = "Website URL is required." });
                }

                var updated = await _repo.UpdateAsync(id, website);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Website with id '{id}' not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _repo.DeleteAsync(id);
                return Ok(new { message = $"Website '{id}' deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Website with id '{id}' not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
