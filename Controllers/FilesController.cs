using AutoMapper;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlogApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public FilesController(IFileService fileService, IMapper mapper)
    {
        _fileService = fileService;
        _mapper = mapper;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileResponse>> UploadFile(IFormFile file, [FromForm] string? objectId, [FromForm] string? objectType)
    {
        var result = await _fileService.UploadFileAsync(file, objectId, objectType);
        return Ok(_mapper.Map<FileResponse>(result));
    }

    [AllowAnonymous]
    [HttpPost("metadata")]
    public async Task<ActionResult<List<FileResponse>>> GetFileMetadataByObjectId([FromForm] string? objectId)
    {
        if (string.IsNullOrEmpty(objectId))
        {
            return BadRequest("ObjectId is required.");
        }

        var results = await _fileService.GetFilesByObjectIdAsync(objectId);
        if (results.Count == 0) return NotFound("File not found");
        
        return Ok(_mapper.Map<List<FileResponse>>(results));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var result = await _fileService.DeleteFileAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        // ... Logic download giữ nguyên vì trả về FileResult, không phải DTO
        // Có thể refactor thành service trả về Stream/Bytes
        try
        {
            var (bytes, contentType, fileName) = await _fileService.DownloadFileAsync(id);
            return File(bytes, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [AllowAnonymous]
    [HttpGet("view/{id}")]
    public async Task<IActionResult> ViewFile(Guid id)
    {
        try
        {
            var (bytes, contentType, _) = await _fileService.DownloadFileAsync(id);
            return File(bytes, contentType);
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning(ex, "File not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("view/object/{objectId}")]
    public async Task<IActionResult> ViewFileByObjectId(string objectId)
    {
        try
        {
            var (bytes, contentType, _) = await _fileService.DownloadFileByObjectIdAsync(objectId);
            return File(bytes, contentType);
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning(ex, "File by objectId not found: {ObjectId}", objectId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error viewing file by objectId: {ObjectId}", objectId);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }

    [AllowAnonymous]
    [HttpGet("download/object/{objectId}")]
    public async Task<IActionResult> DownloadFileByObjectId(string objectId)
    {
        try
        {
            var (bytes, contentType, fileName) = await _fileService.DownloadFileByObjectIdAsync(objectId);
            return File(bytes, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }
}
