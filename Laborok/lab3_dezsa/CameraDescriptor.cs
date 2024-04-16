using Silk.NET.Maths;

namespace lab3_dezsa
{
    internal class CameraDescriptor
    {
        private Vector3D<float> lookDirection = new Vector3D<float>(0, 0, -1);
        private Vector3D<float> position = new Vector3D<float>(0, 0, 7);
        private Vector3D<float> target = new Vector3D<float>(0, 0, 0);
        private Vector3D<float> upVector = new Vector3D<float>(0, 1, 0);

        /// <summary>
        /// Gets the position of the camera.
        /// </summary>
        public Vector3D<float> Position
        {
            get
            {
                return position;
            }
        }

        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                return upVector;
            }
        }

        /// <summary>
        /// Gets the target point of the camera view.
        /// </summary>
        public Vector3D<float> Target
        {
            get
            {
                target = position + lookDirection;
                return target;
            }
        }

        public void StrafeLeft(float distance)
        {
            position += Vector3D.Normalize(new Vector3D<float>(-lookDirection.Z, 0, lookDirection.X)) * distance; ;
            target = position + lookDirection;
        }

        public void StrafeRight(float distance)
        {
            position += Vector3D.Normalize(new Vector3D<float>(lookDirection.Z, 0, -lookDirection.X)) * distance;
            target = position + lookDirection;
        }

        public void MoveForward(float distance)
        {
            position += Vector3D.Normalize(lookDirection) * distance;
            target = position + lookDirection;
        }

        public void MoveBackward(float distance)
        {
            position += Vector3D.Normalize(-lookDirection) * distance;
            target = position + lookDirection;
        }

        public void MoveUp(float distance)
        {
            position.Y += distance;
            target = position + lookDirection;
        }

        public void MoveDown(float distance)
        {
            position.Y -= distance;
            target = position + lookDirection;
        }

        public void RotateAroundY(double angle)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);

            double newX = lookDirection.X * cosAngle + lookDirection.Z * sinAngle;
            double newZ = -lookDirection.X * sinAngle + lookDirection.Z * cosAngle;
            lookDirection.X = (float)newX;
            lookDirection.Z = (float)newZ;
            target = position + lookDirection;
        }

        public void RotateAroundX(double angle)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);

            double newY = lookDirection.Y * cosAngle + Math.Abs(lookDirection.Z) * sinAngle;
            lookDirection.Y = (float)newY;
            target = position + lookDirection;
        }
    }
}
