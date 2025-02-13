using Microsoft.AspNetCore.Mvc;
using ASIS.API.Models;

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
    public ActionResult<IEnumerable<Employee>> GetAll()
    {
        return Ok(_employees);
    }

    [HttpGet("{id}")]
    public ActionResult<Employee> GetById(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        if (employee == null)
        {
            return NotFound();
        }
        return Ok(employee);
    }

    [HttpPost]
    public ActionResult<Employee> Create(Employee employee)
    {
        employee.Id = _employees.Max(e => e.Id) + 1;
        _employees.Add(employee);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Employee employee)
    {
        var existingEmployee = _employees.FirstOrDefault(e => e.Id == id);
        if (existingEmployee == null)
        {
            return NotFound();
        }

        existingEmployee.FirstName = employee.FirstName;
        existingEmployee.LastName = employee.LastName;
        existingEmployee.Email = employee.Email;
        existingEmployee.Department = employee.Department;
        existingEmployee.Salary = employee.Salary;
        existingEmployee.HireDate = employee.HireDate;

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        if (employee == null)
        {
            return NotFound();
        }

        _employees.Remove(employee);
        return NoContent();
    }
} 