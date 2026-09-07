using UnityEngine;

namespace Mox
{
    namespace NDisplay
    {
        public class CameraUtility
        {
            public struct Frustum
            {
                public float left;
                public float right;
                public float bottom;
                public float top;
                public float clipNear;
                public float clipFar;

                public Vector3 side;
                public Vector3 up;
                public Vector3 fwd;
            };

            // Source: ds unity starter kit
            public static Frustum CalculateCameraFrustum(Camera camera, Transform targetPlane)
            {
                Frustum result = new Frustum();

                if (camera != null
                    && targetPlane != null)
                {
                    result.side = camera.transform.localToWorldMatrix.GetColumn(0).normalized;
                    result.up = camera.transform.localToWorldMatrix.GetColumn(1).normalized;
                    result.fwd = camera.transform.localToWorldMatrix.GetColumn(2).normalized;

                    Vector3 planeDistance = camera.transform.position - targetPlane.position;
                    Vector2 planeSize = new Vector2(targetPlane.lossyScale.x, targetPlane.lossyScale.z) * 10.0f; // because the standard plane is 10 x 10, so scale is 1/10 of the actual size

                    float distSide = Vector3.Dot(planeDistance, result.side);
                    float distUp = Vector3.Dot(planeDistance, result.up);
                    float distFwd = Vector3.Dot(planeDistance, result.fwd);

                    result.top = -(planeSize.y * 0.5f - distUp) / distFwd * camera.nearClipPlane;
                    result.bottom = (planeSize.y * 0.5f + distUp) / distFwd * camera.nearClipPlane;
                    result.left = (planeSize.x * 0.5f + distSide) / distFwd * camera.nearClipPlane;
                    result.right = -(planeSize.x * 0.5f - distSide) / distFwd * camera.nearClipPlane;
                    result.clipNear = camera.nearClipPlane;
                    result.clipFar = camera.farClipPlane;
                }

                return result;
            }

            public static float CalculateFOV(Camera camera, Transform targetPlane)
            {
                float result = 0.0f;

                if (camera != null
                    && targetPlane != null)
                {
                    float dist = (camera.transform.localPosition - targetPlane.localPosition).magnitude;

                    Vector2 planeSize = new Vector2(targetPlane.lossyScale.x, targetPlane.lossyScale.z) * 10.0f; // because the standard plane is 10 x 10, so scale is 1/10 of the actual size

                    float h = planeSize.y / 2.0f;

                    // float c = Mathf.Sqrt((dist * dist) + (h * h));

                    result = (Mathf.Atan(h / dist) * 180.0f / 3.14159f) * 2.0f;
                }

                return result;
            }

            // Source: ds unity starter kit
            public static Matrix4x4 CreateProjectionMatrix(Frustum frustum)
            {
                Matrix4x4 result = new Matrix4x4();

                result.SetRow(0, new Vector4(2.0f * frustum.clipNear / (frustum.right - frustum.left), 0f, (frustum.right + frustum.left) / (frustum.right - frustum.left), 0f));
                result.SetRow(1, new Vector4(0f, 2.0f * frustum.clipNear / (frustum.top - frustum.bottom), (frustum.top + frustum.bottom) / (frustum.top - frustum.bottom), 0f));
                result.SetRow(2, new Vector4(0f, 0f, (frustum.clipFar + frustum.clipNear) / (frustum.clipNear - frustum.clipFar), 2.0f * (frustum.clipFar * frustum.clipNear) / (frustum.clipNear - frustum.clipFar)));
                result.SetRow(3, new Vector4(0f, 0f, -1.0f, 0f));

                return result;
            }
        }
    }
}

