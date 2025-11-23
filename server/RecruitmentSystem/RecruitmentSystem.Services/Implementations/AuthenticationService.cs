using AutoMapper;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace RecruitmentSystem.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly double _accessTokenLifetimeDays;
        private readonly double _refreshTokenLifetimeDays;

        public AuthenticationService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IJwtService jwtService,
            IMapper mapper,
            IEmailService emailService,
            ApplicationDbContext dbContext,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _mapper = mapper;
            _emailService = emailService;
            _dbContext = dbContext;
            _configuration = configuration;
            _accessTokenLifetimeDays = double.TryParse(_configuration["Jwt:ExpireDays"], out var accessDays)
                ? accessDays
                : 7d;
            _refreshTokenLifetimeDays = double.TryParse(_configuration["Jwt:RefreshTokenDays"], out var refreshDays)
                ? refreshDays
                : 30d;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress, string? userAgent)
        {
            // Add artificial delay to prevent timing attacks
            await Task.Delay(Random.Shared.Next(100, 300));

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                Console.WriteLine($"Login attempt for non-existent user: {loginDto.Email} from IP: {ipAddress}");
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Check if account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                Console.WriteLine($"Login attempt for locked account: {loginDto.Email} from IP: {ipAddress}");
                throw new UnauthorizedAccessException("Account is temporarily locked due to multiple failed login attempts. Please try again later.");
            }

            // Check if email is verified
            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedAccessException("Please verify your email before logging in");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                await _userManager.AccessFailedAsync(user);
                Console.WriteLine($"Failed login attempt for user: {loginDto.Email} from IP: {ipAddress}. Failed attempts: {await _userManager.GetAccessFailedCountAsync(user)}");
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            Console.WriteLine($"Successful login for user: {loginDto.Email} from IP: {ipAddress}");

            await RevokeAllUserRefreshTokensAsync(user.Id, ipAddress, "New login");

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = await PersistRefreshTokenAsync(user, ipAddress, userAgent);

            return await BuildAuthResponseAsync(user, token, refreshToken);
        }

        // Staff registration - Admin only
        public async Task<AuthResponseDto> RegisterStaffAsync(RegisterStaffDto registerDto, string ipAddress, string? userAgent)
        {

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            // Assign roles
            foreach (var roleName in registerDto.Roles)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }

            // Revoke all existing refresh tokens for this user to ensure single active session
            await RevokeAllUserRefreshTokensAsync(user.Id, ipAddress, "Staff registration login");

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = await PersistRefreshTokenAsync(user, ipAddress, userAgent);

            return await BuildAuthResponseAsync(user, token, refreshToken);
        }

        // Candidate self-registration - Public 
        // future enhancement : Magic link for email confirmation 
        public async Task<RegisterResponseDto> RegisterCandidateAsync(CandidateRegisterDto registerDto)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            // Candidate role
            if (await _roleManager.RoleExistsAsync("Candidate"))
            {
                await _userManager.AddToRoleAsync(user, "Candidate");
            }

            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = new List<string> { "Candidate" };

            return new RegisterResponseDto
            {
                Message = "Registration successful. Please check your email to verify your account.",
                RequiresEmailVerification = true,
                User = userProfile
            };
        }

        // Initial Super Admin
        public async Task<AuthResponseDto> RegisterInitialSuperAdminAsync(InitialAdminDto registerDto, string ipAddress, string? userAgent)
        {
            // Check if SuperAdmin already exists
            var hasAdmin = await HasSuperAdminAsync();
            if (hasAdmin)
            {
                throw new InvalidOperationException("Super Admin already exists");
            }

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            // Assign SuperAdmin role
            if (await _roleManager.RoleExistsAsync("SuperAdmin"))
            {
                await _userManager.AddToRoleAsync(user, "SuperAdmin");
            }

            // Revoke any existing refresh tokens for this user (shouldn't be any for initial admin, but for safety)
            await RevokeAllUserRefreshTokensAsync(user.Id, ipAddress, "Initial admin registration");

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = await PersistRefreshTokenAsync(user, ipAddress, userAgent);

            return await BuildAuthResponseAsync(user, token, refreshToken);
        }

        // Check if any SuperAdmin exists in the system
        public async Task<bool> HasSuperAdminAsync()
        {
            var superAdminRole = await _roleManager.FindByNameAsync("SuperAdmin");
            if (superAdminRole == null) return false;

            var usersInRole = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            return usersInRole.Any(u => u.IsActive);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user,
                changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            return result.Succeeded;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return userProfile;
        }

        public async Task LogoutAsync(Guid userId, string? refreshToken, string ipAddress)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await RevokeRefreshTokenAsync(refreshToken, ipAddress, "User logout");
                return;
            }

            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (!activeTokens.Any())
            {
                return;
            }

            var revocationTime = DateTime.UtcNow;
            foreach (var token in activeTokens)
            {
                token.RevokedAt = revocationTime;
                token.RevokedByIp = ipAddress;
                token.ReasonRevoked = "User logout";
                token.UpdatedAt = revocationTime;
            }

            _dbContext.RefreshTokens.UpdateRange(activeTokens);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            return (await _userManager.GetRolesAsync(user)).ToList();
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedAccessException("Refresh token is required");
            }

            var existingToken = await GetRefreshTokenEntityAsync(refreshToken);
            if (existingToken == null)
            {
                throw new UnauthorizedAccessException("Refresh token is invalid");
            }

            if (!existingToken.IsActive)
            {
                // Mark token as revoked if it was simply expired and someone attempted to reuse it.
                if (existingToken.RevokedAt == null)
                {
                    existingToken.RevokedAt = DateTime.UtcNow;
                    existingToken.RevokedByIp = ipAddress;
                    existingToken.ReasonRevoked = "Expired token reuse";
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    _dbContext.RefreshTokens.Update(existingToken);
                    await _dbContext.SaveChangesAsync();
                }

                throw new UnauthorizedAccessException("Refresh token is no longer valid");
            }

            var user = existingToken.User;
            var newRefreshToken = await PersistRefreshTokenAsync(user, ipAddress, userAgent);

            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.ReasonRevoked = "Rotated";
            existingToken.ReplacedByTokenHash = newRefreshToken.TokenHash;
            existingToken.UpdatedAt = DateTime.UtcNow;

            _dbContext.RefreshTokens.Update(existingToken);
            await _dbContext.SaveChangesAsync();

            var jwt = await _jwtService.GenerateJwtTokenAsync(user);
            return await BuildAuthResponseAsync(user, jwt, newRefreshToken);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string ipAddress, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var existingToken = await GetRefreshTokenEntityAsync(refreshToken);
            if (existingToken == null || !existingToken.IsActive)
            {
                return;
            }

            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.ReasonRevoked = reason ?? "Revoked";
            existingToken.UpdatedAt = DateTime.UtcNow;

            _dbContext.RefreshTokens.Update(existingToken);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<RegisterResponseDto>> BulkRegisterCandidatesAsync(IFormFile file)
        {
            var results = new List<RegisterResponseDto>();

            using (var stream = file.OpenReadStream())
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheets.First();
                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    var email = worksheet.Cell(row, 1).Value.ToString();
                    var firstName = worksheet.Cell(row, 2).Value.ToString();
                    var lastName = worksheet.Cell(row, 3).Value.ToString();
                    var phoneNumber = worksheet.Cell(row, 4).Value.ToString();
                    var providedPassword = worksheet.Cell(row, 5).Value.ToString();

                    if (string.IsNullOrEmpty(email))
                    {
                        continue;
                    }

                    try
                    {
                        var existingUser = await _userManager.FindByEmailAsync(email);
                        if (existingUser != null)
                        {
                            results.Add(new RegisterResponseDto
                            {
                                Message = $"User with email {email} already exists. Skipped registration.",
                                RequiresEmailVerification = false,
                                User = null
                            });
                            continue;
                        }

                        // Generate default password if not provided
                        var password = string.IsNullOrEmpty(providedPassword)
                            ? GenerateDefaultPassword()
                            : providedPassword;

                        var user = new User
                        {
                            UserName = email,
                            Email = email,
                            FirstName = firstName ?? "",
                            LastName = lastName ?? "",
                            PhoneNumber = phoneNumber,
                            EmailConfirmed = true
                        };

                        var result = await _userManager.CreateAsync(user, password);
                        if (result.Succeeded)
                        {
                            if (await _roleManager.RoleExistsAsync("Candidate"))
                            {
                                await _userManager.AddToRoleAsync(user, "Candidate");
                            }

                            var userProfile = _mapper.Map<UserProfileDto>(user);
                            userProfile.Roles = new List<string> { "Candidate" };

                            // Send welcome email in background
                            var fullName = $"{firstName} {lastName}".Trim();
                            var isDefaultPassword = string.IsNullOrEmpty(providedPassword);

                            // keeping the email sending in background
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _emailService.SendBulkWelcomeEmailAsync(email, fullName, password, isDefaultPassword);
                                }
                                catch (Exception emailEx)
                                {
                                    Console.WriteLine($"Failed to send welcome email to {email}: {emailEx.Message}");
                                }
                            });

                            results.Add(new RegisterResponseDto
                            {
                                Message = $"Candidate registered successfully by admin. Welcome email will be sent with {(isDefaultPassword ? "default password" : "provided password")}.",
                                RequiresEmailVerification = false,
                                User = userProfile
                            });
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            results.Add(new RegisterResponseDto
                            {
                                Message = $"Failed to create user {email}: {errors}",
                                RequiresEmailVerification = false,
                                User = null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(new RegisterResponseDto
                        {
                            Message = $"Error processing row {row} (Email: {email}): {ex.Message}",
                            RequiresEmailVerification = false,
                            User = null
                        });
                    }
                }
            }

            return results;
        }

        private string GenerateDefaultPassword()
        {
            // Generate a secure default password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Ensure it meets password requirements
            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => "!@#$%^&*".Contains(c));

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
            {
                // Regenerate if requirements not met
                return GenerateDefaultPassword();
            }

            return password;
        }

        public async Task<List<UserProfileDto>> GetAllRecruitersAsync()
        {
            var recruiters = await _userManager.GetUsersInRoleAsync("Recruiter");
            var activeRecruiters = recruiters.Where(u => u.IsActive).ToList();
            return _mapper.Map<List<UserProfileDto>>(activeRecruiters);
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(User user, string jwtToken, RefreshTokenMetadata refreshToken)
        {
            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return new AuthResponseDto
            {
                Token = jwtToken,
                Expiration = GetAccessTokenExpiration(),
                RefreshToken = refreshToken.RawToken,
                RefreshTokenExpiration = refreshToken.ExpiresAt,
                User = userProfile
            };
        }

        private DateTime GetAccessTokenExpiration()
        {
            return DateTime.UtcNow.AddDays(_accessTokenLifetimeDays);
        }

        private async Task<RefreshTokenMetadata> PersistRefreshTokenAsync(User user, string ipAddress, string? userAgent)
        {
            var rawToken = await _jwtService.GenerateRefreshTokenAsync();
            var tokenHash = HashToken(rawToken);
            var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenLifetimeDays);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedByIp = ipAddress,
                UserAgent = userAgent,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return new RefreshTokenMetadata(rawToken, tokenHash, expiresAt);
        }

        private async Task<RefreshToken?> GetRefreshTokenEntityAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            return await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private async Task RevokeAllUserRefreshTokensAsync(Guid userId, string ipAddress, string reason)
        {
            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (!activeTokens.Any())
            {
                return;
            }

            var revocationTime = DateTime.UtcNow;
            foreach (var token in activeTokens)
            {
                token.RevokedAt = revocationTime;
                token.RevokedByIp = ipAddress;
                token.ReasonRevoked = reason;
                token.UpdatedAt = revocationTime;
            }

            _dbContext.RefreshTokens.UpdateRange(activeTokens);
            await _dbContext.SaveChangesAsync();
        }

        private record RefreshTokenMetadata(string RawToken, string TokenHash, DateTime ExpiresAt);
    }
}