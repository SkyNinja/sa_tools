﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace SonicRetro.SAModel
{
    public class Mesh
    {
        public ushort MaterialID { get; set; }
        public PolyType PolyType { get; private set; }
        public ReadOnlyCollection<Poly> Poly { get; private set; }
        public string PolyName { get; set; }
        public int PAttr { get; set; }
        public Vertex[] PolyNormal { get; private set; }
        public string PolyNormalName { get; set; }
        public Color[] VColor { get; private set; }
        public string VColorName { get; set; }
        public UV[] UV { get; private set; }
        public string UVName { get; set; }
        public string Name { get; set; }

        public static int Size(bool DX) { return DX ? 0x1C : 0x18; }

        public Mesh(byte[] file, int address, uint imageBase)
            : this(file, address, imageBase, new Dictionary<int, string>()) { }
        
        public Mesh(byte[] file, int address, uint imageBase, Dictionary<int, string> labels)
        {
            if (labels.ContainsKey(address))
                Name = labels[address];
            else
                Name = "mesh_" + address.ToString("X8");
            MaterialID = ByteConverter.ToUInt16(file, address);
            PolyType = (PolyType)(MaterialID >> 0xE);
            MaterialID &= 0x3FFF;
            Poly[] polys = new Poly[ByteConverter.ToInt16(file, address + 2)];
            int tmpaddr = (int)(ByteConverter.ToUInt32(file, address + 4) - imageBase);
            if (labels.ContainsKey(tmpaddr))
                PolyName = labels[tmpaddr];
            else
                PolyName = "poly_" + tmpaddr.ToString("X8");
            int striptotal = 0;
            for (int i = 0; i < polys.Length; i++)
            {
                polys[i] = SAModel.Poly.CreatePoly(PolyType, file, tmpaddr);
                striptotal += polys[i].Indexes.Length;
                tmpaddr += polys[i].Size;
            }
            Poly = new ReadOnlyCollection<SAModel.Poly>(polys);
            PAttr = ByteConverter.ToInt32(file, address + 8);
            tmpaddr = ByteConverter.ToInt32(file, address + 0xC);
            if (tmpaddr != 0)
            {
                tmpaddr = (int)unchecked((uint)tmpaddr - imageBase);
                if (labels.ContainsKey(tmpaddr))
                    PolyNormalName = labels[tmpaddr];
                else
                    PolyNormalName = "polynormal_" + tmpaddr.ToString("X8");
                PolyNormal = new Vertex[polys.Length];
                for (int i = 0; i < polys.Length; i++)
                {
                    PolyNormal[i] = new Vertex(file, tmpaddr);
                    tmpaddr += SAModel.Vertex.Size;
                }
            }
            else
                PolyNormalName = "polynormal_" + Object.GenerateIdentifier();
            tmpaddr = ByteConverter.ToInt32(file, address + 0x10);
            if (tmpaddr != 0)
            {
                tmpaddr = (int)unchecked((uint)tmpaddr - imageBase);
                if (labels.ContainsKey(tmpaddr))
                    VColorName = labels[tmpaddr];
                else
                    VColorName = "vcolor_" + tmpaddr.ToString("X8");
                VColor = new Color[striptotal];
                for (int i = 0; i < striptotal; i++)
                {
                    VColor[i] = SAModel.VColor.FromBytes(file, tmpaddr);
                    tmpaddr += SAModel.VColor.Size;
                }
            }
            else
                VColorName = "vcolor_" + Object.GenerateIdentifier();
            tmpaddr = ByteConverter.ToInt32(file, address + 0x14);
            if (tmpaddr != 0)
            {
                tmpaddr = (int)unchecked((uint)tmpaddr - imageBase);
                if (labels.ContainsKey(tmpaddr))
                    UVName = labels[tmpaddr];
                else
                    UVName = "uv_" + tmpaddr.ToString("X8");
                UV = new UV[striptotal];
                for (int i = 0; i < striptotal; i++)
                {
                    UV[i] = new UV(file, tmpaddr);
                    tmpaddr += SAModel.UV.Size;
                }
            }
            else
                UVName = "uv_" + Object.GenerateIdentifier();
        }

        public Mesh(PolyType polyType, int polyCount, bool hasPolyNormal, bool hasUV, bool hasVColor)
        {
            if (polyType == SAModel.PolyType.NPoly | polyType == SAModel.PolyType.Strips) throw new ArgumentException("Cannot create a Poly of that type!\nTry another overload to create Strip-type Polys.", "polyType");
            Name = "mesh_" + Object.GenerateIdentifier();
            PolyName = "poly_" + Object.GenerateIdentifier();
            PolyType = polyType;
            Poly[] polys = new Poly[polyCount];
            int striptotal = 0;
            for (int i = 0; i < polys.Length; i++)
            {
                polys[i] = SAModel.Poly.CreatePoly(PolyType);
                striptotal += polys[i].Indexes.Length;
            }
            Poly = new ReadOnlyCollection<SAModel.Poly>(polys);
            if (hasPolyNormal)
            {
                PolyNormalName = "polynormal_" + Object.GenerateIdentifier();
                PolyNormal = new Vertex[polys.Length];
                for (int i = 0; i < polys.Length; i++)
                    PolyNormal[i] = new Vertex();
            }
            if (hasVColor)
            {
                VColorName = "vcolor_" + Object.GenerateIdentifier();
                VColor = new Color[striptotal];
            }
            if (hasUV)
            {
                UVName = "uv_" + Object.GenerateIdentifier();
                UV = new UV[striptotal];
                for (int i = 0; i < striptotal; i++)
                    UV[i] = new UV();
            }
        }

        public Mesh(Poly[] polys, bool hasPolyNormal, bool hasUV, bool hasVColor)
            : this(polys[0].PolyType, polys.Length, hasPolyNormal, hasUV, hasVColor)
        {
            int striptotal = 0;
            for (int i = 0; i < polys.Length; i++)
                striptotal += polys[i].Indexes.Length;
            Poly = new ReadOnlyCollection<SAModel.Poly>(polys);
            if (hasVColor)
                VColor = new Color[striptotal];
            if (hasUV)
            {
                UV = new UV[striptotal];
                for (int i = 0; i < striptotal; i++)
                    UV[i] = new UV();
            }
        }

        public byte[] GetBytes(uint polyAddress, uint polyNormalAddress, uint vColorAddress, uint uVAddress, bool DX)
        {
            List<byte> result = new List<byte>();
            result.AddRange(ByteConverter.GetBytes((ushort)((MaterialID & 0x3FFF) | ((int)PolyType << 0xE))));
            result.AddRange(ByteConverter.GetBytes((ushort)Poly.Count));
            result.AddRange(ByteConverter.GetBytes(polyAddress));
            result.AddRange(ByteConverter.GetBytes(PAttr));
            result.AddRange(ByteConverter.GetBytes(polyNormalAddress));
            result.AddRange(ByteConverter.GetBytes(vColorAddress));
            result.AddRange(ByteConverter.GetBytes(uVAddress));
            if (DX) result.AddRange(new byte[4]);
            return result.ToArray();
        }
    }
}