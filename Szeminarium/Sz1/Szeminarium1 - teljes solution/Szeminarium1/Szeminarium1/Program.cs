using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        //ablak letrehozasa, rajzolas nelkul
        private static IWindow graphicWindow;

        //ezzel rajzolunk
        private static GL Gl;

        private static uint program;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
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
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            //hozzaadjuk ezeket a letrejott ablakhoz
            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Load()
        {
            //csak egyszer fut le -> egyszeri beallitasokat itt kell megoldani
            //Console.WriteLine("Loaded");

            //letrejon egy grafikus kontextus -> ezzel rajzolok
            Gl = graphicWindow.CreateOpenGL();

            //parameterek lehetnek szamok is, hatterszin beallitasa -> ebbol meg nem lesz rajzolas -> kell frissiteni az alkalmazast
            Gl.ClearColor(System.Drawing.Color.White);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            //shadereknek beallitjuk a forraskodot
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            //shader helyessegenek ellenorzese
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            //kod ami lekell fusson -> shadereket fellehet szabaditani
            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL -> nem teszunk grafikus dolgokat -> csak a modellt modositsuk pl. auto pozicio modositasa
            // make it threadsave -> nem szalbiztos

            //folyamatosan kiir -> tobbszor folyamatosan meghivodik
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //folyamatosan kiir -> tobbszor folyamatosan meghivodik
            //Console.WriteLine($"Render after {deltaTime} [s]");

            //tisztitjuk az ablakot(minden szint torlunk) -> mindig igy kezdjuk a rendert
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            //a vertez tombot rakossuk a GL kornyezetre -> GL ezt fogja ezentul kezelni
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            /*szeminarium kodja
             * 4 pont, amivel dolgozunk
             * 
            float[] vertexArray = new float[] {
                -0.5f, -0.5f, 0.0f,
                +0.5f, -0.5f, 0.0f,
                 0.0f, +0.5f, 0.0f,
                 1f, 1f, 0f
            };

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] { 
                0, 1, 2,
                2, 1, 3
            };
            */

            float[] vertexArray = new float[] {
                //jobb oldal 0 1 2 3
                0.0f, 0.0f, 0.0f,  //kozep kozep
                0.0f, -0.5f, 0.0f,//kozep lent
                +0.5f, -0.25f, 0.0f, //jobb also
                +0.5f, +0.25f, 0.0f, //jobb felso

                //bal oldal 4 5 6 7
                0.0f, 0.0f, 0.0f, //kozep kozep
                0.0f, -0.5f, 0.0f,//kozep lent
                -0.5f, -0.25f, 0.0f,//bal lent
                -0.5f, +0.25f, 0.0f,//bal fent

                //teto 8 9 10 11
                0.0f, 0.0f, 0.0f,  //kozep kozep
                -0.5f, +0.25f, 0.0f,//bal fent
                +0.5f, +0.25f, 0.0f, //jobb felso
                +0.0f, 0.5f, 0.0f, //kozep fent

                //vonalak 
                0.0f, 0.0f, 0.0f,  //kozep kozep 12
                0.0f, -0.5f, 0.0f,//kozep lent 13
                +0.5f, -0.25f, 0.0f, //jobb also 14
                +0.5f, +0.25f, 0.0f, //jobb felso 15
                -0.5f, -0.25f, 0.0f,//bal lent 16
                -0.5f, +0.25f, 0.0f,//bal fent 17
                +0.0f, 0.5f, 0.0f, //kozep fent 18

                0.0f, -0.32f, 0.0f,//kozep felezok 20
                0.0f, -0.16f, 0.0f,//kozep felezok 19

                +0.5f, -0.09f, 0.0f, //jobb felezok 21
                +0.5f, +0.07f, 0.0f, //jobb felezok 22

                -0.5f, -0.08f, 0.0f,//bal lent felezo 23
                -0.5f, +0.08f, 0.0f,//bal fent felezo 24

                //felezo jobb fent
                0.166f, 0.0833f, 0.0f,  //kozep kozep 25
                +0.333f, +0.16f, 0.0f, //jobb felso 26

                //felezo jobb lent
                +0.166f, -0.416f, 0.0f, //jobb also 27
                0.3333f, -0.333f, 0.0f,//kozep lent 28

                //felezo bal fent
                -0.166f, 0.0833f, 0.0f,  //29
                -0.333f, +0.166f, 0.0f,//30

                //felezo bal lent
                -0.166f, -0.4166f, 0.0f, //31
                -0.333f, -0.333f, 0.0f,// 32

                //felezo bal hatso
                -0.166f, 0.4166f, 0.0f, //33
                -0.333f, +0.333f, 0.0f,// 34

                //felezo jobb hatso
                +0.166f, +0.4166f, 0.0f, //35
                +0.333f, +0.333f, 0.0f, //36
            };

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                
                1.0f, 1.0f, 1.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,
            };

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,
                
                4, 5, 6,
                4, 6, 7,

                8, 9 , 10,
                9, 10, 11,

                12, 13,
                14, 15,
                16, 17,
                13, 16,
                13, 14,
                17, 18,
                15, 18,
                12, 15,
                12, 17,
                19, 21,
                20, 22,
                19, 23,
                20, 24,
                25, 27,
                26, 28,
                29, 31,
                30, 32,
                33, 26,
                35, 30,
                34, 25,
                36, 29,
            };


            //bufferen keresztul kerulnek be az adatok a GL-be
            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            //fent betoltott buffer tartalma kerul be
            //readonly -> memoriabol van es veletlenul se irjuk at
            //spam -> memoria melyik reszet akarjuk hasznalni -> kiindexeles elkerulese
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            //hanyadik atributum, meret, tipus, normalized ertek, stride: ha ugy lenne hogy 3 kordinata 3 pont
            //bekell irni az unsafet a fuggveny fejlecbe -> properties-nel: unsafe enable
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);

            //szinekre vonatkozo beallitasok, enelkul nem jelenik meg semmi
            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            //a kov. 3 sor adja a szint az alakzatnak, ezek nelkul full fekete lesz
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            
            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            //buffer felszabaditada -> mindig kell!!
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            //shaderek ide lettek berakva es ezt hasznaljuk
            Gl.UseProgram(program);
            
            //miket rajzolok ki
            //mode,count, type. idexek
            Gl.DrawElements(GLEnum.Triangles, 18, GLEnum.UnsignedInt, null); // we used element buffer
            Gl.DrawElements(GLEnum.Lines, (uint)indexArray.Length, GLEnum.UnsignedInt, (void*)(18 * sizeof(uint)));
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            //kell ezeket torolni
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
        }
    }
}
