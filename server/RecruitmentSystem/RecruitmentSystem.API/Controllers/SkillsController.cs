using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.API.Controllers;

[Route("api/skills")]
[ApiController]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(
        ISkillService skillService,
        ILogger<SkillsController> logger)
    {
        _skillService = skillService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetSkills()
    {
        try
        {
            var skills = await _skillService.GetAllAsync();
            return Ok(ApiResponse<List<SkillDto>>.SuccessResponse(skills, "Skills retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skills catalog");
            return StatusCode(500, ApiResponse<List<SkillDto>>.FailureResponse(new List<string>
            {
                "An error occurred while retrieving skills"
            }, "Couldn't Retrieve Skills"));
        }
    }
}
