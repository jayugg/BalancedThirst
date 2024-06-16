
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace BalancedThirst;

public static class Extensions
{
    public static void EnsureAttributesNotNull(this CollectibleObject obj) => obj.Attributes ??= new JsonObject(new JObject());
}