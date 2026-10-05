using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlDawahPharma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;

    public CustomersController(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomerDto>>>> GetAll()
    {
        var list = await _customerRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CustomerDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(long id)
    {
        var item = await _customerRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Customer", id);
        return Ok(ApiResponse<CustomerDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ValidationException("Customer name is required.");

        var created = await _customerRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.CustomerID }, ApiResponse<CustomerDto>.Ok(created, "Customer created successfully."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] UpdateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ValidationException("Customer name is required.");

        var existing = await _customerRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Customer", id);

        await _customerRepository.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok("Customer updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        var existing = await _customerRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Customer", id);

        await _customerRepository.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Customer deleted successfully."));
    }
}
