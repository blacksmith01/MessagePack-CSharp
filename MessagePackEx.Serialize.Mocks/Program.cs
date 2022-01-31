using System;
using System.Text.Json;
using MessagePack;
using MessagePack.Resolvers;

namespace MessagePackEx.Serialize.Mocks
{
    public static class Program
    {
        public static void Main()
        {
            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(GeneratedResolver.Instance, StandardResolver.Instance));

            var msgSerialze = new MsgLogin()
            {
                Id = 1,
                Name = "Lee",
                Params = new ListEx<LoginParam>(new LoginParam[]
                {
                    new LoginParam()
                    {
                        Id = 1,
                        Value = "Param1",
                    },
                    new LoginParam()
                    {
                        Id = 2,
                        Value = "Param2",
                    },
                }),
            };

            var serialized = MessagePackSerializer.Serialize(msgSerialze);
            var deserialize = MessagePackSerializer.DeserializeEx(serialized, MyMsgStaticPool<MsgLogin>.Instance);
            var serialized2 = MessagePackSerializer.Serialize(deserialize);
            var deserialize2 = MessagePackSerializer.DeserializeEx(serialized2, MyMsgStaticPool<MsgLogin>.Instance);

            Console.WriteLine($"deserialize2= {JsonSerializer.Serialize(deserialize2, new JsonSerializerOptions() { WriteIndented = true })}");
        }
    }

    public interface IMyMsg
    {

    }

    public class MyMsgStaticPool<T>
        where T : IMyMsg, new()
    {
        public static T Instance = new T();
    }

    [MessagePackObject]
    public class LoginParam : IMyMsg
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string Value { get; set; }
    }

    [MessagePackObject]
    public class MsgLogin : IMyMsg
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string Name { get; set; }
        [Key(2)] public ListEx<LoginParam> Params { get; set; }
    }
}
