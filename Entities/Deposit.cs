namespace EverestRemit.Api.Entities;
public enum DepositStatus { Pending, Confirmed, Rejected }
public sealed class Deposit 
{ 
    public int Id {get;set;} 
    public string UserId {get;set;}=""; 
    public ApplicationUser? User {get;set;} 
    public decimal Amount {get;set;} 
    public string Currency {get;set;}="USD"; 
    public string ReferenceNumber {get;set;}=""; 
    public string? ProofUrl {get;set;} 
    public DepositStatus Status {get;set;}=DepositStatus.Pending; 
    public string? AdminNote {get;set;} 
    public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow; 
    public DateTime? ReviewedAtUtc {get;set;} 
}