using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

namespace QTKD.UnitTests;

public class Ctr02SubmitContractTests
{
    [Fact]
    public async Task Submit_AllConditionsMet_MovesToPendingApproval()
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        var service = data.CreateContractService();

        var result = await service.SubmitAsync(contract.Id, default);

        Assert.Equal(contract.Id, result.ContractId);
        Assert.Equal("PendingApproval", result.Status);
        Assert.Equal(ContractStatus.PendingApproval, contract.Status);
        var approval = Assert.Single(data.Contracts.ApprovalRequests);
        Assert.Same(contract, approval.Contract);
        Assert.Equal(1, data.Contracts.SaveChangesCount);
    }

    [Theory]
    [InlineData("CustomerId")]
    [InlineData("StartDate")]
    [InlineData("EndDate")]
    [InlineData("Content")]
    [InlineData("AttachmentFileName")]
    public async Task Submit_OneConditionMissing_IsRejectedAndContractStaysDraft(string missing)
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        switch (missing)
        {
            case "CustomerId": contract.Customer.Status = CustomerStatus.Suspended; break;
            case "StartDate": contract.StartDate = null; break;
            case "EndDate": contract.EndDate = null; break;
            case "Content": contract.Content = null; break;
            case "AttachmentFileName": contract.AttachmentFileName = " "; break;
        }
        var service = data.CreateContractService();

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => service.SubmitAsync(contract.Id, default));

        Assert.Equal("CTR-02", ex.Rule);
        Assert.Equal(missing, Assert.Single(ex.Errors).Key);
        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Empty(data.Contracts.ApprovalRequests);
        Assert.Equal(0, data.Contracts.SaveChangesCount);
    }

    [Fact]
    public async Task Submit_SeveralConditionsMissing_ReportsAllOfThem()
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        contract.Content = null;
        contract.AttachmentFileName = null;
        var service = data.CreateContractService();

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => service.SubmitAsync(contract.Id, default));

        Assert.Equal(new[] { "AttachmentFileName", "Content" }, ex.Errors.Keys.OrderBy(k => k));
        Assert.Equal(ContractStatus.Draft, contract.Status);
    }

    [Fact]
    public async Task Submit_ContractAlreadySubmitted_IsRejectedWithoutNewApproval()
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        var service = data.CreateContractService();
        await service.SubmitAsync(contract.Id, default);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.SubmitAsync(contract.Id, default));

        Assert.Equal("US03", ex.Rule);
        Assert.Single(data.Contracts.ApprovalRequests);
        Assert.Equal(ContractStatus.PendingApproval, contract.Status);
        Assert.Equal(1, data.Contracts.SaveChangesCount);
    }
}
