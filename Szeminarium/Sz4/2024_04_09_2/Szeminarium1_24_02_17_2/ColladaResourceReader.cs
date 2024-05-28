using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Silk.NET.Maths;
using Silk.NET.OpenGL;


namespace Szeminarium1_24_02_17_2
{
    // collada fajlba leirt modelleket olvas be
    internal class ColladaResourceReader
    {
        public static unsafe GlObject CreateColladaCubeWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;

            ReadColladaData(out objVertices, out objFaces);

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        public static unsafe GlObject CreateColladaBallWithColor(GL Gl, float[] faceColor, float[] faceColor2)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFacesPosition;
            List<int[]> objFacesNormal;
            List<float[]> objNormals;

            ReadBallColladaData(out objVertices, out objNormals, out objFacesPosition, out objFacesNormal);

            // itt rakunk normalist es szineket az objektumnak (mint eddig)
            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArraysNormals(faceColor, faceColor2, objVertices, glVertices, glColors, glIndices, objNormals, objFacesPosition, objFacesNormal);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe void ReadColladaData(out List<float[]> objVertices, out List<int[]> objFaces)
        {
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();

            // collada fajl kezelese mint xml dokumentum
            XmlDocument doc = new XmlDocument();
            doc.Load(@"D:\Egyetem\2023-2024 masodev\02_02_masodik felev\Grafika\Szeminarium\Sz4\2024_04_09_2\Szeminarium1_24_02_17_2\Resources\cube.dae");

            // ez kell ahhoz, hogy az xml fajlt megfeleloen tudjuk ertelmezni
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");

            //a megfelelo taggel rendelkezo csomopontok kivevese
            XmlNodeList vertexNodes = doc.SelectNodes("//c:source[@id='cube-vertex-positions']/c:float_array", namespaceManager);
            if (vertexNodes != null && vertexNodes.Count > 0)
            {
                // a kinyert csomopontban csak a pontok lesznek -> feldaraboljuk szokozok menten es 3-assaval elmentjuk
                string[] vertexData = vertexNodes[0].InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < vertexData.Length; i += 3)
                {
                    float[] vertex = new float[3];
                    vertex[0] = float.Parse(vertexData[i]);
                    vertex[1] = float.Parse(vertexData[i + 1]);
                    vertex[2] = float.Parse(vertexData[i + 2]);
                    objVertices.Add(vertex);
                }
            }

            // Olvassa be a face adatokat a megfelelo collada csomopontbol
            XmlNodeList faceNodes = doc.SelectNodes("//c:triangles/c:p", namespaceManager);
            if (faceNodes != null && faceNodes.Count > 0)
            {
                foreach (XmlNode faceNode in faceNodes)
                {
                    string[] faceData = faceNode.InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < faceData.Length; i += 3)
                    {
                        int[] face = new int[3];
                        face[0] = int.Parse(faceData[i]);
                        face[1] = int.Parse(faceData[i + 1]);
                        face[2] = int.Parse(faceData[i + 2]);
                        objFaces.Add(face);
                    }
                }
            }
        }

        private static unsafe void ReadBallColladaData(out List<float[]> objVertices, out List<float[]> objNormals, out List<int[]> objFacesPosition, out List<int[]> objFacesNormal)
        {
            objVertices = new List<float[]>();
            objNormals = new List<float[]>();
            objFacesPosition = new List<int[]>();
            objFacesNormal = new List<int[]>();
            float[] transformationMatrix = new float[16]; //transzformacios matrix egyelore nem hasznaljuk

            // collada fajl kezelese mint xml dokumentum
            XmlDocument doc = new XmlDocument();
            doc.Load(@"D:\Egyetem\2023-2024 masodev\02_02_masodik felev\Grafika\Szeminarium\Sz4\2024_04_09_2\Szeminarium1_24_02_17_2\Resources\ball.dae");

            // ez kell ahhoz, hogy az xml fajlt megfeleloen tudjuk ertelmezni
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");

            //transzformacios matrix beolvasasa
            XmlNode transformNode = doc.SelectSingleNode("//c:node/c:matrix", namespaceManager);
            if (transformNode != null)
            {
                string[] transformData = transformNode.InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < 16; i++)
                {
                    transformationMatrix[i] = float.Parse(transformData[i], CultureInfo.InvariantCulture);
                }
            }

            //a megfelelo taggel rendelkezo csomopontok kivevese
            XmlNodeList vertexNodes = doc.SelectNodes("//c:source[@id='Solid_001-mesh-positions']/c:float_array", namespaceManager);
            XmlNodeList normalNodes = doc.SelectNodes("//c:source[@id='Solid_001-mesh-normals']/c:float_array", namespaceManager);

            if (vertexNodes != null && vertexNodes.Count > 0 && normalNodes != null && normalNodes.Count > 0)
            {
                // a kinyert csomopontban csak a pontok lesznek -> feldaraboljuk szokozok menten es 3-assaval elmentjuk
                string[] vertexData = vertexNodes[0].InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] normalData = normalNodes[0].InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < vertexData.Length; i += 3)
                {
                    float[] vertex = new float[3];
                    float[] normal = new float[3];

                    vertex[0] = float.Parse(vertexData[i], CultureInfo.InvariantCulture);
                    vertex[1] = float.Parse(vertexData[i + 1], CultureInfo.InvariantCulture);
                    vertex[2] = float.Parse(vertexData[i + 2], CultureInfo.InvariantCulture);

                    normal[0] = float.Parse(normalData[i], CultureInfo.InvariantCulture);
                    normal[1] = float.Parse(normalData[i + 1], CultureInfo.InvariantCulture);
                    normal[2] = float.Parse(normalData[i + 2], CultureInfo.InvariantCulture);

                    //transzformacio alkalmazasa a vertexekre
                    float[] transformedVertex =
                    [
                        transformationMatrix[0] * vertex[0] + transformationMatrix[4] * vertex[1] + transformationMatrix[8] * vertex[2] + transformationMatrix[12],
                        transformationMatrix[1] * vertex[0] + transformationMatrix[5] * vertex[1] + transformationMatrix[9] * vertex[2] + transformationMatrix[13],
                        transformationMatrix[2] * vertex[0] + transformationMatrix[6] * vertex[1] + transformationMatrix[10] * vertex[2] + transformationMatrix[14],
                    ];
                    //objVertices.Add(vertex);
                    objVertices.Add(transformedVertex);
                    objNormals.Add(normal);
                }
            }

            // Olvassa be a face adatokat a megfelelo collada csomopontbol
            XmlNodeList faceNodes = doc.SelectNodes("//c:polylist/c:p", namespaceManager);
            if (faceNodes != null && faceNodes.Count > 0)
            {
                foreach (XmlNode faceNode in faceNodes)
                {
                    // ezek ugy vannak tarolva, hogy csucs, normalis, csucs, normalis, csucs, normalis, ... egy ilyen 3-as tesz ki egy face-t
                    string[] faceData = faceNode.InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < faceData.Length; i += 6)
                    {
                        int[] facePosition = new int[3];
                        int[] faceNormal = new int[3];
                        facePosition[0] = int.Parse(faceData[i]);
                        faceNormal[0] = int.Parse(faceData[i + 1]);
                        facePosition[1] = int.Parse(faceData[i + 2]);
                        faceNormal[1] = int.Parse(faceData[i + 3]);
                        facePosition[2] = int.Parse(faceData[i + 4]);
                        faceNormal[2] = int.Parse(faceData[i + 5]);
                        objFacesPosition.Add(facePosition);
                        objFacesNormal.Add(faceNormal);
                    }
                }
            }
        }

        //szeminarium kod, csak itt az indexeles 0-tol volta a collada fajlba
        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            foreach (var objFace in objFaces)
            {
                // normalvektor kiszamitasa
                var aObjVertex = objVertices[objFace[0]];
                var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                var bObjVertex = objVertices[objFace[1]];
                var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                var cObjVertex = objVertices[objFace[2]];
                var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);

                var normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));

                // vao frissitese, normalvektor hozzaadasa
                // process 3 vertices
                for (int i = 0; i < objFace.Length; ++i)
                {
                    var objVertex = objVertices[objFace[i]];

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

        private static unsafe void CreateGlArraysFromObjArraysNormals(float[] faceColor, float[] faceColor2, List<float[]> objVertices, List<float> glVertices, List<float> glColors, List<uint> glIndices, List<float[]> objNormals, List<int[]> objFacesPosition, List<int[]> objFacesNormal)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();

            // Iterate through the faces
            for (int j = 0; j < objFacesPosition.Count; j++)
            {
                var objFace = objFacesPosition[j];
                var normalFace = objFacesNormal[j];

                // Process 3 vertices per face
                for (int i = 0; i < objFace.Length; ++i)
                {
                    // Vertex and corresponding normal
                    var objVertex = objVertices[objFace[i]];
                    var objNormal = objNormals[normalFace[i]];

                    // Create GL description of vertex
                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(objVertex);
                    glVertex.AddRange(objNormal);

                    // Convert to a unique string key
                    var glVertexStringKey = string.Join(" ", glVertex);

                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        if (j < 21720)
                        {
                            glColors.AddRange(faceColor);
                        }
                        else
                        {
                            glColors.AddRange(faceColor2);
                        }
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    // Add vertex to triangle indices
                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }
            }
        }

        //szeminarium kod
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
    }
}
