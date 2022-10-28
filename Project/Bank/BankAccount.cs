namespace DADProject;

public class BankAccount
{
    public double Balance { get; private set; } = 0;

    public void Deposit(double amount)
    {
        Balance += amount;
    }

    public bool Withdraw(double amount)
    {
        if (amount > Balance)
            return false;
        Balance -= amount;
        return true;
    }
}