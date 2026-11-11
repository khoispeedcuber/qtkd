using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

namespace QTKD.UnitTests;

public class Ctr03StatusTransitionTests
{
    [Fact]
    public async Task Submit_ValidDraft_GoesToPendingApprovalNotApproved()
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        var service = data.CreateContractService();

        await service.SubmitAsync(contract.Id, default);

        Assert.Equal(ContractStatus.PendingApproval, contract.Status);
        Assert.NotEqual(ContractStatus.Approved, contract.Status);
    }

    [Fact]
    public async Task Update_StatusSameAsCurrent_IsAccepted()
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        var service = data.CreateContractService();

        var result = await service.UpdateAsync(contract.Id, TestData.UpdateRequestFor(contract, "Nội dung mới", "Draft"), default);

        Assert.Equal("Draft", result.Status);
        Assert.Equal(ContractStatus.Draft, contract.Status);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("approved")]
    public async Task Update_DraftToApproved_IsRejectedAndStaysDraft(string requestedStatus)
    {
        var data = new TestData();
        var contract = data.AddCompleteContract();
        var service = data.CreateContractService();

        var ex = await Assert.ThrowsAsync<RuleViolationException>(
            () => service.UpdateAsync(contract.Id, TestData.UpdateRequestFor(contract, "Nội dung mới", requestedStatus), default));

        Assert.Equal("CTR-03", ex.Rule);
        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Equal(TestData.OriginalContent, contract.Content);
        Assert.Equal(0, data.Contracts.SaveChangesCount);
    }
}
