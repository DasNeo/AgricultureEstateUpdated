namespace AgricultureEstate.Application
{
    public interface IEstateAccount
    {
        int Gold { get; }
        void Spend(int amount);
        void Receive(int amount);
    }
}
