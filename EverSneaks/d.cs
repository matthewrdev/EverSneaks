using Evergine.Framework;

namespace EverSneaks;

public static class EntityExtensions
{
    public static Entity FindChildWithComponent<T>(this Entity entity) where T : Component
    {
        if (entity.FindComponent<T>() != null)
            return entity;

        foreach (var child in entity.ChildEntities)
        {
            var result = child.FindChildWithComponent<T>();
            if (result != null)
                return result;
        }

        return null;
    }
}
