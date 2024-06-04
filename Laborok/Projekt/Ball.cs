using Silk.NET.Maths;

namespace Projekt
{
    internal class Ball
    {
        public GlObject glBall;
        
        //eredeti poziciok kezdesnel
        public float x;
        public float y;
        public float z;

        public Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity;

        public Vector3D<float> position = new Vector3D<float>(0, 1.02310574f, 0);

        public Matrix4X4<float> modelMatrix;

        public bool helyenVan()
        {
            return (Math.Abs(x - modelMatrix.M41) < 0.1 && Math.Abs(y - modelMatrix.M42) < 0.1 && Math.Abs(z - modelMatrix.M43) < 0.1);
        }

        public Ball(GlObject glBall, float x, float y, float z, Matrix4X4<float> modelMatrix)
        {
            this.glBall = glBall;
            this.x = x;
            this.y = y;
            this.z = z;
            this.modelMatrix = modelMatrix;
        }   
    }
}
