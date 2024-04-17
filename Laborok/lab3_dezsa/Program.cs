﻿using System.Diagnostics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace lab3_dezsa
{
    internal static class Program
    {
        //ambientStrengt atirasa vec3-ra es jojjon fel shader valtozonak es ui-val valtoztatni ezeket
        private static CameraDescriptor cameraDescriptor = new();
        private const double AngleChangeStepSize = Math.PI / 180 * 2;

        private static IWindow window;

        //gombokat itt kotottuk be
        private static IInputContext inputContext;

        private static GL Gl;

        //grafikus felulet
        private static ImGuiController controller;

        private static uint program;
        private static uint programGourard;
        private static bool phongArnyalas = true;

        private static Dezsa dezsa;
        private static Dezsa dezsa2;
        private static float d = (float)(1 / (2 * Math.Tan(Math.PI / 18)));


        //anyag tulajdonsag
        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        //normalvektor behuzasa
        //out valtozokat a program tovabbadja a fragment shadernek
        //vilag pontjait is tovabb adjuk
        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outNormal = uNormal*vNorm;
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
        }
        ";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        //ambientStrength beallitasa -> feny erossege
        //fenyszine lightColor, uniformoknak valtozo
        //diffuseStregth -> minden iranyba ugyanolyan intenzitassal szorunk, mindegy honnan nezzuk a pontot ugyanugy nez ki
        //-> ehhez kell feluleti normalis -> kockak modositasa GlCube osztaly
        //in valtozokat a vertex shadertol kapjuk
        //diff, ha a felulet hatulrol kapja a fenyt, akkor 0
        //spekularis feny komponens: mashogy tunjon egy pont szine, kulonbozo nezopontbol
        private static readonly string FragmentShaderSource = @"
        #version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;

        out vec4 FragColor;

		in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = 0.2;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 0.3;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            float specularStrength = 0.5;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = sign(max(dot(norm, lightDir), 0)) * pow(max(dot(viewDir, reflectDir), 0.0), shininess) /max(dot(norm,viewDir), -dot(norm,lightDir));
            vec3 specular = specularStrength * spec * lightColor;  

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";



        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Gouard arnyalas
        private static readonly string VertexShaderSourceGourard = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outNormal = uNormal*vNorm;
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
        }
        ";

        private static readonly string FragmentShaderSourceGouard = @"
        #version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;

        out vec4 FragColor;

		in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = 0.5;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 0.3;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            float specularStrength = 0.5;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = sign(max(dot(norm, lightDir), 0)) * pow(max(dot(viewDir, reflectDir), 0.0), shininess) /max(dot(norm,viewDir), -dot(norm,lightDir));
            vec3 specular = specularStrength * spec * lightColor;  

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Gouard arnyalas

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab3_1";
            windowOptions.Size = new Vector2D<int>(500, 500);

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
           // foreach (var keyboard in inputContext.Keyboards)
           // {
           //     keyboard.KeyDown += Keyboard_KeyDown;
           // }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += newSize =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(newSize);
            };


            Gl.ClearColor(System.Drawing.Color.White);

            SetUpObjects();

            LinkProgram();
            LinkProgramGourard();

            //Gl.Enable(EnableCap.CullFace);

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

        private static void LinkProgramGourard()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSourceGourard);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSourceGouard);
            Gl.CompileShader(fshader);

            programGourard = Gl.CreateProgram();
            Gl.AttachShader(programGourard, vshader);
            Gl.AttachShader(programGourard, fshader);
            Gl.LinkProgram(programGourard);
            Gl.GetProgram(programGourard, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(programGourard)}");
            }
            Gl.DetachShader(programGourard, vshader);
            Gl.DetachShader(programGourard, fshader);
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
            if (keyboard.IsKeyPressed(Key.A)) { cameraDescriptor.StrafeRight(0.4f); }
            if (keyboard.IsKeyPressed(Key.D)) { cameraDescriptor.StrafeLeft(0.4f); }
            if (keyboard.IsKeyPressed(Key.W)) { cameraDescriptor.MoveForward(0.4f); }
            if (keyboard.IsKeyPressed(Key.S)) { cameraDescriptor.MoveBackward(0.4f); }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            //cubeArrangementModel.AdvanceTime(deltaTime);

            var keyboard = window.CreateInput().Keyboards[0];
            Keyboard_KeyPressed(keyboard);

            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            if (phongArnyalas)
            {
                Gl.UseProgram(program);
            }
            else
            {
                Gl.UseProgram(programGourard);
            }

            SetViewMatrix();
            SetProjectionMatrix();

            //feny szinenek beallitasa
            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();

            for(int i = 0; i < 18; i++)
            {
                DrawDezsa((float)(2 * i * (Math.PI / 18)));
            }
            for (int i = 0; i < 18; i++)
            {
                DrawDezsa2((float)(2 * i * (Math.PI / 18)));
            }

            //ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.Begin("Lighting properties",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);
            
            ImGuiNET.ImGui.RadioButton("Phong arnyalas", phongArnyalas);
            if (ImGuiNET.ImGui.IsItemClicked())
            {
                phongArnyalas = true;
            }
            ImGuiNET.ImGui.RadioButton("Gourard arnyalas", !phongArnyalas);
            if (ImGuiNET.ImGui.IsItemClicked())
            {
                phongArnyalas = false;
            }
            ImGuiNET.ImGui.End();

            controller.Render();
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

            Gl.Uniform3(location, 0f, 1f, 5f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
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


        private static unsafe void DrawDezsa(float forgatasiSzog)
        {
            Matrix4X4<float> roty = Matrix4X4.CreateRotationY(forgatasiSzog);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(0, 0, d); // pozicio valtozik

            var modelMatrix = trans * roty;
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(dezsa.Vao);
            Gl.DrawElements(GLEnum.Triangles, dezsa.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawDezsa2(float forgatasiSzog)
        {
            Matrix4X4<float> roty = Matrix4X4.CreateRotationY(forgatasiSzog);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(0, 2, d); // pozicio valtozik

            var modelMatrix = trans * roty;
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(dezsa2.Vao);
            Gl.DrawElements(GLEnum.Triangles, dezsa2.IndexArrayLength, GLEnum.UnsignedInt, null);
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

            //vertex shader uNormal matrix beallitas
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

        private static unsafe void SetUpObjects()
        {
            dezsa = Dezsa.CreateDezsa(Gl);
            dezsa2 = Dezsa.CreateDezsa2(Gl);
        }

        private static void Window_Closing()
        {
            dezsa.ReleaseDezsa();
            dezsa2.ReleaseDezsa();
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