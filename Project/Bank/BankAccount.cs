using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADProject
{
    public class BankAccount
    {
        private double balance = 694.20;

        public BankAccount() { }

        public double ReadBalance()
        {
            return balance;
        }

        public void Deposit(double amount)
        {
            balance += amount;
        }

        public bool Withdraw(double amount)
        {
            if (amount > balance)
                return false;
            balance -= amount;
            return true;
        }
    }
}
