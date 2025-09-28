using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using ExcelReplacement.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ExcelReplacement.Services
{
    public interface IAuthenticationService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, int organizationId);
        Task<bool> ValidateTokenAsync(string token);
        Task<User> GetUserFromTokenAsync(string token);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public User User { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _jwtSecret;
        private readonly int _jwtExpirationMinutes;
        private readonly ApplicationDbContext _context;

        public AuthenticationService(ApplicationDbContext context, string jwtSecret, int jwtExpirationMinutes = 60)
        {
            _context = context;
            _jwtSecret = jwtSecret;
            _jwtExpirationMinutes = jwtExpirationMinutes;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Organization)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

                if (user == null || !VerifyPassword(password, user.PasswordHash))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid email or password"
                    };
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    User = user
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, int organizationId)
        {
            try
            {
                // Check if user already exists
                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "User with this email already exists"
                    };
                }

                // Get default role (assuming role ID 1 is "User")
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (defaultRole == null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Default user role not found"
                    };
                }

                var user = new User
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PasswordHash = HashPassword(password),
                    OrganizationId = organizationId,
                    RoleId = defaultRole.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    User = user
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<User> GetUserFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jwt.Claims.FirstOrDefault(x => x.Type == "user_id");

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return null;
                }

                return await _context.Users
                    .Include(u => u.Organization)
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !VerifyPassword(currentPassword, user.PasswordHash))
                {
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
                if (user == null)
                {
                    return false; // Don't reveal if email exists
                }

                // TODO: Implement password reset logic (email with reset link)
                // For now, just return true
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("user_id", user.Id.ToString()),
                    new Claim("email", user.Email),
                    new Claim("organization_id", user.OrganizationId.ToString()),
                    new Claim("role_id", user.RoleId.ToString()),
                    new Claim("first_name", user.FirstName),
                    new Claim("last_name", user.LastName)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            // Simple refresh token implementation
            return Guid.NewGuid().ToString();
        }

        private string HashPassword(string password)
        {
            // Simple password hashing - in production, use BCrypt or similar
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(password + "salt"));
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
    }

    // Database Context
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
        public DbSet<ProcessingJob> ProcessingJobs { get; set; }
        public DbSet<ProcessingJobLog> ProcessingJobLogs { get; set; }
        public DbSet<AISuggestion> AISuggestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);

            // Add indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Domain)
                .IsUnique();
        }
    }
}
