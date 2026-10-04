using AgricultureEstate.Domain;

namespace AgricultureEstate.Application
{
    public interface IEstateStore
    {
        EstateState Load();
        void Save(EstateState estate);
    }
}
