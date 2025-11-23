using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/interview-evaluations")]
    [ApiController]
    [Authorize]
    public class InterviewEvaluationController : ControllerBase
    {
        private readonly IInterviewEvaluationService _evaluationService;
        private readonly IMapper _mapper;

        public InterviewEvaluationController(
            IInterviewEvaluationService evaluationService,
            IMapper mapper)
        {
            _evaluationService = evaluationService;
            _mapper = mapper;
        }

        #region Evaluation Submission

        /// <summary>
        /// Submit or update an evaluation for an interview
        /// </summary>
        [HttpPost("interviews/{interviewId:guid}/evaluations")]
        [Authorize]
        public async Task<ActionResult<InterviewEvaluationResponseDto>> SubmitEvaluation(
            Guid interviewId,
            [FromBody] CreateInterviewEvaluationDto dto)
        {
            var evaluatorUserId = GetCurrentUserId();

            // Check if user can evaluate this interview
            var canEvaluate = await _evaluationService.CanEvaluateInterviewAsync(interviewId, evaluatorUserId);
            if (!canEvaluate)
            {
                return Forbid("You are not authorized to evaluate this interview");
            }

            // Check if evaluation already exists
            var existingEvaluation = await _evaluationService.GetEvaluationByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);

            InterviewEvaluation evaluation;
            if (existingEvaluation != null)
            {
                // Update existing evaluation
                var updatedEvaluation = _mapper.Map(dto, existingEvaluation);
                evaluation = await _evaluationService.UpdateEvaluationAsync(updatedEvaluation);
            }
            else
            {
                // Create new evaluation
                var newEvaluation = _mapper.Map<InterviewEvaluation>(dto);
                newEvaluation.InterviewId = interviewId;
                newEvaluation.EvaluatorUserId = evaluatorUserId;
                evaluation = await _evaluationService.SubmitEvaluationAsync(newEvaluation);
            }

            var responseDto = _mapper.Map<InterviewEvaluationResponseDto>(evaluation);
            return Ok(ApiResponse<InterviewEvaluationResponseDto>.SuccessResponse(
                responseDto,
                existingEvaluation != null ? "Evaluation updated successfully" : "Evaluation submitted successfully"));
        }

        #endregion

        #region Evaluation Retrieval

        /// <summary>
        /// Get evaluation by interview and evaluator
        /// </summary>
        [HttpGet("interviews/{interviewId:guid}/my-evaluation")]
        [Authorize]
        public async Task<ActionResult<InterviewEvaluationResponseDto>> GetMyEvaluation(Guid interviewId)
        {
            var evaluatorUserId = GetCurrentUserId();
            var evaluation = await _evaluationService.GetEvaluationByInterviewAndEvaluatorAsync(interviewId, evaluatorUserId);

            if (evaluation == null)
            {
                return NotFound(ApiResponse<InterviewEvaluationResponseDto>.FailureResponse(
                    new List<string> { "Evaluation not found" },
                    "Not Found"));
            }

            var responseDto = _mapper.Map<InterviewEvaluationResponseDto>(evaluation);
            return Ok(ApiResponse<InterviewEvaluationResponseDto>.SuccessResponse(responseDto, "Evaluation retrieved successfully"));
        }

        /// <summary>
        /// Get all evaluations for an interview (HR/Admin only)
        /// </summary>
        [HttpGet("interviews/{interviewId:guid}/all")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<List<InterviewEvaluationResponseDto>>> GetAllInterviewEvaluations(Guid interviewId)
        {
            var evaluations = await _evaluationService.GetInterviewEvaluationsAsync(interviewId);
            var responseDtos = _mapper.Map<List<InterviewEvaluationResponseDto>>(evaluations);
            return Ok(ApiResponse<List<InterviewEvaluationResponseDto>>.SuccessResponse(responseDtos, "Evaluations retrieved successfully"));
        }

        #endregion

        #region Evaluation Analytics

        /// <summary>
        /// Get average score for an interview (HR/Admin only)
        /// </summary>
        [HttpGet("interviews/{interviewId:guid}/average-score")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<double>> GetAverageScoreForInterview(Guid interviewId)
        {
            var averageScore = await _evaluationService.GetAverageInterviewScoreAsync(interviewId);
            return Ok(ApiResponse<double>.SuccessResponse(averageScore, "Average score calculated successfully"));
        }

        /// <summary>
        /// Check if all required evaluations are submitted (HR/Admin only)
        /// </summary>
        [HttpGet("interviews/{interviewId:guid}/completion-status")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<bool>> IsEvaluationComplete(Guid interviewId)
        {
            var isComplete = await _evaluationService.IsInterviewEvaluationCompleteAsync(interviewId);
            return Ok(ApiResponse<bool>.SuccessResponse(isComplete, "Evaluation completion status checked successfully"));
        }

        /// <summary>
        /// Get overall recommendation for an interview (HR/Admin only)
        /// </summary>
        [HttpGet("interviews/{interviewId:guid}/recommendation")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<EvaluationRecommendation?>> GetOverallRecommendation(Guid interviewId)
        {
            var recommendation = await _evaluationService.GetOverallRecommendationAsync(interviewId);
            return Ok(ApiResponse<EvaluationRecommendation?>.SuccessResponse(recommendation, "Recommendation retrieved successfully"));
        }

        #endregion

        #region Outcome Management

        /// <summary>
        /// Set interview outcome based on evaluations (HR/Admin only)
        /// </summary>
        [HttpPut("interviews/{interviewId:guid}/outcome")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<InterviewResponseDto>> SetInterviewOutcome(
            Guid interviewId,
            [FromBody] SetInterviewOutcomeDto dto)
        {
            var setByUserId = GetCurrentUserId();
            var interview = await _evaluationService.SetInterviewOutcomeAsync(interviewId, dto.Outcome, setByUserId);
            var responseDto = _mapper.Map<InterviewResponseDto>(interview);
            return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Interview outcome set successfully"));
        }

        /// <summary>
        /// Get overall interview outcome for job application (HR/Admin only)
        /// </summary>
        [HttpGet("applications/{jobApplicationId:guid}/outcome")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<InterviewOutcome?>> GetOverallOutcomeForApplication(Guid jobApplicationId)
        {
            var outcome = await _evaluationService.GetOverallInterviewOutcomeForApplicationAsync(jobApplicationId);
            return Ok(ApiResponse<InterviewOutcome?>.SuccessResponse(outcome, "Overall outcome retrieved successfully"));
        }

        #endregion

        #region Validation and Status

        /// <summary>
        /// Check if user can evaluate a specific interview
        /// </summary>
        [HttpGet("interviews/{interviewId:guid}/can-evaluate")]
        [Authorize]
        public async Task<ActionResult<bool>> CanEvaluateInterview(Guid interviewId)
        {
            var evaluatorUserId = GetCurrentUserId();
            var canEvaluate = await _evaluationService.CanEvaluateInterviewAsync(interviewId, evaluatorUserId);
            return Ok(ApiResponse<bool>.SuccessResponse(canEvaluate, "Evaluation eligibility checked successfully"));
        }

        /// <summary>
        /// Check if interview process is complete for job application (HR/Admin only)
        /// </summary>
        [HttpGet("applications/{jobApplicationId:guid}/process-complete")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<bool>> IsInterviewProcessComplete(Guid jobApplicationId)
        {
            var isComplete = await _evaluationService.IsInterviewProcessCompleteAsync(jobApplicationId);
            return Ok(ApiResponse<bool>.SuccessResponse(isComplete, "Interview process status checked successfully"));
        }

        /// <summary>
        /// Get interviews requiring evaluation for current user
        /// </summary>
        [HttpGet("pending")]
        [Authorize]
        public async Task<ActionResult<List<InterviewResponseDto>>> GetInterviewsRequiringEvaluation()
        {
            var evaluatorUserId = GetCurrentUserId();
            var interviews = await _evaluationService.GetInterviewsRequiringEvaluationAsync(evaluatorUserId);
            var responseDtos = _mapper.Map<List<InterviewResponseDto>>(interviews);
            return Ok(ApiResponse<List<InterviewResponseDto>>.SuccessResponse(responseDtos, "Pending evaluations retrieved successfully"));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets the current user's ID from the JWT token
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        /// <summary>
        /// Gets the current user's roles from the JWT token
        /// </summary>
        private List<string> GetCurrentUserRoles()
        {
            return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }

        #endregion
    }

    #region Helper DTOs

    public class SetInterviewOutcomeDto
    {
        public InterviewOutcome Outcome { get; set; }
    }

    public class CreateInterviewEvaluationDto
    {
        public int? OverallRating { get; set; }
        public string? Strengths { get; set; }
        public string? Concerns { get; set; }
        public string? AdditionalComments { get; set; }
        public EvaluationRecommendation Recommendation { get; set; }
    }

    #endregion
}