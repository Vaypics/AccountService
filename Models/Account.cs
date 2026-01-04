using System;
using System.Collections.Generic;
using System.Transactions;

namespace AccountService.Models
{
    public class Account
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public AccountType Type { get; set; }
        public string Currency { get; set; } = "RUB";
        public decimal Balance { get; set; }
        public decimal? InterestRate { get; set; }
        public DateTime OpenedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public List<Transaction> Transactions { get; set; } = new();
    }

    public enum AccountType
    {
        Checking,   
        Deposit,    
        Credit      
    }
}
