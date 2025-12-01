using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize] // Yêu cầu đăng nhập
    public class AiController : ControllerBase
    {
        private readonly LibraryAiService _aiService;

        public AiController(LibraryAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var response = await _aiService.ProcessQueryAsync(request.Message, userId);
            return Ok(response);
        }
    }
}