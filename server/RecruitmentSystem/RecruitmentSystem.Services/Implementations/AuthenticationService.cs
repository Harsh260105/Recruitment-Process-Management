using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using ClosedXML.Excel;

namespace RecruitmentSystem.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public AuthenticationService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IJwtService jwtService,
            IMapper mapper,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Check if email is verified
            if (!user.EmailConfirmed)
            {
                throw new UnauthorizedAccessException("Please verify your email before logging in");
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                User = userProfile
            };
        }

        // Staff registration - Admin only
        public async Task<AuthResponseDto> RegisterStaffAsync(RegisterStaffDto registerDto)
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

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = registerDto.Roles;

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                User = userProfile
            };
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
        public async Task<AuthResponseDto> RegisterInitialSuperAdminAsync(InitialAdminDto registerDto)
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

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = new List<string> { "SuperAdmin" };

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
                User = userProfile
            };
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

        public async Task<bool> LogoutAsync(Guid userId)
        {
            // logout will be handled on client side by deleting the token
            // future todo : token blacklisting
            return await Task.FromResult(true);
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
    }
}