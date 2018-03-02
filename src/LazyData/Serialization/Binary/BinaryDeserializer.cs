using System;
using System.IO;
using LazyData.Mappings.Types;
using LazyData.Registries;

namespace LazyData.Serialization.Binary
{
    public class BinaryDeserializer : GenericDeserializer<BinaryWriter, BinaryReader>, IBinaryDeserializer
    {
        protected BinaryPrimitiveDeserializer BinaryPrimitiveDeserializer { get; }

        public BinaryDeserializer(IMappingRegistry mappingRegistry, ITypeCreator typeCreator, BinaryConfiguration configuration = null) : base(mappingRegistry, typeCreator)
        {
            Configuration = configuration ?? new BinaryConfiguration();
            BinaryPrimitiveDeserializer = new BinaryPrimitiveDeserializer();
        }

        protected override bool IsDataNull(BinaryReader reader)
        {
            var currentPosition = reader.BaseStream.Position;

            foreach (var nullByte in BinarySerializer.NullDataSig)
            {
                var readByte = reader.ReadByte();
                if (nullByte != readByte)
                {
                    reader.BaseStream.Position = currentPosition;
                    return false;
                }
            }
            return true;
        }

        protected override bool IsObjectNull(BinaryReader reader)
        {
            var currentPosition = reader.BaseStream.Position;

            foreach (var nullByte in BinarySerializer.NullObjectSig)
            {
                var readByte = reader.ReadByte();
                if (nullByte != readByte)
                {
                    reader.BaseStream.Position = currentPosition;
                    return false;
                }
            }
            return true;
        }
        
        protected override int GetCountFromState(BinaryReader state)
        { return state.ReadInt32(); }

        protected override object DeserializeDefaultPrimitive(Type type, BinaryReader reader)
        { return BinaryPrimitiveDeserializer.DeserializeDefaultPrimitive(type, reader);
        }

        public override object Deserialize(DataObject data)
        {
            using (var memoryStream = new MemoryStream(data.AsBytes))
            using (var reader = new BinaryReader(memoryStream))
            {
                var typeName = reader.ReadString();
                var type = TypeCreator.LoadType(typeName);
                var typeMapping = MappingRegistry.GetMappingFor(type);
                var instance = Activator.CreateInstance(type);
                Deserialize(typeMapping.InternalMappings, instance, reader);
                return instance;
            }
        }

        public override void DeserializeInto(DataObject data, object existingInstance)
        {
            using (var memoryStream = new MemoryStream(data.AsBytes))
            using (var reader = new BinaryReader(memoryStream))
            {
                var typeName = reader.ReadString();
                var type = TypeCreator.LoadType(typeName);
                var typeMapping = MappingRegistry.GetMappingFor(type);
                Deserialize(typeMapping.InternalMappings, existingInstance, reader);
            }
        }
        
        protected override string GetDynamicTypeNameFromState(BinaryReader state)
        { return state.ReadString(); }

        protected override BinaryReader GetDynamicTypeDataFromState(BinaryReader state)
        { return state; }
    }
}