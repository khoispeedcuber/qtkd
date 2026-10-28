namespace QTKD.Api.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public List<Contract> Contracts { get; set; } = new();
}
