using Silk.NET.Maths;

namespace Projekt
{
    internal class Player
    {
        public GlObject glPlayer;

        //eredeti poziciok kezdesnel
        public float x;
        public float y;
        public float z;

        public Matrix4X4<float> modelMatrix;

        public bool helyenVan()
        {
            return (Math.Abs(x - modelMatrix.M41) < 0.1 && Math.Abs(y - modelMatrix.M42) < 0.1 && Math.Abs(z - modelMatrix.M43) < 0.1);
        }

        public Player(GlObject glPlayer, float x, float y, float z, Matrix4X4<float> modelMatrix)
        {
            this.glPlayer = glPlayer;
            this.x = x;
            this.y = y;
            this.z = z;
            this.modelMatrix = modelMatrix;
        }   
    }
}
