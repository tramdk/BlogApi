using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlogApi.Application.Features.Auth.Commands;
using BlogApi.Domain.Entities;
using Xunit;

namespace BlogApi.Tests.IntegrationTests;

public class FilesControllerTests : BaseIntegrationTest
{
    public FilesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> GetTokenAsync(string email)
    {
        var regCmd = new RegisterCommand(email, "Password123!", "File User");
        await _client.PostAsJsonAsync("/api/auth/register", regCmd);

        var loginCmd = new LoginCommand(email, "Password123!");
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginCmd);
        var authRes = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        return authRes!.AccessToken;
    }

    [Fact]
    public async Task UploadAndDownloadFile_Works()
    {
        // Arrange
        var token = await GetTokenAsync("filetest@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "testfile.jpg");
        // Add as Form Data
        content.Add(new StringContent("Test"), "objectType");
        content.Add(new StringContent("123"), "objectId");
        content.Add(new StringContent("true"), "isPublic");

        // Act - Upload
        var uploadResp = await _client.PostAsync("/api/files/upload", content);
        
        // Assert - Upload
        Assert.Equal(HttpStatusCode.OK, uploadResp.StatusCode);
        var fileInfo = await uploadResp.Content.ReadFromJsonAsync<FileResponse>();
        Assert.NotNull(fileInfo);
        Assert.Equal("testfile.jpg", fileInfo!.FileName);
        Assert.Contains("cloudinary.com", fileInfo.ViewUrl);
        Assert.Contains("cloudinary.com", fileInfo.DownloadUrl);
        
        // Act - Download
        var downloadResp = await _client.GetAsync($"/api/files/download/{fileInfo.Id}");

        // Assert - Download
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);
    }

    [Fact]
    public async Task PrivateFile_CannotBeAccessedByOthers()
    {
        // Arrange - User A uploads a private file
        var tokenA = await GetTokenAsync("userA@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "private.jpg");
        content.Add(new StringContent("Secret"), "objectType");
        content.Add(new StringContent("S1"), "objectId");
        content.Add(new StringContent("false"), "isPublic");

        var uploadResp = await _client.PostAsync("/api/files/upload", content);
        var fileInfo = await uploadResp.Content.ReadFromJsonAsync<FileResponse>();

        // Act - User B tries to download
        var tokenB = await GetTokenAsync("userB@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var downloadResp = await _client.GetAsync($"/api/files/download/{fileInfo!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, downloadResp.StatusCode);
    }

    [Fact]
    public async Task PublicFile_CanBeAccessedByOthers()
    {
        // Arrange - User A uploads a public file
        var tokenA = await GetTokenAsync("userPublicA@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "public.jpg");
        content.Add(new StringContent("Public"), "objectType");
        content.Add(new StringContent("P1"), "objectId");
        content.Add(new StringContent("true"), "isPublic");

        var uploadResp = await _client.PostAsync("/api/files/upload", content);
        var fileInfo = await uploadResp.Content.ReadFromJsonAsync<FileResponse>();

        // Act - User B tries to download
        var tokenB = await GetTokenAsync("userPublicB@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var downloadResp = await _client.GetAsync($"/api/files/download/{fileInfo!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);
    }

    [Fact]
    public async Task DeleteFile_Works()
    {
        // Arrange
        var token = await GetTokenAsync("filedelete@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "testfile_to_delete.jpg");
        content.Add(new StringContent("DeleteTest"), "objectType");
        content.Add(new StringContent("del1"), "objectId");

        var uploadResp = await _client.PostAsync("/api/files/upload", content);
        var fileInfo = await uploadResp.Content.ReadFromJsonAsync<FileResponse>();

        // Act
        var deleteResp = await _client.DeleteAsync($"/api/files/{fileInfo!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    private record AuthResponse(string AccessToken, string RefreshToken);
    public record FileResponse(Guid Id, string FileName, bool IsPublic, string ViewUrl, string DownloadUrl);
}
