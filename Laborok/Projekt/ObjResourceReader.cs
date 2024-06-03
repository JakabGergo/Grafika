using System.Globalization;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

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
            ReadObjDataWithNormalsAndTexture(out objVertices, out objFaces, out objNormals, out objFacesNormal, out objFacesTexture, out objTexture);

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArraysNormals(faceColor, objVertices, objFaces, glVertices, glColors, glIndices, objNormals, objFacesNormal, objFacesTexture, objTexture);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }


        //olyan, mint amikor kockat hoztunk letre, csak itt minden adott az obj fajlokbol
        public static unsafe GlObject CreateTeapotWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // ezekbe meg nincsenek normalisok
            List<float[]> objVertices;
            List<int[]> objFaces;

            // beolvassuk az objektumot
            ReadObjDataForTeapot(out objVertices, out objFaces);

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
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

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            foreach (var objFace in objFaces)
            {
                // normalvektor kiszamitasa
                var aObjVertex = objVertices[objFace[0] - 1];
                var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                var bObjVertex = objVertices[objFace[1] - 1];
                var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                var cObjVertex = objVertices[objFace[2] - 1];
                var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);

                var normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));

                // vao frissitese, normalvektor hozzaadasa
                // process 3 vertices
                for (int i = 0; i < objFace.Length; ++i)
                {
                    var objVertex = objVertices[objFace[i] - 1];

                    // create gl description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    glVertex.Add(normal.X);
                    glVertex.Add(normal.Y);
                    glVertex.Add(normal.Z);
                    // add textrure, color

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
            }
        }

        private static unsafe void CreateGlArraysFromObjArraysNormals(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices, List<float[]> objNormals, List<int[]> objFacesNormal, List<int[]> objFacesTexture, List<float[]> objTexture)
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

        private static unsafe void ReadObjDataForTeapot(out List<float[]> objVertices, out List<int[]> objFaces)
        {
            //objektum leirasanak beolvasasa
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();

            //azert van zarojelbe, hogy hiba eseten alljon le
            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Resources.teapot.obj"))
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
                            int[] face = new int[3];
                            for (int i = 0; i < face.Length; ++i)
                                face[i] = int.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objFaces.Add(face);
                            break;
                        default:
                            throw new Exception("Unexpected obj component.");
                            break;
                    }
                }
            }
        }

        private static unsafe void ReadObjDataWithNormalsAndTexture(out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<int[]> objFacesNormal, out List<int[]> objFacesTexture, out List<float[]> objTexture)
        {
            //objektum leirasanak beolvasasa
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objFacesTexture = new List<int[]>();
            objFacesNormal = new List<int[]>();
            objNormals = new List<float[]>();
            objTexture = new List<float[]>();

            //azert van zarojelbe, hogy hiba eseten alljon le
            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Projekt.Resources.kapu.obj"))
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
                            float[] texture = new float[3];
                            for (int i = 0; i < texture.Length; ++i)
                                texture[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objTexture.Add(texture);
                            break;
                        default:
                            throw new Exception("Unexpected obj component.");
                            break;
                    }
                }
            }
        }
    }
}
