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
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoriesController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetAll()
    {
        var list = await _categoryRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(long id)
    {
        var item = await _categoryRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Category", id);
        return Ok(ApiResponse<CategoryDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
            throw new ValidationException("Category name is required.");

        var created = await _categoryRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.CategoryID }, ApiResponse<CategoryDto>.Ok(created, "Category added successfully via stored procedure."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
            throw new ValidationException("Category name is required.");

        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Category", id);

        await _categoryRepository.UpdateAsync(id, request.CategoryName);
        return Ok(ApiResponse.Ok("Category updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Category", id);

        await _categoryRepository.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Category deleted successfully."));
    }
}
