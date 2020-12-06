using System.IO;
using System.Collections.Generic;
using NiDump;
using SharpDX;
using SharpDX.Direct3D11;

namespace MiniCube
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

    public class Mesh
    {
        NiHeader header;
        ObjectRef triShape_ref;

        BSTriShape triShape;
        BSSkinInstance skin_instance;
        BSSkinBoneData bone_data;

        BSLightingShaderProperty shader_property = null;
        public uint SLSF1
        {
            get { return shader_property.shader_flags_1; }
        }
        public uint SLSF2
        {
            get { return shader_property.shader_flags_2; }
        }

        public string albedoMap_path;
        //public string normalMap_path;

        public uint num_bones;
        public int[] bones;

        public Buffer vb_positions, vb_uvs, vb_weights, vb_indices, ib;
        public int num_triangle_points;

        public void Dispose()
        {
            ib.Dispose();
            vb_indices.Dispose();
            vb_weights.Dispose();
            vb_uvs.Dispose();
            vb_positions.Dispose();
        }

        public Mesh(Device device, NiHeader header, ObjectRef triShape_ref)
        {
            this.header = header;
            this.triShape_ref = triShape_ref;

            triShape = header.GetObject<BSTriShape>(triShape_ref);
            skin_instance = header.GetObject<BSSkinInstance>(triShape.skin);
            bone_data = header.GetObject<BSSkinBoneData>(skin_instance.data);

            shader_property = header.GetObject<BSLightingShaderProperty>(triShape.shader_property);
            var shader_texture_set = header.GetObject<BSShaderTextureSet>(shader_property.texture_set);

            albedoMap_path = Path.GetFileName(shader_texture_set.textures[0]);

            num_bones = skin_instance.num_bones;
            bones = skin_instance.bones;

            NiDump.Transform[] bone_transforms;

            bone_transforms = new NiDump.Transform[num_bones];
            for (int i = 0; i < num_bones; i++)
            {
                GetBoneLocal(i, out bone_transforms[i]);
            }

            // create device resources
            {
                Vector3[] positions = new Vector3[triShape.num_vertices];
                Vector2[] uvs = new Vector2[triShape.num_vertices];
                Vector4[] bone_weights = new Vector4[triShape.num_vertices];
                uint[] bone_indices = new uint[triShape.num_vertices];

                for (int v = 0; v < triShape.num_vertices; v++)
                {
                    positions[v] = Vector3.Zero;
                    for (int x = 0; x < 4; x++)
                    {
                        int i = triShape.vertex_data[v].bone_indices[x];
                        float weight = triShape.vertex_data[v].bone_weights[x];
                        positions[v] += bone_transforms[i] * triShape.vertex_data[v].vertex * weight;
                    }
                    uvs[v] = triShape.vertex_data[v].uv;
                    bone_weights[v] = new Vector4(
                        triShape.vertex_data[v].bone_weights[0],
                        triShape.vertex_data[v].bone_weights[1],
                        triShape.vertex_data[v].bone_weights[2],
                        triShape.vertex_data[v].bone_weights[3]);
                    bone_indices[v] = System.BitConverter.ToUInt32(triShape.vertex_data[v].bone_indices, 0);
                }

                this.vb_positions = Buffer.Create(device, BindFlags.VertexBuffer, positions);
                this.vb_uvs = Buffer.Create(device, BindFlags.VertexBuffer, uvs);
                this.vb_weights = Buffer.Create(device, BindFlags.VertexBuffer, bone_weights);
                this.vb_indices = Buffer.Create(device, BindFlags.VertexBuffer, bone_indices);
                this.ib = Buffer.Create(device, BindFlags.IndexBuffer, triShape.triangles);

                this.num_triangle_points = triShape.triangles.Length * 3;
            }
        }

        public string GetBoneName(int i)
        {
            ObjectRef node_ref = this.bones[i];
            NiNode node = header.GetObject<NiNode>(node_ref);
            return header.strings[node.name];
        }

        void TransformToMatrix(ref NiDump.Transform t, out Matrix m)
        {
            m = new Matrix
            {
                M11 = t.rotation.M11,
                M12 = t.rotation.M21,
                M13 = t.rotation.M31,
                M14 = 0,

                M21 = t.rotation.M12,
                M22 = t.rotation.M22,
                M23 = t.rotation.M32,
                M24 = 0,

                M31 = t.rotation.M13,
                M32 = t.rotation.M23,
                M33 = t.rotation.M33,
                M34 = 0,

                M41 = t.translation.X,
                M42 = t.translation.Y,
                M43 = t.translation.Z,
                M44 = 1
            };
        }

        public void GetBoneLocal(int i, out NiDump.Transform t)
        {
            ObjectRef node_ref = this.bones[i];
            NiNode node = header.GetObject<NiNode>(node_ref);
            node.self_ref = node_ref;

            NiDump.Transform node_local = node.GetLocalTransform(skin_instance.skeleton_root);
            NiDump.Transform bone_trans = bone_data.bones[i].transform;
            t = node_local * bone_trans;
        }

        public void GetBoneLocal(int i, out Matrix m)
        {
            NiDump.Transform t;
            GetBoneLocal(i, out t);
            TransformToMatrix(ref t, out m);
        }
    }
}
