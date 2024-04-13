using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class RubicCubeElement
    {
        public GlCube glCube;
        
        //eredeti poziciok kezdesnel
        public float x;
        public float y;
        public float z;

        public Matrix4X4<float> modelMatrix;

        public bool helyenVan()
        {
            return (Math.Abs(x - modelMatrix.M41) < 0.1 && Math.Abs(y - modelMatrix.M42) < 0.1 && Math.Abs(z - modelMatrix.M43) < 0.1);
        }

        public RubicCubeElement(GlCube glCube, float x, float y, float z, Matrix4X4<float> modelMatrix)
        {
            this.glCube = glCube;
            this.x = x;
            this.y = y;
            this.z = z;
            this.modelMatrix = modelMatrix;
        }   
    }
}
