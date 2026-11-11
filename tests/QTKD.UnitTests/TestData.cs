using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Services;
using QTKD.UnitTests.Fakes;

namespace QTKD.UnitTests;

public sealed class TestData
{
    public const string OriginalContent = "Hợp đồng dịch vụ xếp dỡ container năm 2026.";

    public FakeCustomerRepository Customers { get; } = new();
    public FakeContractRepository Contracts { get; } = new();

    public ContractService CreateContractService() => new(Contracts, Customers);

    public Customer AddCustomer(CustomerStatus status = CustomerStatus.Active)
    {
        var customer = new Customer
        {
            Id = Customers.Customers.Count + 1,
            Name = "Công ty Minh Phát",
            Status = status
        };
        Customers.Customers.Add(customer);
        return customer;
    }

    public Contract AddCompleteContract(ContractStatus status = ContractStatus.Draft)
    {
        var customer = AddCustomer();
        var contract = new Contract
        {
            Id = Contracts.Contracts.Count + 1,
            ContractNo = $"HD-2026-{Contracts.Contracts.Count + 18:000}",
            CustomerId = customer.Id,
            Customer = customer,
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2027, 10, 31),
            Content = OriginalContent,
            AttachmentFileName = "HD-2026-018.pdf",
            Status = status
        };
        Contracts.Contracts.Add(contract);
        return contract;
    }

    public static UpdateContractRequest UpdateRequestFor(Contract contract, string content, string? status = null) => new()
    {
        CustomerId = contract.CustomerId,
        StartDate = contract.StartDate,
        EndDate = contract.EndDate,
        Content = content,
        AttachmentFileName = contract.AttachmentFileName,
        Status = status
    };
}
