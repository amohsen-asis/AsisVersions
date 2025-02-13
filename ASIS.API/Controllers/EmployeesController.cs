using Microsoft.AspNetCore.Mvc;
using ASIS.API.Models;
using System.Text.RegularExpressions;

namespace ASIS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private static List<Employee> _employees = new List<Employee>
    {
        new Employee 
        { 
            Id = 1, 
            FirstName = "John", 
            LastName = "Doe", 
            Email = "john.doe@example.com",
            Department = "IT",
            Salary = 75000,
            HireDate = new DateTime(2022, 1, 15)
        }
    };

    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(ILogger<EmployeesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Employee>> GetAll([FromQuery] string? department = null, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null,
        [FromQuery] decimal? minSalary = null,
        [FromQuery] decimal? maxSalary = null)
    {
        var query = _employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(e => e.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.HireDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.HireDate <= endDate.Value);
        }

        if (minSalary.HasValue)
        {
            query = query.Where(e => e.Salary >= minSalary.Value);
        }

        if (maxSalary.HasValue)
        {
            query = query.Where(e => e.Salary <= maxSalary.Value);
        }

        return Ok(query.ToList());
    }

    [HttpGet("{id}")]
    public ActionResult<Employee> GetById(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        if (employee == null)
        {
            return NotFound($"Employee with ID {id} not found");
        }
        return Ok(employee);
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<Employee>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search query cannot be empty");
        }

        var results = _employees.Where(e =>
            e.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            e.LastName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            e.Email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            e.Department.Contains(query, StringComparison.OrdinalIgnoreCase));

        return Ok(results);
    }

    [HttpGet("department/{department}/stats")]
    public ActionResult GetDepartmentStats(string department)
    {
        var departmentEmployees = _employees.Where(e => 
            e.Department.Equals(department, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!departmentEmployees.Any())
        {
            return NotFound($"No employees found in department: {department}");
        }

        var stats = new
        {
            Department = department,
            EmployeeCount = departmentEmployees.Count,
            AverageSalary = departmentEmployees.Average(e => e.Salary),
            MinSalary = departmentEmployees.Min(e => e.Salary),
            MaxSalary = departmentEmployees.Max(e => e.Salary),
            TotalSalary = departmentEmployees.Sum(e => e.Salary)
        };

        return Ok(stats);
    }

    [HttpPost]
    public ActionResult<Employee> Create(Employee employee)
    {
        var validationError = ValidateEmployee(employee);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        if (_employees.Any(e => e.Email.Equals(employee.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("An employee with this email already exists");
        }

        employee.Id = _employees.Count > 0 ? _employees.Max(e => e.Id) + 1 : 1;
        _employees.Add(employee);

        _logger.LogInformation("Created new employee: {FirstName} {LastName}", 
            employee.FirstName, employee.LastName);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Employee employee)
    {
        var existingEmployee = _employees.FirstOrDefault(e => e.Id == id);
        if (existingEmployee == null)
        {
            return NotFound($"Employee with ID {id} not found");
        }

        var validationError = ValidateEmployee(employee);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        if (_employees.Any(e => e.Id != id && 
            e.Email.Equals(employee.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("An employee with this email already exists");
        }

        existingEmployee.FirstName = employee.FirstName.Trim();
        existingEmployee.LastName = employee.LastName.Trim();
        existingEmployee.Email = employee.Email.Trim();
        existingEmployee.Department = employee.Department.Trim();
        existingEmployee.Salary = employee.Salary;
        existingEmployee.HireDate = employee.HireDate;

        _logger.LogInformation("Updated employee: {FirstName} {LastName}", 
            employee.FirstName, employee.LastName);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        if (employee == null)
        {
            return NotFound($"Employee with ID {id} not found");
        }

        _employees.Remove(employee);
        _logger.LogInformation("Deleted employee: {FirstName} {LastName}", 
            employee.FirstName, employee.LastName);

        return NoContent();
    }

    [HttpGet("departments")]
    public ActionResult<IEnumerable<string>> GetDepartments()
    {
        var departments = _employees
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d);
        
        return Ok(departments);
    }

    private string? ValidateEmployee(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.FirstName))
            return "First name is required";

        if (string.IsNullOrWhiteSpace(employee.LastName))
            return "Last name is required";

        if (string.IsNullOrWhiteSpace(employee.Email))
            return "Email is required";

        if (!IsValidEmail(employee.Email))
            return "Invalid email format";

        if (string.IsNullOrWhiteSpace(employee.Department))
            return "Department is required";

        if (employee.Salary < 0)
            return "Salary cannot be negative";

        if (employee.HireDate > DateTime.UtcNow)
            return "Hire date cannot be in the future";

        return null;
    }

    private bool IsValidEmail(string email)
    {
        // Basic email validation using regex
        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }
} 