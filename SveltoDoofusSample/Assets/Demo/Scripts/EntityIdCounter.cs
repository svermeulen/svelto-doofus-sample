
namespace SveltoDoofusSample
{
    public class EntityIdCounter
    {
        // 1 is reserved for global entity
        uint _count = 2;

        public uint CreateEntityId()
        {
            var newId = _count;
            _count += 1;
            return newId;
        }

        public uint CreateMultipleEntityIds(uint amount)
        {
            var startId = _count;
            _count += amount;
            return startId;
        }
    }
}
