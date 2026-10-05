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
public class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _companyRepository;

    public CompaniesController(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyDto>>>> GetAll()
    {
        var list = await _companyRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CompanyDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetById(long id)
    {
        var item = await _companyRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Company", id);
        return Ok(ApiResponse<CompanyDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Create([FromBody] CreateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            throw new ValidationException("Company name is required.");

        var created = await _companyRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.CompanyID }, ApiResponse<CompanyDto>.Ok(created, "Company created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] UpdateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            throw new ValidationException("Company name is required.");

        var existing = await _companyRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Company", id);

        await _companyRepository.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok("Company updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        var existing = await _companyRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Company", id);

        await _companyRepository.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Company deleted successfully."));
    }
}
