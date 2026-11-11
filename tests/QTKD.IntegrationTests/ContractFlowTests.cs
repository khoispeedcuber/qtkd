using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.IntegrationTests.Infrastructure;

namespace QTKD.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class ContractFlowTests : IAsyncLifetime
{
    private const string FullContent = "Hợp đồng dịch vụ xếp dỡ container năm 2026.";

    private readonly QtkdApiFactory _factory;
    private readonly HttpClient _client;

    public ContractFlowTests(QtkdApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TC01_CreateCustomer_Valid_IsSavedAndReadBack()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new { name = "Công ty Đại Dương" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(created);
        Assert.Equal("Công ty Đại Dương", created.Name);
        Assert.Equal("Active", created.Status);
        Assert.Equal($"/api/customers/{created.Id}", response.Headers.Location?.AbsolutePath);

        var readBack = await _client.GetFromJsonAsync<CustomerDto>($"/api/customers/{created.Id}");
        Assert.Equal(created, readBack);

        await using var db = _factory.CreateDbContext();
        var saved = await db.Customers.SingleAsync();
        Assert.Equal(created.Id, saved.Id);
        Assert.Equal("Công ty Đại Dương", saved.Name);
        Assert.Equal(CustomerStatus.Active, saved.Status);
    }

    [Fact]
    public async Task TC02_CreateCustomer_MissingName_IsRejectedWithMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ProblemJson.ReadAsync(response);
        Assert.Contains("Name", problem.ErrorFields, StringComparer.OrdinalIgnoreCase);

        await using var db = _factory.CreateDbContext();
        Assert.False(await db.Customers.AnyAsync());
    }

    [Fact]
    public async Task TC03_UpdateDraftContract_ValidData_IsSaved()
    {
        var contract = await CreateContractAsync(complete: false);

        var response = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", UpdateBody(contract, FullContent));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ContractDto>();
        Assert.NotNull(updated);
        Assert.Equal(FullContent, updated.Content);
        Assert.Equal("HD-2026-020.pdf", updated.AttachmentFileName);
        Assert.Equal("Draft", updated.Status);

        await using var db = _factory.CreateDbContext();
        var saved = await db.Contracts.SingleAsync(c => c.Id == contract.Id);
        Assert.Equal(FullContent, saved.Content);
        Assert.Equal("HD-2026-020.pdf", saved.AttachmentFileName);
        Assert.Equal(ContractStatus.Draft, saved.Status);
    }

    [Fact]
    public async Task TC04_UpdatePendingApprovalContract_IsRejectedAndDataUnchanged()
    {
        var contract = await CreateContractAsync(complete: true);
        (await _client.PostAsync($"/api/contracts/{contract.Id}/submit", null)).EnsureSuccessStatusCode();

        var response = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", UpdateBody(contract, "NỘI DUNG SỬA TRÁI PHÉP"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CTR-01", (await ProblemJson.ReadAsync(response)).Rule);

        await using var db = _factory.CreateDbContext();
        var saved = await db.Contracts.SingleAsync(c => c.Id == contract.Id);
        Assert.Equal(FullContent, saved.Content);
        Assert.Equal(ContractStatus.PendingApproval, saved.Status);
    }

    [Fact]
    public async Task TC05_SubmitContract_MissingCtr02Conditions_IsRejectedAndStaysDraft()
    {
        var contract = await CreateContractAsync(complete: false);

        var response = await _client.PostAsync($"/api/contracts/{contract.Id}/submit", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ProblemJson.ReadAsync(response);
        Assert.Equal("CTR-02", problem.Rule);
        Assert.Equal(new[] { "AttachmentFileName", "Content" }, problem.ErrorFields);

        var readBack = await _client.GetFromJsonAsync<ContractDto>($"/api/contracts/{contract.Id}");
        Assert.Equal("Draft", readBack?.Status);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(ContractStatus.Draft, (await db.Contracts.SingleAsync(c => c.Id == contract.Id)).Status);
        Assert.False(await db.ApprovalRequests.AnyAsync());
    }

    [Fact]
    public async Task TC06_SubmitContract_Valid_MovesToPendingApproval()
    {
        var contract = await CreateContractAsync(complete: true);

        var response = await _client.PostAsync($"/api/contracts/{contract.Id}/submit", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SubmitContractResponse>();
        Assert.NotNull(result);
        Assert.Equal(contract.Id, result.ContractId);
        Assert.Equal("PendingApproval", result.Status);

        var readBack = await _client.GetFromJsonAsync<ContractDto>($"/api/contracts/{contract.Id}");
        Assert.Equal("PendingApproval", readBack?.Status);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(ContractStatus.PendingApproval, (await db.Contracts.SingleAsync(c => c.Id == contract.Id)).Status);
        var approval = await db.ApprovalRequests.SingleAsync();
        Assert.Equal(result.ApprovalId, approval.Id);
        Assert.Equal(contract.Id, approval.ContractId);
    }

    [Fact]
    public async Task TC07_UpdateDraftContract_ToApproved_IsRejected()
    {
        var contract = await CreateContractAsync(complete: true);

        var response = await _client.PutAsJsonAsync($"/api/contracts/{contract.Id}", UpdateBody(contract, "Nội dung mới", status: "Approved"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("CTR-03", (await ProblemJson.ReadAsync(response)).Rule);

        await using var db = _factory.CreateDbContext();
        var saved = await db.Contracts.SingleAsync(c => c.Id == contract.Id);
        Assert.Equal(ContractStatus.Draft, saved.Status);
        Assert.Equal(FullContent, saved.Content);
    }

    [Fact]
    public async Task TC08_SubmitSameContractTwice_CreatesOnlyOneApproval()
    {
        var contract = await CreateContractAsync(complete: true);

        var first = await _client.PostAsync($"/api/contracts/{contract.Id}/submit", null);
        var second = await _client.PostAsync($"/api/contracts/{contract.Id}/submit", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("US03", (await ProblemJson.ReadAsync(second)).Rule);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.ApprovalRequests.CountAsync(a => a.ContractId == contract.Id));
        Assert.Equal(ContractStatus.PendingApproval, (await db.Contracts.SingleAsync(c => c.Id == contract.Id)).Status);
    }

    [Fact]
    public async Task TC08_SubmitSameContractConcurrently_CreatesOnlyOneApproval()
    {
        var contract = await CreateContractAsync(complete: true);

        var responses = await Task.WhenAll(
            _client.PostAsync($"/api/contracts/{contract.Id}/submit", null),
            _client.PostAsync($"/api/contracts/{contract.Id}/submit", null));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.ApprovalRequests.CountAsync(a => a.ContractId == contract.Id));
    }

    private async Task<ContractDto> CreateContractAsync(bool complete)
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new { name = "Công ty Minh Phát" });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var contractResponse = await _client.PostAsJsonAsync("/api/contracts", new
        {
            contractNo = "HD-2026-020",
            customerId = customer!.Id,
            startDate = "2026-11-01",
            endDate = "2027-10-31",
            content = complete ? FullContent : null,
            attachmentFileName = complete ? "HD-2026-020.pdf" : null
        });
        Assert.Equal(HttpStatusCode.Created, contractResponse.StatusCode);
        var contract = await contractResponse.Content.ReadFromJsonAsync<ContractDto>();
        Assert.Equal("Draft", contract!.Status);
        return contract;
    }

    private static object UpdateBody(ContractDto contract, string content, string? status = null) => new
    {
        customerId = contract.CustomerId,
        startDate = contract.StartDate,
        endDate = contract.EndDate,
        content,
        attachmentFileName = "HD-2026-020.pdf",
        status
    };
}
