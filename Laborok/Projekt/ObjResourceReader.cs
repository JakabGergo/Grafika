﻿using System.Globalization;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace Projekt
{
    //objektumok olvasasa a resource mappabol
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateObjKapuWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<int[]> objFacesNormal;
            List<int[]> objFacesTexture;
            List<float[]> objNormals;
            List<float[]> objTexture;

            // beolvassuk az objektumot
            ReadObjDataWithNormalsAndTexture(out objVertices, out objFaces, out objNormals, out objFacesNormal, out objFacesTexture, out objTexture, "kapu.obj");

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArraysNormals(faceColor, objVertices, objFaces, glVertices, glColors, glIndices, objNormals, objFacesNormal, objFacesTexture, objTexture, false);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices, false);
        }

        public static unsafe GlObject CreateObjWallWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<int[]> objFacesNormal;
            List<int[]> objFacesTexture;
            List<float[]> objNormals;
            List<float[]> objTexture;

            // beolvassuk az objektumot
            ReadObjDataWithNormalsAndTexture(out objVertices, out objFaces, out objNormals, out objFacesNormal, out objFacesTexture, out objTexture, "wall.obj");

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArraysNormals(faceColor, objVertices, objFaces, glVertices, glColors, glIndices, objNormals, objFacesNormal, objFacesTexture, objTexture, true);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices, true);
        }

        public static unsafe GlObject CreateObjPlayerWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<int[]> objFacesNormal;
            List<int[]> objFacesTexture;
            List<float[]> objNormals;
            List<float[]> objTexture;

            // beolvassuk az objektumot
            ReadObjDataWithNormalsAndTexture(out objVertices, out objFaces, out objNormals, out objFacesNormal, out objFacesTexture, out objTexture, "player.obj");

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArraysNormals(faceColor, objVertices, objFaces, glVertices, glColors, glIndices, objNormals, objFacesNormal, objFacesTexture, objTexture, false);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices, false);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices, bool textura)
        {
            if (!textura)
            {
                uint offsetPos = 0;
                uint offsetNormal = offsetPos + (3 * sizeof(float));
                uint vertexSize = offsetNormal + (3 * sizeof(float));

                uint vertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
                Gl.EnableVertexAttribArray(0);

                Gl.EnableVertexAttribArray(2);
                Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

                uint colors = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(1);

                uint indices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

                // release array buffer
                Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
                uint indexArrayLength = (uint)glIndices.Count;

                return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
            }
            else
            {
                uint offsetPos = 0;
                uint offsetNormal = offsetPos + (3 * sizeof(float));
                uint offsetTexture = offsetNormal + (3 * sizeof(float));
                uint vertexSize = offsetTexture + (2 * sizeof(float));

                uint vertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
                Gl.EnableVertexAttribArray(0);

                Gl.EnableVertexAttribArray(2);
                Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

                uint colors = Gl.GenBuffer();
                
                // set texture
                // create texture
                uint texture = Gl.GenTexture();
                // activate texture 0
                Gl.ActiveTexture(TextureUnit.Texture1);
                // bind texture
                Gl.BindTexture(TextureTarget.Texture2D, texture);

                var skyboxImageResult = ReadTextureImage("wall.jpg");
                var textureBytes = (ReadOnlySpan<byte>)skyboxImageResult.Data.AsSpan();
                // Here we use "result.Width" and "result.Height" to tell OpenGL about how big our texture is.
                Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)skyboxImageResult.Width,
                    (uint)skyboxImageResult.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureBytes);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                // unbinde texture
                Gl.BindTexture(TextureTarget.Texture2D, 0);

                Gl.EnableVertexAttribArray(3);
                Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexture);


                uint indices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

                // release array buffer
                Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
                uint indexArrayLength = (uint)glIndices.Count;

                return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl, texture);
            }
        }

        private static unsafe void CreateGlArraysFromObjArraysNormals(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices, List<float[]> objNormals, List<int[]> objFacesNormal, List<int[]> objFacesTexture, List<float[]> objTexture, bool textura)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            // egyszerre igy tudunk vegig menni a faceken
            // mert kellenek a csomopontok es a normalisok is
            int j = 0;
            foreach (var objFace in objFaces)
            {
                // vao frissitese, normalvektor hozzaadasa
                // process 3 vertices
                for (int i = 0; i < objFace.Length; ++i)
                {
                    //egy haromszog csomopontjai
                    var objVertex = objVertices[objFace[i] - 1];
                    //es a hozza tartozo normalis
                    var objNormal = objNormals[objFacesNormal[j][i] - 1];
                    var objT = objTexture[objFacesTexture[j][i] - 1];

                    // create gl description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    glVertex.AddRange(objNormal);
                    if (textura)
                    {
                        glVertex.AddRange(objT);
                    }


                    // check if vertex exists
                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    // add vertex to triangle indices
                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }
                j++;
            }
        }

        private static unsafe void ReadObjDataWithNormalsAndTexture(out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<int[]> objFacesNormal, out List<int[]> objFacesTexture, out List<float[]> objTexture, string fajlnev)
        {
            //objektum leirasanak beolvasasa
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objFacesTexture = new List<int[]>();
            objFacesNormal = new List<int[]>();
            objNormals = new List<float[]>();
            objTexture = new List<float[]>();

            //azert van zarojelbe, hogy hiba eseten alljon le
            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Projekt.Resources." + fajlnev))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    // ures sor vagy komment atugrasa
                    if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                        continue;

                    // kivesszuk az obj sor elso betujet, hogy vertex vagy face
                    var lineClassifier = line.Substring(0, line.IndexOf(' '));

                    if (lineClassifier != "v" && lineClassifier != "f" && lineClassifier != "vt" && lineClassifier != "vn")
                        continue;

                    // a sorokat vagjuk szokozok menten igy kapjuk meg a haromszogek koordinatait
                    var lineData = line.Substring(lineClassifier.Length).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;
                        case "f":
                            //igy vannak leirva facek
                            //f v1/vt1/vn1 v2/vt2/vn2 v3/vt3/vn3 v4/vt4/vn4
                            int[] face = new int[3];
                            int[] faceTexture = new int[3];
                            int[] faceNormal = new int[3];
                            for (int i = 0; i < 3; ++i)
                            {
                                string[] indices = lineData[i].Split('/');
                                face[i] = int.Parse(indices[0], CultureInfo.InvariantCulture);
                                faceTexture[i] = int.Parse(indices[1], CultureInfo.InvariantCulture);
                                faceNormal[i] = int.Parse(indices[2], CultureInfo.InvariantCulture);
                            }
                            // Add the first triangle
                            objFaces.Add(face);
                            objFacesTexture.Add(faceTexture);
                            objFacesNormal.Add(faceNormal);

                            if (lineData.Length > 3)
                            {
                                for (int i = 2; i < lineData.Length - 1; ++i)
                                {
                                    int[] additionalFace = new int[3];
                                    int[] additionalFaceTexture = new int[3];
                                    int[] additionalFaceNormal = new int[3];

                                    string[] indices1 = lineData[0].Split('/');
                                    string[] indices2 = lineData[i].Split('/');
                                    string[] indices3 = lineData[i + 1].Split('/');

                                    additionalFace[0] = int.Parse(indices1[0], CultureInfo.InvariantCulture);
                                    additionalFace[1] = int.Parse(indices2[0], CultureInfo.InvariantCulture);
                                    additionalFace[2] = int.Parse(indices3[0], CultureInfo.InvariantCulture);

                                    additionalFaceTexture[0] = int.Parse(indices1[1], CultureInfo.InvariantCulture);
                                    additionalFaceTexture[1] = int.Parse(indices2[1], CultureInfo.InvariantCulture);
                                    additionalFaceTexture[2] = int.Parse(indices3[1], CultureInfo.InvariantCulture);

                                    additionalFaceNormal[0] = int.Parse(indices1[2], CultureInfo.InvariantCulture);
                                    additionalFaceNormal[1] = int.Parse(indices2[2], CultureInfo.InvariantCulture);
                                    additionalFaceNormal[2] = int.Parse(indices3[2], CultureInfo.InvariantCulture);

                                    // Add the additional triangle
                                    objFaces.Add(additionalFace);
                                    objFacesTexture.Add(additionalFaceTexture);
                                    objFacesNormal.Add(additionalFaceNormal);
                                }
                            }
                            break;

                        case "vn":
                            float[] normal = new float[3];
                            for (int i = 0; i < normal.Length; ++i)
                                normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormals.Add(normal);
                            break;
                        case "vt":
                            if (lineData.Length > 2)
                            {
                                float[] texture = new float[3];
                                for (int i = 0; i < texture.Length; ++i)
                                    texture[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                                objTexture.Add(texture);
                            }
                            else
                            {
                                float[] texture = new float[2];
                                for (int i = 0; i < texture.Length; ++i)
                                {
                                    if (i == 1) 
                                    {
                                        texture[i] = 1 - float.Parse(lineData[i], CultureInfo.InvariantCulture);
                                    } else { texture[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture); }
                                }
                                objTexture.Add(texture);
                            }
                            break;
                        default:
                            throw new Exception("Unexpected obj component.");
                            break;
                    }
                }
            }
        }

        private static unsafe ImageResult ReadTextureImage(string textureResource)
        {
            //fuggoseg behozasa a kep betoltesere
            ImageResult result;
            using (Stream skyeboxStream
                = typeof(GlCube).Assembly.GetManifestResourceStream("Projekt.Resources." + textureResource))
                result = ImageResult.FromStream(skyeboxStream, ColorComponents.RedGreenBlueAlpha);

            return result;
        }
    }
}
