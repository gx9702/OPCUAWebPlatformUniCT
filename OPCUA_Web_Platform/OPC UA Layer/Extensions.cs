using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using NJsonSchema;
using WebPlatform.Exceptions;
using WebPlatform.Models.DataSet;
using WebPlatform.OPC_UA_Layer;

namespace WebPlatform.Extensions
{
    public static class ExpandedNodeIdExtensionMethods
    {
        public static string ToStringId(this ExpandedNodeId expandedNodeId, NamespaceTable namespaceTable)
        {
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, namespaceTable);
            return $"{nodeId.NamespaceIndex}-{nodeId.Identifier}";
        }
    }

    public static class CollectionInitializerExtensionMethods
    {
        public static void Add(this IList<JToken> list, IList<JToken> toAdd)
        {
            foreach (var a in toAdd)
            {
                list.Add(a);
            }
        }
    }

    public static class JTokenExtensionMethods
    {
        public static int[] GetJsonArrayDimensions(this JToken jToken)
        {
            if (jToken.Type != JTokenType.Array) 
                throw new ValueToWriteTypeException("Expected a JSON Array but received a " + jToken.Type);
            while (jToken.HasValues)
            {
                var children = jToken.Children();
                var count = children.First().Count();
                
                //if(children.All(x => x.Count() == count)) throw new ValueToWriteTypeException("The array sent must have the same number of element in each dimension");
                
                foreach (var child in children)
                {
                    if(child.Count() != count)
                        throw new ValueToWriteTypeException("The array sent must have the same number of element in each dimension");
                }
                jToken = jToken.Last;
            }

            const string pattern = @"\[(\d+)\]";
            var regex = new Regex(pattern);
            var matchColl = regex.Matches(jToken.Path);
            var dimensions = new int[matchColl.Count];
            for (var i = 0; i < matchColl.Count; i++)
            {
                dimensions[i] = int.Parse(matchColl[i].Groups[1].Value) + 1;
            }
            return dimensions;
        }
        
        public static JArray ToOneDimensionJArray(this JToken jToken)
        {
            var dimensions = jToken.GetJsonArrayDimensions();
            return jToken.ToOneDimensionJArray(dimensions);
        }
        
        public static JArray ToOneDimensionJArray(this JToken jToken, int [] dimensions)
        {
            var flatValuesToWrite = jToken.Children().ToArray();
            for (var i = 0; i < dimensions.Length - 1; i++)
                flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();

            return new JArray(flatValuesToWrite);
        }

        public static int CalculateActualValueRank(this JToken jToken)
        {
            if (jToken.Type != JTokenType.Array)
                return -1;
            
            var jArray = jToken.ToArray();
            int numDimensions = 1;
            
            while(jArray.GetElementsType() == JTokenType.Array)
            {
                jArray = jArray.Children().ToArray();
                numDimensions++;
            }

            return numDimensions;
        }
        
        
        public static JTokenType GetElementsType(this JToken[] jTokens)
        {
            if(!jTokens.ElementsHasSameType())
                throw new ValueToWriteTypeException("The array sent must have the same number of element in each dimension");
            return jTokens.First().Type;
        }

        private static bool ElementsHasSameType(this JToken[] jTokens)
        {
            var checkType = jTokens[0].Type == JTokenType.Integer ? JTokenType.Float : jTokens[0].Type;
            return jTokens
                .Select(x => (x.Type == JTokenType.Integer) ? JTokenType.Float : x.Type)
                .All(t => t == checkType);
        }
        
    }
    
    
    public static class BuiltInExtensionMethods
    {
        public static List<T> FlatList<T>(this List<List<T>> list)
        {
            return list.SelectMany(a => a).ToList();
        }

        public static Func<Variant> GetDecodeDelegate(this BuiltInType builtIn, PlatformJsonDecoder decoder)
        {
            switch (builtIn)
            {
                case BuiltInType.Boolean:
                    return () => new Variant(decoder.ReadBoolean("Value"));
                case BuiltInType.SByte:
                    return () => new Variant(decoder.ReadBoolean("Value"));
                case BuiltInType.Byte:
                    return () => new Variant(decoder.ReadSByte("Value"));
                case BuiltInType.Int16:
                    return () => new Variant(decoder.ReadInt16("Value"));
                case BuiltInType.UInt16:
                    return () => new Variant(decoder.ReadUInt16("Value"));
                case BuiltInType.Int32:
                    return () => new Variant(decoder.ReadInt32("Value"));
                case BuiltInType.UInt32:
                    return () => new Variant(decoder.ReadUInt32("Value"));
                case BuiltInType.Int64:
                    return () => new Variant(decoder.ReadInt64("Value"));
                case BuiltInType.UInt64:
                    return () => new Variant(decoder.ReadUInt64("Value"));
                case BuiltInType.Float:
                    return () => new Variant(decoder.ReadFloat("Value"));
                case BuiltInType.Double:
                    return () => new Variant(decoder.ReadDouble("Value"));
                case BuiltInType.String:
                    return () => new Variant(decoder.ReadString("Value"));
                case BuiltInType.DateTime:
                    return () => new Variant(decoder.ReadDateTime("Value"));
                case BuiltInType.Guid:
                    return () => new Variant(decoder.ReadGuid("Value"));
                case BuiltInType.ByteString:
                    return () => new Variant(decoder.ReadByteString("Value"));
                case BuiltInType.XmlElement:
                    return () => new Variant(decoder.ReadXmlElement("Value"));
                case BuiltInType.NodeId:
                    return () => new Variant(decoder.ReadNodeId("Value"));
                case BuiltInType.ExpandedNodeId:
                    return () => new Variant(decoder.ReadExpandedNodeId("Value"));
                case BuiltInType.StatusCode:
                    return () => new Variant(decoder.ReadStatusCode("Value"));
                case BuiltInType.QualifiedName:
                    return () => new Variant(decoder.ReadQualifiedName("Value"));
                case BuiltInType.LocalizedText:
                    return () => new Variant(decoder.ReadLocalizedText("Value"));
                case BuiltInType.ExtensionObject:
                    throw new NotImplementedException();
                case BuiltInType.DiagnosticInfo:
                    return () => new Variant(decoder.ReadDiagnosticInfo("Value"));
                case BuiltInType.Enumeration:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
        
        public static Func<Variant> GetDecodeArrayDelegate(this BuiltInType builtIn, PlatformJsonDecoder decoder)
        {
            switch (builtIn)
            {
                case BuiltInType.Boolean:
                    return () => new Variant(decoder.ReadBooleanArray("Value").ToArray());
                case BuiltInType.SByte:
                    return () => new Variant(decoder.ReadSByteArray("Value").ToArray());
                case BuiltInType.Byte:
                    return () => new Variant(decoder.ReadSByteArray("Value").ToArray());
                case BuiltInType.Int16:
                    return () => new Variant(decoder.ReadInt16Array("Value").ToArray());
                case BuiltInType.UInt16:
                    return () => new Variant(decoder.ReadUInt16Array("Value").ToArray());
                case BuiltInType.Int32:
                    return () => new Variant(decoder.ReadInt32Array("Value").ToArray());
                case BuiltInType.UInt32:
                    return () => new Variant(decoder.ReadUInt32Array("Value").ToArray());
                case BuiltInType.Int64:
                    return () => new Variant(decoder.ReadInt64Array("Value").ToArray());
                case BuiltInType.UInt64:
                    return () => new Variant(decoder.ReadUInt64Array("Value").ToArray());
                case BuiltInType.Float:
                    return () => new Variant(decoder.ReadFloatArray("Value").ToArray());
                case BuiltInType.Double:
                    return () => new Variant(decoder.ReadDoubleArray("Value").ToArray());
                case BuiltInType.String:
                    return () => new Variant(decoder.ReadStringArray("Value").ToArray());
                case BuiltInType.DateTime:
                    return () => new Variant(decoder.ReadDateTimeArray("Value").ToArray());
                case BuiltInType.Guid:
                    return () => new Variant(decoder.ReadGuidArray("Value").ToArray());
                case BuiltInType.ByteString:
                    return () => new Variant(decoder.ReadByteStringArray("Value").ToArray());
                case BuiltInType.XmlElement:
                    return () => new Variant(decoder.ReadXmlElementArray("Value").ToArray());
                case BuiltInType.NodeId:
                    return () => new Variant(decoder.ReadNodeIdArray("Value").ToArray());
                case BuiltInType.ExpandedNodeId:
                    return () => new Variant(decoder.ReadExpandedNodeIdArray("Value").ToArray());
                case BuiltInType.StatusCode:
                    return () => new Variant(decoder.ReadStatusCodeArray("Value").ToArray());
                case BuiltInType.QualifiedName:
                    return () => new Variant(decoder.ReadQualifiedNameArray("Value").ToArray());
                case BuiltInType.LocalizedText:
                    return () => new Variant(decoder.ReadLocalizedTextArray("Value").ToArray());
                case BuiltInType.ExtensionObject:
                    throw new NotImplementedException();
                case BuiltInType.DiagnosticInfo:
                    return () => new Variant(decoder.ReadDiagnosticInfoArray("Value").ToArray());
                case BuiltInType.Enumeration:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
        
        public static Func<Variant> GetDecodeMatrixDelegate(this BuiltInType builtIn, PlatformJsonDecoder decoder)
        {
            switch (builtIn)
            {
                case BuiltInType.Boolean:
                    return () => new Variant(new Matrix(decoder.ReadBooleanArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.SByte:
                    return () => new Variant(new Matrix(decoder.ReadSByteArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Byte:
                    return () => new Variant(new Matrix(decoder.ReadSByteArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Int16:
                    return () => new Variant(new Matrix(decoder.ReadInt16Array("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.UInt16:
                    return () => new Variant(new Matrix(decoder.ReadUInt16Array("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Int32:
                    return () => new Variant(new Matrix(decoder.ReadInt32Array("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.UInt32:
                    return () => new Variant(new Matrix(decoder.ReadUInt32Array("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Int64:
                    return () => new Variant(new Matrix(decoder.ReadInt64Array("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.UInt64:
                    return () => new Variant(new Matrix(decoder.ReadUInt64Array("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Float:
                    return () => new Variant(new Matrix(decoder.ReadFloatArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Double:
                    return () => new Variant(new Matrix(decoder.ReadDoubleArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.String:
                    return () => new Variant(new Matrix(decoder.ReadStringArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.DateTime:
                    return () => new Variant(new Matrix(decoder.ReadDateTimeArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Guid:
                    return () => new Variant(new Matrix(decoder.ReadGuidArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.ByteString:
                    return () => new Variant(new Matrix(decoder.ReadByteStringArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.XmlElement:
                    return () => new Variant(new Matrix(decoder.ReadXmlElementArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.NodeId:
                    return () => new Variant(new Matrix(decoder.ReadNodeIdArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.ExpandedNodeId:
                    return () => new Variant(new Matrix(decoder.ReadExpandedNodeIdArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.StatusCode:
                    return () => new Variant(new Matrix(decoder.ReadStatusCodeArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.QualifiedName:
                    return () => new Variant(new Matrix(decoder.ReadQualifiedNameArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.LocalizedText:
                    return () => new Variant(new Matrix(decoder.ReadLocalizedTextArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.ExtensionObject:
                    throw new NotImplementedException();
                case BuiltInType.DiagnosticInfo:
                    return () => new Variant(new Matrix(decoder.ReadDiagnosticInfoArray("Value").ToArray(), builtIn, decoder.Dimensions));
                case BuiltInType.Enumeration:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}