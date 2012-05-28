﻿using System.Collections.Generic;
using Microsoft.DirectX;

namespace SonicRetro.SAModel.SADXLVL2
{
    public static class Extensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            TValue output;
            if (dict.TryGetValue(key, out output))
                return output;
            return @default;
        }

        public static Vector3 ToVector3(this SADXPCTools.Vertex vertex)
        {
            return new Vector3(vertex.X, vertex.Y, vertex.Z);
        }

        public static SADXPCTools.Vertex ToSADXPCTools(this Vertex vertex)
        {
            return new SADXPCTools.Vertex(vertex.X, vertex.Y, vertex.Z);
        }

        public static Vertex ToSAModel(this SADXPCTools.Vertex vertex)
        {
            return new Vertex(vertex.X, vertex.Y, vertex.Z);
        }
    }
}
