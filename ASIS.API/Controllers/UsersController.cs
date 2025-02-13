using Microsoft.AspNetCore.Mvc;
using ASIS.API.Models;

namespace ASIS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static List<User> _users = new List<User>
    {
        new User 
        { 
            Id = 1, 
            Username = "admin",
            Password = "admin123", // Note: In a real application, passwords should be hashed
            Email = "admin@example.com",
            FullName = "System Administrator"
        }
    };

    private readonly ILogger<UsersController> _logger;

    public UsersController(ILogger<UsersController> logger)
    {
        _logger = logger;
    }

    [HttpPost("login")]
    public ActionResult<User> Login([FromBody] LoginRequest request)
    {
        var user = _users.FirstOrDefault(u => 
            u.Username == request.Username && 
            u.Password == request.Password);

        if (user == null)
        {
            return Unauthorized("Invalid username or password");
        }

        // In a real application, you would generate and return a JWT token here
        return Ok(user);
    }

    [HttpGet]
    public ActionResult<IEnumerable<User>> GetAll()
    {
        return Ok(_users);
    }

    [HttpGet("{id}")]
    public ActionResult<User> GetById(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpPost]
    public ActionResult<User> Create(User user)
    {
        if (_users.Any(u => u.Username == user.Username))
        {
            return BadRequest("Username already exists");
        }

        user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
        _users.Add(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, User user)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == id);
        if (existingUser == null)
        {
            return NotFound();
        }

        if (_users.Any(u => u.Username == user.Username && u.Id != id))
        {
            return BadRequest("Username already exists");
        }

        existingUser.Username = user.Username;
        existingUser.Password = user.Password;
        existingUser.Email = user.Email;
        existingUser.FullName = user.FullName;
        existingUser.IsActive = user.IsActive;

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        _users.Remove(user);
        return NoContent();
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
} 