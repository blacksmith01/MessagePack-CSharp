using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#pragma warning disable SA1309
#pragma warning disable SA1403
#pragma warning disable SA1513
#pragma warning disable SA1516
#pragma warning disable SA1649
#pragma warning disable RS0016

namespace MessagePack
{
    public class ListEx<T>
        where T : class
    {
        public List<T> Data { get; set; }
        private Queue<T> _pool;

        public ListEx()
        {
            Data = new List<T>();
            _pool = new Queue<T>();
        }
        public ListEx(IEnumerable<T> col)
        {
            Data = new List<T>(col);
            _pool = new Queue<T>(Data.Count);
        }
        public ListEx(int capacity)
        {
            Data = new List<T>(capacity);
            _pool = new Queue<T>(capacity);
        }

        public void ClearToPool()
        {
            foreach (var item in Data)
            {
                if (!_pool.Contains(item))
                {
                    _pool.Enqueue(item);
                }
            }
            Data.Clear();
        }
        public bool TryPopPooled(out T item)
        {
            if (_pool.Any())
            {
                item = _pool.Dequeue();
                return true;
            }
            else
            {
                item = default(T);
                return false;
            }
        }
    }
}

namespace MessagePack.Formatters
{
    public interface IMessagePackExFormatter<T> : IMessagePackFormatter<T>
    {
        T DeserializeEx(ref MessagePackReader reader, T value, MessagePackSerializerOptions options);
    }
}

namespace MessagePack.Formatters
{
    public class ListExFormatter<T> : IMessagePackExFormatter<ListEx<T>>
        where T : class
    {
        public void Serialize(ref MessagePackWriter writer, ListEx<T> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                var c = value.Data.Count;
                writer.WriteArrayHeader(c);

                for (int i = 0; i < c; i++)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    formatter.Serialize(ref writer, value.Data[i], options);
                }
            }
        }

        public ListEx<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                var list = new ListEx<T>((int)len);
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int i = 0; i < len; i++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        list.Data.Add(formatter.Deserialize(ref reader, options));
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return list;
            }
        }

        public ListEx<T> DeserializeEx(ref MessagePackReader reader, ListEx<T> value, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                value.ClearToPool();
                options.Security.DepthStep(ref reader);
                try
                {
                    for (int i = 0; i < len; i++)
                    {
                        reader.CancellationToken.ThrowIfCancellationRequested();
                        if (value.TryPopPooled(out var item) && options.Resolver.GetFormatter<T>() is IMessagePackExFormatter<T> exFomatter)
                        {
                            exFomatter.DeserializeEx(ref reader, item, options);
                        }
                        else
                        {
                            item = formatter.Deserialize(ref reader, options);
                        }
                        value.Data.Add(item);
                    }
                }
                finally
                {
                    reader.Depth--;
                }

                return value;
            }
        }
    }
}


namespace MessagePack
{
    public static partial class MessagePackSerializer
    {
        public static T DeserializeEx<T>(ReadOnlyMemory<byte> buffer, T value, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            var reader = new MessagePackReader(buffer)
            {
                CancellationToken = cancellationToken,
            };
            return DeserializeEx(ref reader, value, options);
        }

        public static T DeserializeEx<T>(ref MessagePackReader reader, T value, MessagePackSerializerOptions options = null)
        {
            options = options ?? DefaultOptions;

            try
            {
                if (options.Compression.IsCompression())
                {
                    throw new Exception("!Support Compression");
                }
                else
                {
                    return (options.Resolver.GetFormatterWithVerify<T>() as Formatters.IMessagePackExFormatter<T>).DeserializeEx(ref reader, value, options);
                }
            }
            catch (Exception ex)
            {
                throw new MessagePackSerializationException($"Failed to deserialize {typeof(T).FullName} value.", ex);
            }
        }
    }
}

#pragma warning restore SA1309
#pragma warning restore SA1403
#pragma warning restore SA1513
#pragma warning restore SA1516
#pragma warning restore SA1649
#pragma warning restore RS0016
