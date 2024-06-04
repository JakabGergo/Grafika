using Silk.NET.Maths;

namespace Projekt
{
    internal class CameraDescriptor
    {
        private Vector3D<float> lookDirection = new Vector3D<float>(0, 0, -1);
        private Vector3D<float> position = new Vector3D<float>(0,2,7);
        private Vector3D<float> positionKoveto = new Vector3D<float>(0,3,22);
        private Vector3D<float> felsoNezet = new Vector3D<float>(0f, 74f, 17f);
        private Vector3D<float> target = new Vector3D<float>(0,0,0);
        private Vector3D<float> upVector = new Vector3D<float>(0,1,0);

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

        public Vector3D<float> PositionKoveto
        {
            get
            {
                return positionKoveto;
            }
            set
            {
                positionKoveto = value;
            }
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }

        public Vector3D <float> FelsoNezet
        {
            get
            {
                return felsoNezet;
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

        public void updatePositionKoveto(Vector3D<float> position)
        {
            positionKoveto += position;
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
