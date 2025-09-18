using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using OfficeOpenXml;

namespace RecruitmentSystem.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;

        public AuthenticationService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IJwtService jwtService,
            IMapper mapper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _mapper = mapper;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
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
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
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
        public async Task<AuthResponseDto> RegisterCandidateAsync(CandidateRegisterDto registerDto)
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

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var userProfile = _mapper.Map<UserProfileDto>(user);
            userProfile.Roles = new List<string> { "Candidate" };

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7),
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

        public async Task<List<AuthResponseDto>> BulkRegisterCandidatesAsync(IFormFile file)
        {
            var results = new List<AuthResponseDto>();

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0]; 
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) 
                {
                    var email = worksheet.Cells[row, 1].Value?.ToString();
                    var firstName = worksheet.Cells[row, 2].Value?.ToString();
                    var lastName = worksheet.Cells[row, 3].Value?.ToString();
                    var phoneNumber = worksheet.Cells[row, 4].Value?.ToString();
                    var password = worksheet.Cells[row, 5].Value?.ToString();

                    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    {
                        continue;
                    }

                    try
                    {
                        var existingUser = await _userManager.FindByEmailAsync(email);
                        if (existingUser != null)
                        {
                            continue;
                        }

                        var user = new User
                        {
                            UserName = email,
                            Email = email,
                            FirstName = firstName ?? "",
                            LastName = lastName ?? "",
                            PhoneNumber = phoneNumber,
                            EmailConfirmed = false
                        };

                        var result = await _userManager.CreateAsync(user, password);
                        if (result.Succeeded)
                        {   
                            if (await _roleManager.RoleExistsAsync("Candidate"))
                            {
                                await _userManager.AddToRoleAsync(user, "Candidate");
                            }

                            var token = await _jwtService.GenerateJwtTokenAsync(user);
                            var userProfile = _mapper.Map<UserProfileDto>(user);
                            userProfile.Roles = new List<string> { "Candidate" };

                            results.Add(new AuthResponseDto
                            {
                                Token = token,
                                Expiration = DateTime.UtcNow.AddDays(7),
                                User = userProfile
                            });
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return results;
        }
    }
}