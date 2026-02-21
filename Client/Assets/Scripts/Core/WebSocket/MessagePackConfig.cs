using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;

namespace Core.WebSocket
{
    public class MessagePackConfig
    {
        public void InitMessagePackResolvers()
        {
            // Combine the UnityResolver (for Vector3, Color, Quaternion, etc.)
            var compositeResolver = CompositeResolver.Create(
                UnityResolver.Instance,
                StandardResolver.Instance,
                ContractlessStandardResolver.Instance // Fallback for "unannotated" C# classes
            );

            // Apply composite resolver to default options
            var options = MessagePackSerializerOptions
                .Standard
                .WithResolver(compositeResolver);

            MessagePackSerializer.DefaultOptions = options;
        }
    }
}
