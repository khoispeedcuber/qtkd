using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

namespace QTKD.UnitTests;

public class Ctr01UpdateContractTests
{
    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.RevisionRequested)]
    public async Task Update_EditableStatus_SavesNewData(ContractStatus status)
    {
        var data = new TestData();
        var contract = data.AddCompleteContract(status);
        var service = data.CreateContractService();

        var result = await service.UpdateAsync(contract.Id, TestData.UpdateRequestFor(contract, "Nội dung đã cập nhật"), default);

        Assert.Equal("Nội dung đã cập nhật", result.Content);
        Assert.Equal("Nội dung đã cập nhật", contract.Content);
        Assert.Equal(status, contract.Status);
        Assert.Equal(1, data.Contracts.SaveChangesCount);
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Rejected)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Expired)]
    public async Task Update_LockedStatus_IsRejectedAndDataUnchanged(ContractStatus status)
    {
        var data = new TestData();
        var contract = data.AddCompleteContract(status);
        var service = data.CreateContractService();

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.UpdateAsync(contract.Id, TestData.UpdateRequestFor(contract, "Nội dung sửa trái phép"), default));

        Assert.Equal("CTR-01", ex.Rule);
        Assert.Equal(TestData.OriginalContent, contract.Content);
        Assert.Equal(status, contract.Status);
        Assert.Equal(0, data.Contracts.SaveChangesCount);
    }
}
