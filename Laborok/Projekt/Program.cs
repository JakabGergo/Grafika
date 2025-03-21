﻿using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Projekt
{
    internal class Program
    {
        private static IWindow window;

        private static IInputContext inputContext;

        private static GL Gl;

        private static ImGuiController controller;

        private static uint program;

        private static CameraDescriptor cameraDescriptor = new();

        private static GlObject colladaBall;
        private static Ball ball;
        private static GlObject kapu;
        private static GlObject kapu2;
        private static GlObject table;
        private static List<Player> players;


        private static GlObject wall;
        private static GlCube skyBox;

        private static bool felsoNezet = true;
        private static bool fejlesztoiNezet = false;
        private static bool labdakovetoNezet = false;

        private const double AngleChangeStepSize = Math.PI / 180 * 2;
        private static float oldalraEltolas = 0.25f;
        private static bool balra = true;

        public const float LabdaMagassag = (1.02310574f)/2;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Projekt";
            windowOptions.Size = new Vector2D<int>(800, 800);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            // set up input handling
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                //keyboard.KeyDown += Keyboard_KeyDown;
            }
            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };


            Gl.ClearColor(System.Drawing.Color.White);

            SetUpObjects();

            LinkProgram();

            //Gl.Enable(EnableCap.CullFace);
            //attetszoseg engedelyezese
            //Gl.Enable(EnableCap.Blend);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static string ReadShader(string shaderFileName)
        {
            //shaderek beolvasasa
            using (Stream shaderStream = typeof(Program).Assembly.GetManifestResourceStream("Projekt.Shaders." + shaderFileName))
            using (StreamReader shaderReader = new StreamReader(shaderStream))
                return shaderReader.ReadToEnd();
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            var keyboard = inputContext.Keyboards[0];
            Keyboard_KeyPressed(keyboard);

            //cubeArrangementModel.AdvanceTime(deltaTime);

            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();
            
            DrawPulsingColladaBall();
            DrawKapu();
            DrawKapu2();
            if (balra)
            {
                oldalraEltolas += 0.25f;
            }
            else
            {
                oldalraEltolas -= 0.25f;
            }
            if(Math.Abs(oldalraEltolas) >= 7)
            {
                balra = !balra;
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (i == 0 || (i >= 3 && i <= 7))
                {
                    if (balra)
                    {
                        players[i].x += 0.25f;
                    }
                    else
                    {
                        players[i].x -= 0.25f;
                    }
                    DrawPlayer(players[i], true);
                }
                else 
                {
                    if (balra)
                    {
                        players[i].x -= 0.25f;
                    }
                    else
                    {
                        players[i].x += 0.25f;
                    }
                    DrawPlayer(players[i], false); 
                }
            }
            DrawWallS();
            DrawSkyBox();

            //ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.Begin("Lighting properties",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);


            ImGuiNET.ImGui.RadioButton("Fejlesztoi nezet", fejlesztoiNezet);
            if (ImGuiNET.ImGui.IsItemClicked()){
                fejlesztoiNezet = true;
                felsoNezet = false;
                labdakovetoNezet = false;
            }
            ImGuiNET.ImGui.RadioButton("Felso nezet", felsoNezet);
            if (ImGuiNET.ImGui.IsItemClicked())
            {
                fejlesztoiNezet = false;
                felsoNezet = true;
                labdakovetoNezet = false;
            }
            ImGuiNET.ImGui.RadioButton("Labda koveto nezet", labdakovetoNezet);
            if (ImGuiNET.ImGui.IsItemClicked())
            {
                fejlesztoiNezet = false;
                felsoNezet = false;
                labdakovetoNezet = true;
            }
            ImGuiNET.ImGui.End();

            for (int i = 0; i < players.Count; i++)
            {
                float tav = (float)Math.Sqrt(Math.Pow(players[i].x - ball.position.X, 2) + Math.Pow(players[i].z - ball.position.Z, 2));
                if (tav < 1)
                {
                    cameraDescriptor.PositionKoveto = new Vector3D<float>(0, 3, 22);
                    ball.position.X = 0;
                    ball.position.Y = 1.02310574f;
                    ball.position.Z = 17;
                }
            }



                controller.Render();
        }

        private static void Window_Closing()
        {            
            colladaBall.ReleaseGlObject();
            kapu.ReleaseGlObject();
            kapu2.ReleaseGlObject();
            for(int i = 0; i < players.Count; i++)
            {
                players[i].glPlayer.ReleaseGlObject();
            }
        }

        private static unsafe void SetUpObjects()
        {

            float[] face1Color = [1f, 0f, 0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            //fekete feher labda letrehozasa
            //+-1.02310574f ez a legmagasabb es legalacsonyabb Y koordinata
            var translationForBall = Matrix4X4.CreateTranslation(new Vector3D<float>(0, LabdaMagassag, 0));
            var pulsingScaleMatrix = Matrix4X4.CreateScale(1f);
            var modelMatrixForBall = pulsingScaleMatrix * translationForBall;
            colladaBall = ColladaResourceReader.CreateColladaBallWithColor(Gl, [1f, 1f, 1f, 1.0f], [0f, 0f, 0f, 1.0f]);
            ball = new Ball(colladaBall, 0f, 1f, 0f, modelMatrixForBall);
            
            kapu = ObjResourceReader.CreateObjKapuWithColor(Gl, [1f, 1f, 1f, 1.0f]);
            kapu2 = ObjResourceReader.CreateObjKapuWithColor(Gl, [1f, 1f, 1f, 1.0f]);
            
            GlObject player = ObjResourceReader.CreateObjPlayerWithColor(Gl, face1Color);
            float[] xIndex = {  -0.5f,   5,   -5, -11.5f, -6f,  -0.5f, 6f,  11.5f, -7.5f, -0.5f, 6.5f};
            float[] zIndex = { -17.5f, -10, -10,   -2,    -2,     -2, -2,     -2,     6,     6,    6};

            var pulsingScaleMatrixForPlayer = Matrix4X4.CreateScale(0.03f);
            var rotationYMatrix = Matrix4X4.CreateRotationX((float)(-Math.PI / 2));
            var translationForPlayer = Matrix4X4.CreateTranslation(new Vector3D<float>(xIndex[0], 0, zIndex[0]));

            players = new List<Player>();
            players.Add(new Player(player, xIndex[0], 0, zIndex[0], pulsingScaleMatrixForPlayer * rotationYMatrix * translationForPlayer));
            for (int i = 1; i < 11; i++)
            {
                translationForPlayer = Matrix4X4.CreateTranslation(new Vector3D<float>(xIndex[i], 0, zIndex[i]));
                players.Add(new Player(player, xIndex[i], 0, zIndex[i], pulsingScaleMatrixForPlayer * rotationYMatrix * translationForPlayer));
            }

            skyBox = GlCube.CreateInteriorCube(Gl, "");
            wall = ObjResourceReader.CreateObjWallWithColor(Gl, face6Color);

            float[] tableColor = [82f / 256f,   // Red component
                                  110f / 256f,  // Green component
                                  35f / 256f,   // Blue component
                                  1f];
            table = GlCube.CreateSquare(Gl, tableColor);
        }

        private static unsafe void SetViewMatrix()
        {
            //fejlesztoi nezet
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            if (felsoNezet)
            {
                viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.FelsoNezet, Vector3D<float>.Zero, cameraDescriptor.UpVector);
            } else if (labdakovetoNezet)
            {
                viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.PositionKoveto, ball.position, cameraDescriptor.UpVector);
            }
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 1000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 1000f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }
            if (felsoNezet)
            {
                Gl.Uniform3(location, cameraDescriptor.FelsoNezet.X, cameraDescriptor.FelsoNezet.Y, cameraDescriptor.FelsoNezet.Z);
                CheckError();
            }
            else if(fejlesztoiNezet) 
            {
                Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
                CheckError();
            }
            else
            {
                //koveto nezet
                Gl.Uniform3(location, cameraDescriptor.PositionKoveto.X, cameraDescriptor.PositionKoveto.Y, cameraDescriptor.PositionKoveto.Z);
                CheckError();
            }
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void DrawPulsingColladaBall()
        {
            var translation = Matrix4X4.CreateTranslation(ball.position);
            var pulsingScaleMatrix = Matrix4X4.CreateScale(0.5f);
            var modelMatrixForBall = ball.modelMatrix * pulsingScaleMatrix * ball.rotationMatrix * translation;

            modelMatrixForBall.M42 = LabdaMagassag;
            SetModelMatrix(modelMatrixForBall);
            Gl.BindVertexArray(ball.glBall.Vao);
            Gl.DrawElements(GLEnum.Triangles, ball.glBall.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            var modelMatrixForTable = Matrix4X4.CreateScale(1f, 1f, 1f);

            SetModelMatrix(modelMatrixForTable);
            Gl.BindVertexArray(table.Vao);
            Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawKapu()
        {
            // set material uniform to rubber
            var translationForGoal = Matrix4X4.CreateTranslation(new Vector3D<float>(5.5f, 0, -20));
            var pulsingScaleMatrix = Matrix4X4.CreateScale(0.025f);
            var rotationYMatrix = Matrix4X4.CreateRotationY((float)Math.PI);

            var modelMatrixForGoal = pulsingScaleMatrix * rotationYMatrix * translationForGoal;

            SetModelMatrix(modelMatrixForGoal);
            Gl.BindVertexArray(kapu.Vao);
            Gl.DrawElements(GLEnum.Triangles, kapu.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawKapu2()
        {
            // set material uniform to rubber
            var translationForCenterCube = Matrix4X4.CreateTranslation(new Vector3D<float>(-7, 0, 20));
            var pulsingScaleMatrix = Matrix4X4.CreateScale(0.025f);

            var modelMatrixForCenterCube = pulsingScaleMatrix * translationForCenterCube;

            SetModelMatrix(modelMatrixForCenterCube);
            Gl.BindVertexArray(kapu2.Vao);
            Gl.DrawElements(GLEnum.Triangles, kapu2.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawPlayer(Player player, bool jobbra)
        {
            var translationForPlayer = Matrix4X4.CreateTranslation(new Vector3D<float>(-oldalraEltolas, 0, 0));
            if (jobbra)
            {
                translationForPlayer = Matrix4X4.CreateTranslation(new Vector3D<float>(oldalraEltolas, 0, 0));
            }
            SetModelMatrix(player.modelMatrix * translationForPlayer);
            Gl.BindVertexArray(player.glPlayer.Vao);
            Gl.DrawElements(GLEnum.Triangles, player.glPlayer.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawWallS()
        {
            float[] transX = { -20,  20, 9.5f, 9.5f };
            float[] transY = { -18.8f, -18.8f, -16, -16 };
            float[] transZ = {  11f, -11, -22.5f, 22.5f };
            float[] rotAngle = { (float)(Math.PI) / 2, (float)(-Math.PI / 2), (float)(Math.PI), (float)(Math.PI)};
            float[] scale = { 11.4f, 11.4f, 10, 10 };

            for (int i = 0; i < 4; i++)
            {
                var translationForPlayer = Matrix4X4.CreateTranslation(new Vector3D<float>(transX[i], transY[i], transZ[i]));
                var rotY = Matrix4X4.CreateRotationY(rotAngle[i]);
                Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(scale[i]);
                SetModelMatrix(modelMatrix * rotY * translationForPlayer);

                Gl.BindVertexArray(wall.Vao);

                // textura letrehozasa
                int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
                if (textureLocation == -1)
                {
                    throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
                }
                // set texture 0
                Gl.Uniform1(textureLocation, 1);

                //a skybox valtozoba betoltjuk a texturat
                //taxtura aktivalas utan mindent az adott texturaval vegzunk
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
                Gl.BindTexture(TextureTarget.Texture2D, wall.Texture.Value);

                Gl.DrawElements(GLEnum.Triangles, wall.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);

                CheckError();
                Gl.BindTexture(TextureTarget.Texture2D, 0);
                CheckError();
            }
        }

        private static unsafe void DrawSkyBox()
        {
            //ez egy nagy kocka lesz
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(800f);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(skyBox.Vao);

            // textura letrehozasa
            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);

            //a skybox valtozoba betoltjuk a texturat
            //taxtura aktivalas utan mindent az adott texturaval vegzunk
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static void Keyboard_KeyPressed(IKeyboard keyboard)
        {
            if (fejlesztoiNezet)
            {
                if (keyboard.IsKeyPressed(Key.Left)) { cameraDescriptor.RotateAroundY(AngleChangeStepSize); }
                if (keyboard.IsKeyPressed(Key.Left)) { cameraDescriptor.RotateAroundY(AngleChangeStepSize); }
                if (keyboard.IsKeyPressed(Key.Right)) { cameraDescriptor.RotateAroundY(-AngleChangeStepSize); }
                if (keyboard.IsKeyPressed(Key.Down)) { cameraDescriptor.RotateAroundX(-AngleChangeStepSize); }
                if (keyboard.IsKeyPressed(Key.Up)) { cameraDescriptor.RotateAroundX(AngleChangeStepSize); }
                if (keyboard.IsKeyPressed(Key.Q)) { cameraDescriptor.MoveUp(0.5f); }
                if (keyboard.IsKeyPressed(Key.E)) { cameraDescriptor.MoveDown(0.5f); }
                if (keyboard.IsKeyPressed(Key.A)) { cameraDescriptor.StrafeRight(0.4f); }
                if (keyboard.IsKeyPressed(Key.D)) { cameraDescriptor.StrafeLeft(0.4f); }
                if (keyboard.IsKeyPressed(Key.W)) { cameraDescriptor.MoveForward(0.4f); }
                if (keyboard.IsKeyPressed(Key.S)) { cameraDescriptor.MoveBackward(0.4f); }
            }
            
            if (keyboard.IsKeyPressed(Key.I))
            {
                ball.rotationMatrix *= Matrix4X4.CreateRotationX((float)(-Math.PI / 25));
                if (ball.position.Z >= -21.6)
                {
                    ball.position += new Vector3D<float>(0, 0, -0.3f);
                    cameraDescriptor.updatePositionKoveto(new Vector3D<float>(0, 0, -0.3f));
                }
            }
            if (keyboard.IsKeyPressed(Key.K))
            {
                ball.rotationMatrix *= Matrix4X4.CreateRotationX((float)(Math.PI / 25));
                if (ball.position.Z <= 21.6f)
                {
                    ball.position += new Vector3D<float>(0, 0, 0.3f);
                    cameraDescriptor.updatePositionKoveto(new Vector3D<float>(0, 0, 0.3f));
                }
            }
            if (keyboard.IsKeyPressed(Key.J))
            {
                ball.rotationMatrix *= Matrix4X4.CreateRotationZ((float)(Math.PI / 25));
                if (ball.position.X > -19)
                {
                    ball.position += new Vector3D<float>(-0.3f, 0, 0);
                    cameraDescriptor.updatePositionKoveto(new Vector3D<float>(-0.3f, 0, 0));
                }
            }
            if (keyboard.IsKeyPressed(Key.L))
            {
                ball.rotationMatrix *= Matrix4X4.CreateRotationZ((float)(-Math.PI / 25));
                if ((ball.position.X < 19))
                {
                    ball.position += new Vector3D<float>(0.3f, 0, 0);
                    cameraDescriptor.updatePositionKoveto(new Vector3D<float>(0.3f, 0, 0));
                }
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                /*
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                    ;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.D:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;*/
            }
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }

    }
}
