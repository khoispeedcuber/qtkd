using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Services;

namespace QTKD.UnitTests;

public class CustomerServiceTests
{
    [Fact]
    public async Task Create_ValidName_IsActive()
    {
        var data = new TestData();
        var service = new CustomerService(data.Customers);

        var result = await service.CreateAsync(new CreateCustomerRequest { Name = "  Công ty Đại Dương  " }, default);

        Assert.Equal("Công ty Đại Dương", result.Name);
        Assert.Equal("Active", result.Status);
        Assert.Single(data.Customers.Customers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_MissingName_IsRejected(string? name)
    {
        var data = new TestData();
        var service = new CustomerService(data.Customers);

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateCustomerRequest { Name = name }, default));

        Assert.Empty(data.Customers.Customers);
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("suspended")]
    public async Task Update_KnownStatusName_IsAccepted(string status)
    {
        var data = new TestData();
        var customer = data.AddCustomer();
        var service = new CustomerService(data.Customers);

        var result = await service.UpdateAsync(customer.Id, new UpdateCustomerRequest { Name = customer.Name, Status = status }, default);

        Assert.Equal("Suspended", result.Status);
        Assert.Equal(CustomerStatus.Suspended, customer.Status);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("Active, Suspended")]
    [InlineData("Closed")]
    public async Task Update_UnknownStatusName_IsRejectedAndUnchanged(string status)
    {
        var data = new TestData();
        var customer = data.AddCustomer();
        var service = new CustomerService(data.Customers);

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.UpdateAsync(customer.Id, new UpdateCustomerRequest { Name = "Tên mới", Status = status }, default));

        Assert.Equal(CustomerStatus.Active, customer.Status);
        Assert.Equal("Công ty Minh Phát", customer.Name);
        Assert.Equal(0, data.Customers.SaveChangesCount);
    }
}
