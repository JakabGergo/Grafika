using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;


namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();
        private static List<RubicCubeElement> rcElements;

        private const double AngleChangeStepSize = Math.PI / 180 * 3;

        //random generalashoz
        private static Random random = new Random();
        private static int rlepes = 0;
        private static List<(int, float)> randomLepesek = new();
        
        private static (int, float) lep = new();
        
        //pontositunk a forgatasok utan
        const float tolerance = 0.0001f;
        public static bool kirakva = true;

        //annimaciohoz kellenek
        private static bool forgat = false;
        private static float segedSzog = 0;
        private static float segedSzog2 = 0;

        private static IWindow window;
        private static IInputContext inputContext;
        private static ImGuiController controller;

        private static float Shininess = 50;

        private static GL Gl;

        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(750, 750);

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
                keyboard.KeyDown += Keyboard_KeyDown;
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

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
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

        private static void Keyboard_KeyPressed(IKeyboard keyboard)
        {
            if (keyboard.IsKeyPressed(Key.Left)) { cameraDescriptor.RotateAroundY(AngleChangeStepSize); }
            if (keyboard.IsKeyPressed(Key.Left)) { cameraDescriptor.RotateAroundY(AngleChangeStepSize); }
            if (keyboard.IsKeyPressed(Key.Right)) { cameraDescriptor.RotateAroundY(-AngleChangeStepSize); }
            if (keyboard.IsKeyPressed(Key.Down)) { cameraDescriptor.RotateAroundX(-AngleChangeStepSize); }
            if (keyboard.IsKeyPressed(Key.Up)) { cameraDescriptor.RotateAroundX(AngleChangeStepSize); }
            if (keyboard.IsKeyPressed(Key.Q)) { cameraDescriptor.MoveUp(0.5f); }
            if (keyboard.IsKeyPressed(Key.E)) { cameraDescriptor.MoveDown(0.5f); }
            if (keyboard.IsKeyPressed(Key.A)) { cameraDescriptor.StrafeRight(0.5f); }
            if (keyboard.IsKeyPressed(Key.D)) { cameraDescriptor.StrafeLeft(0.5f); }
            if (keyboard.IsKeyPressed(Key.W)) { cameraDescriptor.MoveForward(0.5f); }
            if (keyboard.IsKeyPressed(Key.S)) { cameraDescriptor.MoveBackward(0.5f); }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
                //elforgat narancs
                case Key.Keypad1:
                    lep = (1, float.Pi / 2);
                    forgat = true;
                    break;
                case Key.Keypad2:
                    lep = (1, -float.Pi / 2);
                    forgat = true;
                    break;
                //elforgat piros
                case Key.Keypad3:
                    lep = (2, float.Pi / 2);
                    forgat = true;
                    break;
                case Key.Keypad4:
                    lep = (2, -float.Pi / 2);
                    forgat = true;
                    break;
                //elforgat zold
                case Key.Keypad5:
                    lep = (3, float.Pi / 2);
                    forgat = true;
                    break;
                case Key.Keypad6:
                    lep = (3, -float.Pi / 2);
                    forgat = true;
                    break;
                //elforgat kek
                case Key.Keypad7:
                    lep = (4, float.Pi / 2);
                    forgat = true;
                    break;
                case Key.Keypad8:
                    lep = (4, -float.Pi / 2);
                    forgat = true;
                    break;
                //elforgat sarga
                case Key.Keypad9:
                    lep = (5, float.Pi / 2);
                    forgat = true;
                    break;
                case Key.Keypad0:
                    lep = (5, -float.Pi / 2);
                    forgat = true;
                    break;
                //elforgat feher
                case Key.Home:
                    lep = (6, float.Pi / 2);
                    forgat = true;
                    break;
                case Key.End:
                    lep = (6, -float.Pi / 2);
                    forgat = true;
                    break;
                //30 random forgatas generalasa
                case Key.R:
                    forgat = true;
                    for (int i = 0; i < 30; i++)
                    {
                        int randomNumber = random.Next(1, 7);
                        int elojel = random.Next(1, 3);
                        if (elojel == 1) { randomLepesek.Add((randomNumber, -float.Pi / 2)); }
                        else { randomLepesek.Add((randomNumber, float.Pi / 2)); }
                    }
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            var keyboard = window.CreateInput().Keyboards[0];
            Keyboard_KeyPressed(keyboard);

            cubeArrangementModel.AdvanceTime(deltaTime);

            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            // animacios valtozok -> sebesseg es a vegcel, amerre megy a forgatas
            float targetRotationAngle = lep.Item2;
            float rotationSpeed = MathF.PI / 2;

            //ha random van minden kockara elvegezzuk a 30 forgatast
            if (randomLepesek.Count != 0)
            {
                rotationSpeed = 3 * MathF.PI / 2;
                lep = randomLepesek[rlepes];
                targetRotationAngle = lep.Item2;
                forgat = true;
            }

            //ha nem ment le a forgatas
            ForgatasiSzogFrissitese(deltaTime, targetRotationAngle, rotationSpeed);

            bool kirakva = true;
            foreach (var cubeElement in rcElements)
            {
                kirakva &= cubeElement.helyenVan();
                DrawRubicCubeElement(cubeElement);
            }

            if (kirakva) { cubeArrangementModel.AnimationEnabeld = true; }
            else { cubeArrangementModel.AnimationEnabeld = false; }

            GombokKirajzolasa();

            controller.Render();
        }

        private static unsafe void GombokKirajzolasa()
        {
            // ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.Begin("Lighting properties", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            if (!forgat && rlepes == 0)
            {
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
                if (ImGui.Button("Narancs <-"))
                {
                    lep = (1, float.Pi / 2);
                    forgat = true;
                };
                if (ImGui.Button("Narancs ->"))
                {
                    lep = (1, -float.Pi / 2);
                    forgat = true;
                }
                ImGui.PopStyleColor();

                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                if (ImGui.Button("Piros ->"))
                {
                    lep = (2, float.Pi / 2);
                    forgat = true;
                }
                if (ImGui.Button("Piros <-"))
                {
                    lep = (2, -float.Pi / 2);
                    forgat = true;
                }
                ImGui.PopStyleColor();
                
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                if (ImGui.Button("Zold <-"))
                {
                    lep = (3, float.Pi / 2);
                    forgat = true;
                }
                if (ImGui.Button("Zold ->"))
                {
                    lep = (3, -float.Pi / 2);
                    forgat = true;
                }
                ImGui.PopStyleColor();
                
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
                if (ImGui.Button("Kek ->"))
                {
                    lep = (4, float.Pi / 2);
                    forgat = true;
                }
                if (ImGui.Button("Kek <-"))
                {
                    lep = (4, -float.Pi / 2);
                    forgat = true;
                }
                ImGui.PopStyleColor();
                
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                if (ImGui.Button("Sarga ->"))
                {
                    lep = (5, float.Pi / 2);
                    forgat = true;
                }
                if (ImGui.Button("Sarga <-"))
                {
                    lep = (5, -float.Pi / 2);
                    forgat = true;
                }
                ImGui.PopStyleColor();
                
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                ImGuiNET.ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                if (ImGui.Button("Feher ->"))
                {
                    lep = (6, float.Pi / 2);
                    forgat = true;
                }
                if (ImGui.Button("Feher <-"))
                {
                    lep = (6, -float.Pi / 2);
                    forgat = true;
                }
            }
            ImGui.PopStyleColor();
            ImGuiNET.ImGui.End();
        }

        private static unsafe void ForgatasiSzogFrissitese(double deltaTime, float targetRotationAngle, float rotationSpeed)
        {
            if (forgat)
            {
                //vege forgatasnak
                if (Math.Abs(segedSzog) >= Math.Abs(targetRotationAngle))
                {
                    if (randomLepesek.Count != 0)
                    {
                        rlepes++;
                        if (rlepes == 30)
                        {
                            rlepes = 0;
                            randomLepesek = new();
                        }
                    }
                    forgat = false;
                    segedSzog = 0.0f;
                }
                else
                {
                    segedSzog2 = segedSzog;
                    segedSzog += Math.Sign(targetRotationAngle) * rotationSpeed * (float)deltaTime;
                    //utolso forgatassal helyre igazitsuk
                    if (Math.Abs(segedSzog) - Math.Abs(targetRotationAngle) >= tolerance)
                    {
                        segedSzog = targetRotationAngle;
                    }
                }
            }
        }

        private static unsafe void DrawRubicCubeElement(RubicCubeElement cubeElement)
        {
            //ha annimacio alatt vagyunk, akkor kicsiket mozditunk, amig meg nem lesz a teljes forgatas
            if (forgat)
            {
                float szog = segedSzog - segedSzog2;

                if ((lep.Item1 == 1) && Math.Abs(cubeElement.modelMatrix.M41 - 1f) < tolerance)
                {
                    cubeElement.modelMatrix.M41 = 1;
                    cubeElement.modelMatrix *= Matrix4X4.CreateRotationX(szog);
                }
                if ((lep.Item1 == 2) && Math.Abs(cubeElement.modelMatrix.M41 + 1f) < tolerance)
                {
                    cubeElement.modelMatrix.M41 = -1;
                    cubeElement.modelMatrix *= Matrix4X4.CreateRotationX(szog);
                }
                if ((lep.Item1 == 3) && Math.Abs(cubeElement.modelMatrix.M43 - 1f) < tolerance)
                {
                    cubeElement.modelMatrix.M43 = 1;
                    cubeElement.modelMatrix *= Matrix4X4.CreateRotationZ(szog);
                }
                if ((lep.Item1 == 4) && Math.Abs(cubeElement.modelMatrix.M43 + 1f) < tolerance)
                {
                    cubeElement.modelMatrix.M43 = -1;
                    cubeElement.modelMatrix *= Matrix4X4.CreateRotationZ(szog);
                }
                if ((lep.Item1 == 5) && Math.Abs(cubeElement.modelMatrix.M42 - 1f) < tolerance)
                {
                    cubeElement.modelMatrix.M42 = 1;
                    cubeElement.modelMatrix *= Matrix4X4.CreateRotationY(szog);
                }
                if ((lep.Item1 == 6) && Math.Abs(cubeElement.modelMatrix.M42 + 1f) < tolerance)
                {
                    cubeElement.modelMatrix.M42 = -1;
                    cubeElement.modelMatrix *= Matrix4X4.CreateRotationY(szog);
                }
            }
            
            //luktetes
            Matrix4X4<float> rotLocY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution);
            var modelMatrixForCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            
            SetModelMatrix(cubeElement.modelMatrix * modelMatrixForCenterCube * rotLocY);
            Gl.BindVertexArray(cubeElement.glCube.Vao); // glCuve is mas
            Gl.DrawElements(GLEnum.Triangles, cubeElement.glCube.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
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
        }

        private static unsafe void SetUpObjects()
        {
            //0,0,0 mindennek a kozepe
            float[] xIndex = { 
                -1f,     1f, -1f,  0f, 1f, -1f, 0f, 1f, //kozepso lap
                -1f, 0f, 1f, -1f,  0f, 1f, -1f, 0f, 1f, //also lap
                -1f, 0f, 1f, -1f,  0f, 1f, -1f, 0f, 1f, //felso lap
            };
            float[] yIndex = { 
                 0f,       0f,  0f,  0f,  0f,  0f,  0f,  0f, //kozepso lap
                -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, //also lap
                 1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f, //felso lap
            };
            float[] zIndex = {
                0f,     0f, -1f, -1f, -1f, 1f, 1f, 1f,  //kozepso lap
                0f, 0f, 0f, -1f, -1f, -1f, 1f, 1f, 1f,  //also lap
                0f, 0f, 0f, -1f, -1f, -1f, 1f, 1f, 1f,  //felso lap
            };

            float[] feher = [1.0f, 1.0f, 1.0f, 1.0f];  //feher
            float[] zold = [0.0f, 1.0f, 0.0f, 1.0f];  //zold
            float[] narancs = [1.0f, 0.5f, 0.0f, 1.0f];  //narancs
            float[] sarga = [1.0f, 1.0f, 0.0f, 1.0f];  //sarga
            float[] kek = [0.0f, 0.0f, 1.0f, 1.0f];  //kek
            float[] piros = [1.0f, 0.0f, 0.0f, 1.0f]; //piros
            float[] fekete = [0.0f, 0.0f, 0.0f, 1.0f]; //fekete

            
            List<GlCube> szineKockak = new List<GlCube>();
            //kozepso oldal
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, piros, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, fekete, fekete, narancs));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, piros, fekete, kek, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, fekete, kek, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, fekete, kek, narancs));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, zold, piros, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, zold, fekete, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, zold, fekete, fekete, fekete, narancs));
            //also Oldal
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, piros, feher, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, feher, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, feher, fekete, narancs));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, piros, feher, kek, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, feher, kek, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, fekete, fekete, feher, kek, narancs));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, zold, piros, feher, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, zold, fekete, feher, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, fekete, zold, fekete, feher, fekete, narancs));
            //felso oldal
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, fekete, piros, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, fekete, fekete, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, fekete, fekete, fekete, fekete, narancs));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, fekete, piros, fekete, kek, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, fekete, fekete, fekete, kek, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, fekete, fekete, fekete, kek, narancs));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, zold, piros, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, zold, fekete, fekete, fekete, fekete));
            szineKockak.Add(GlCube.CreateCubeWithFaceColors(Gl, sarga, zold, fekete, fekete, fekete, narancs));

            Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.95f); // fix
            Matrix4X4<float> trans;

            rcElements = new List<RubicCubeElement>();
            for (int i = 0; i < xIndex.Length; i++)
            {
                //a modell matrixot is elmentjuk minden kockara
                trans = Matrix4X4.CreateTranslation(xIndex[i], yIndex[i], zIndex[i]); // pozicio valtozik
                rcElements.Add(new RubicCubeElement(szineKockak[i], xIndex[i], yIndex[i], zIndex[i], diamondScale * trans));
            }
        }

        private static void Window_Closing()
        {
            for (int i = 0; i < rcElements.Count; i++)
            {
                rcElements[i].glCube.ReleaseGlCube();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}