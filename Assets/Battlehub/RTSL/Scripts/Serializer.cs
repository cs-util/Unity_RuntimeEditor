using ProtoBuf.Meta;
using System;
using System.IO;
using Battlehub.RTSL.Interface;
using System.Reflection;
using System.Linq;

namespace Battlehub.RTSL
{  
    [ProtoBuf.ProtoContract]
    public class NilContainer { }

    public class ProtobufSerializer : ISerializer
    {
        private static TypeModel model;

        static ProtobufSerializer()
        {

#if !UNITY_EDITOR
            Assembly typeModelAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.FullName.Contains("RTSLTypeModel")).FirstOrDefault();
            Type type = null;
            if (typeModelAssembly != null)
            {
                type = typeModelAssembly.GetTypes().Where(t => t.Name.Contains("RTSLTypeModel")).FirstOrDefault();
            }
            
            if(type != null)
            {
                model = Activator.CreateInstance(type) as TypeModel;
            }  

            if(model == null)
            {
                UnityEngine.Debug.LogError("RTSLTypeModel was not found. Please build type model using the Build All button available through the Tools->Runtime SaveLoad->Config menu item in Unity Editor.");
            }
#endif
            if (model == null)
            {
                model = TypeModelCreator.Create();
            }
            
            model.DynamicTypeFormatting += (sender, args) =>
            {
                if (args.FormattedName == null)
                {
                    return;
                }

                if (Type.GetType(args.FormattedName) == null)
                {
                    args.Type = typeof(NilContainer);
                }
            };

            #if UNITY_EDITOR
            RuntimeTypeModel runtimeTypeModel = model as RuntimeTypeModel;
            if(runtimeTypeModel != null)
            {
                runtimeTypeModel.CompileInPlace();
            }      
            #endif  
        }


        public TData DeepClone<TData>(TData data)
        {
            return (TData)model.DeepClone(data);
        }

        public TData Deserialize<TData>(Stream stream)
        {
            TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
            return deserialized;
        }

        public object Deserialize(byte[] b, Type type)
        {
            using (var stream = new MemoryStream(b))
            {
                return model.Deserialize(stream, null, type);
            }
        }

        public object Deserialize(Stream stream, Type type, long length = -1)
        {
            if(length <= 0)
            {
                return model.Deserialize(stream, null, type);
            }
            return model.Deserialize(stream, null, type, (int)length);
        }

        public TData Deserialize<TData>(byte[] b)
        {
            using (var stream = new MemoryStream(b))
            {
                TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
                return deserialized;
            }
        }

        public TData Deserialize<TData>(byte[] b, TData obj)
        {
            using (var stream = new MemoryStream(b))
            {
                return (TData)model.Deserialize(stream, obj, typeof(TData));
            }
        }

        public void Serialize<TData>(TData data, Stream stream)
        {
            model.Serialize(stream, data);            
        }

        public byte[] Serialize<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                model.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
 }
