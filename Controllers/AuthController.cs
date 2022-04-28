using System.Security.Cryptography;
using todo.Services.UserService;
using Microsoft.AspNetCore.Mvc;
using todo.Dto;
using todo.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace todo.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    public static User user = new User();
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly MyDbContext _context;

    public AuthController(IConfiguration configuration, IUserService userService, MyDbContext context)
    {
        _configuration = configuration;
        _userService = userService;
        _context = context;
    }

    [HttpGet, Authorize]
    public ActionResult<User> GetMe()
    {
        string name = _userService.GetMyName();
        Console.WriteLine(name);
        return Ok(user);
    }


    [HttpPost("register")]
    public ActionResult<User> Register(UserDto req)
    {
        CreatePasswordHash(req.Password, out byte[] passwordHash, out byte[] passwordSalt);

        user.Username = req.Username;
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        return Ok(user);
    }

    [HttpPost("login")]
    public ActionResult<string> login(UserDto req)
    {
        if (user.Username is null || user.PasswordHash is null || user.PasswordSalt is null || user.Username != req.Username)
        {
            return BadRequest("User not found");
        }

        if (!VerifyPasswordHash(req.Password, user.PasswordHash, user.PasswordSalt))
        {
            return BadRequest("Wrong password");
        }

        string token = CreateToken(user);
        return Ok(token);
    }

    private string CreateToken(User user)
    {
        if (user.Username is null)
        {
            return string.Empty;
        }

        List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            _configuration.GetSection("AppSettings:Token").Value));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        if (user.PasswordSalt is null)
        {
            return false;
        }
        using (var hmac = new HMACSHA512(user.PasswordSalt))
        {
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }
}