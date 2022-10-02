namespace DADProject
{
    public class BankAccount
    {
        private double balance = 694.20;

        public BankAccount() { }

        public double Balance
        {
            get { return balance; }
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
