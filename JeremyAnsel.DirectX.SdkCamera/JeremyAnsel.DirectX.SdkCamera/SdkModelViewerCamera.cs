using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.Window;
using System;

namespace JeremyAnsel.DirectX.SdkCamera
{
    /// <summary>
    /// Simple model viewing camera class that rotates around the object.
    /// </summary>
    public class SdkModelViewerCamera : SdkBaseCamera
    {
        protected readonly SdkArcBall m_WorldArcBall = new SdkArcBall();

        protected readonly SdkArcBall m_ViewArcBall = new SdkArcBall();

        protected XMFloat3 m_vModelCenter;

        // Last arcball rotation matrix for model
        protected XMMatrix m_mModelLastRot;

        // Rotation matrix of model
        protected XMMatrix m_mModelRot;

        // World matrix of model
        protected XMMatrix m_mWorld;

        protected SdkCameraMouseKeys m_nRotateModelButtonMask;

        protected SdkCameraMouseKeys m_nZoomButtonMask;

        protected SdkCameraMouseKeys m_nRotateCameraButtonMask;

        protected bool m_bAttachCameraToModel;

        protected bool m_bLimitPitch;

        // True if mouse drag has happened since last time FrameMove is called.
        protected bool m_bDragSinceLastUpdate;

        // Distance from the camera to model
        protected float m_fRadius;

        // Distance from the camera to model
        protected float m_fDefaultRadius;

        // Min radius
        protected float m_fMinRadius;

        // Max radius
        protected float m_fMaxRadius;

        protected XMMatrix m_mCameraRotLast;

        public SdkModelViewerCamera()
        {
            m_nRotateModelButtonMask = SdkCameraMouseKeys.LeftButton;
            m_nZoomButtonMask = SdkCameraMouseKeys.Wheel;
            m_nRotateCameraButtonMask = SdkCameraMouseKeys.RightButton;
            m_bDragSinceLastUpdate = true;
            m_fRadius = 5.0f;
            m_fDefaultRadius = 5.0f;
            m_fMinRadius = 1.0f;
            m_fMaxRadius = float.MaxValue;

            XMMatrix id = XMMatrix.Identity;

            m_mWorld = id;
            m_mModelRot = id;
            m_mModelLastRot = id;
            m_mCameraRotLast = id;
            m_vModelCenter = XMVector.Zero;

            m_bEnablePositionMovement = false;
        }

        public override void HandleMessages(IntPtr hWnd, WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            base.HandleMessages(hWnd, msg, wParam, lParam);

            // Current mouse position
            int iMouseX = (short)((ulong)lParam & 0xffffU);
            int iMouseY = (short)((ulong)lParam >> 16);

            if (((msg == WindowMessageType.LeftButtonDown || msg == WindowMessageType.LeftButtonDoubleClick)
                    && (m_nRotateModelButtonMask & SdkCameraMouseKeys.LeftButton) != 0) ||
                ((msg == WindowMessageType.MiddleButtonDown || msg == WindowMessageType.MiddleButtonDoubleClick)
                    && (m_nRotateModelButtonMask & SdkCameraMouseKeys.MiddleButton) != 0) ||
                ((msg == WindowMessageType.RightButtonDown || msg == WindowMessageType.RightButtonDoubleClick)
                    && (m_nRotateModelButtonMask & SdkCameraMouseKeys.RightButton) != 0))
            {
                m_WorldArcBall.OnBegin(iMouseX, iMouseY);
            }

            if (((msg == WindowMessageType.LeftButtonDown || msg == WindowMessageType.LeftButtonDoubleClick)
                    && (m_nRotateCameraButtonMask & SdkCameraMouseKeys.LeftButton) != 0) ||
                ((msg == WindowMessageType.MiddleButtonDown || msg == WindowMessageType.MiddleButtonDoubleClick)
                    && (m_nRotateCameraButtonMask & SdkCameraMouseKeys.MiddleButton) != 0) ||
                ((msg == WindowMessageType.RightButtonDown || msg == WindowMessageType.RightButtonDoubleClick)
                    && (m_nRotateCameraButtonMask & SdkCameraMouseKeys.RightButton) != 0))
            {
                m_ViewArcBall.OnBegin(iMouseX, iMouseY);
            }

            if (msg == WindowMessageType.MouseMove)
            {
                m_WorldArcBall.OnMove(iMouseX, iMouseY);
                m_ViewArcBall.OnMove(iMouseX, iMouseY);
            }

            if ((msg == WindowMessageType.LeftButtonUp && (m_nRotateModelButtonMask & SdkCameraMouseKeys.LeftButton) != 0) ||
                (msg == WindowMessageType.MiddleButtonUp && (m_nRotateModelButtonMask & SdkCameraMouseKeys.MiddleButton) != 0) ||
                (msg == WindowMessageType.RightButtonUp && (m_nRotateModelButtonMask & SdkCameraMouseKeys.RightButton) != 0))
            {
                m_WorldArcBall.OnEnd();
            }

            if ((msg == WindowMessageType.LeftButtonUp && (m_nRotateCameraButtonMask & SdkCameraMouseKeys.LeftButton) != 0) ||
                (msg == WindowMessageType.MiddleButtonUp && (m_nRotateCameraButtonMask & SdkCameraMouseKeys.MiddleButton) != 0) ||
                (msg == WindowMessageType.RightButtonUp && (m_nRotateCameraButtonMask & SdkCameraMouseKeys.RightButton) != 0))
            {
                m_ViewArcBall.OnEnd();
            }

            if (msg == WindowMessageType.CaptureChanged)
            {
                if (lParam != hWnd)
                {
                    if (((m_nRotateModelButtonMask & SdkCameraMouseKeys.LeftButton) != 0) ||
                        ((m_nRotateModelButtonMask & SdkCameraMouseKeys.MiddleButton) != 0) ||
                        ((m_nRotateModelButtonMask & SdkCameraMouseKeys.RightButton) != 0))
                    {
                        m_WorldArcBall.OnEnd();
                    }

                    if (((m_nRotateCameraButtonMask & SdkCameraMouseKeys.LeftButton) != 0) ||
                        ((m_nRotateCameraButtonMask & SdkCameraMouseKeys.MiddleButton) != 0) ||
                        ((m_nRotateCameraButtonMask & SdkCameraMouseKeys.RightButton) != 0))
                    {
                        m_ViewArcBall.OnEnd();
                    }
                }
            }

            if (msg == WindowMessageType.LeftButtonDown ||
                msg == WindowMessageType.LeftButtonDoubleClick ||
                msg == WindowMessageType.MiddleButtonDown ||
                msg == WindowMessageType.MiddleButtonDoubleClick ||
                msg == WindowMessageType.RightButtonDown ||
                msg == WindowMessageType.RightButtonDoubleClick ||
                msg == WindowMessageType.LeftButtonUp ||
                msg == WindowMessageType.MiddleButtonUp ||
                msg == WindowMessageType.RightButtonUp ||
                msg == WindowMessageType.MouseWheel ||
                msg == WindowMessageType.MouseMove)
            {
                m_bDragSinceLastUpdate = true;
            }
        }

        public override void FrameMove(double fElapsedTime)
        {
            if (IsKeyDown(m_aKeys[(int)SdkCameraKey.Reset]))
            {
                Reset();
            }

            // If no dragged has happend since last time FrameMove is called,
            // and no camera key is held down, then no need to handle again.
            if (!m_bDragSinceLastUpdate && m_cKeysDown == 0)
            {
                return;
            }

            m_bDragSinceLastUpdate = false;

            //// If no mouse button is held down, 
            //// Get the mouse movement (if any) if the mouse button are down
            //if( m_nCurrentButtonMask != 0 ) 
            //    UpdateMouseDelta( fElapsedTime );

            GetInput(m_bEnablePositionMovement, m_nCurrentButtonMask != 0);

            // Get amount of velocity based on the keyboard input and drag (if any)
            UpdateVelocity(fElapsedTime);

            // Simple euler method to calculate position delta
            XMVector vPosDelta = XMVector.LoadFloat3(m_vVelocity) * (float)fElapsedTime;

            // Change the radius from the camera to the model based on wheel scrolling
            if (m_nMouseWheelDelta != 0 && m_nZoomButtonMask == SdkCameraMouseKeys.Wheel)
            {
                m_fRadius -= m_nMouseWheelDelta * m_fRadius * 0.1f / 120.0f;
            }

            m_fRadius = Math.Min(m_fMaxRadius, m_fRadius);
            m_fRadius = Math.Max(m_fMinRadius, m_fRadius);
            m_nMouseWheelDelta = 0;

            // Get the inverse of the arcball's rotation matrix
            XMMatrix mCameraRot = m_ViewArcBall.GetRotationMatrix().Inverse();

            // Transform vectors based on camera's rotation matrix
            XMVector vWorldUp = XMVector3.TransformCoord(XMVector.FromFloat(0.0f, 1.0f, 0.0f, 0.0f), mCameraRot);
            XMVector vWorldAhead = XMVector3.TransformCoord(XMVector.FromFloat(0.0f, 0.0f, 1.0f, 0.0f), mCameraRot);

            // Transform the position delta by the camera's rotation 
            XMVector vPosDeltaWorld = XMVector3.TransformCoord(vPosDelta, mCameraRot);

            // Move the lookAt position 
            XMVector vLookAt = m_vLookAt;
            vLookAt += vPosDeltaWorld;

            if (m_bClipToBoundary)
            {
                vLookAt = ConstrainToBoundary(vLookAt);
            }

            m_vLookAt = vLookAt;

            // Update the eye point based on a radius away from the lookAt position
            XMVector vEye = vLookAt - vWorldAhead * m_fRadius;
            m_vEye = vEye;

            // Update the view matrix
            XMMatrix mView = XMMatrix.LookAtLH(vEye, vLookAt, vWorldUp);
            m_mView = mView;

            XMMatrix mInvView = mView.Inverse();
            mInvView.M41 = 0.0f;
            mInvView.M42 = 0.0f;
            mInvView.M43 = 0.0f;

            XMMatrix mModelLastRot = m_mModelLastRot;
            XMMatrix mModelLastRotInv = mModelLastRot.Inverse();

            // Accumulate the delta of the arcball's rotation in view space.
            // Note that per-frame delta rotations could be problematic over long periods of time.
            XMMatrix mModelRot0 = m_WorldArcBall.GetRotationMatrix();
            XMMatrix mModelRot = m_mModelRot;
            mModelRot *= mView * mModelLastRotInv * mModelRot0 * mInvView;

            if (m_ViewArcBall.IsBeingDragged() && m_bAttachCameraToModel && !IsKeyDown(m_aKeys[(int)SdkCameraKey.ControlDown]))
            {
                // Attach camera to model by inverse of the model rotation
                XMMatrix mCameraRotLast = m_mCameraRotLast;
                XMMatrix mCameraLastRotInv = mCameraRotLast.Inverse();
                XMMatrix mCameraRotDelta = mCameraLastRotInv * mCameraRot; // local to world matrix
                mModelRot *= mCameraRotDelta;
            }

            m_mModelLastRot = mModelRot0;
            m_mCameraRotLast = mCameraRot;

            // Since we're accumulating delta rotations, we need to orthonormalize 
            // the matrix to prevent eventual matrix skew
            XMVector xBasis = XMVector3.Normalize(XMVector.FromFloat(mModelRot.M11, mModelRot.M12, mModelRot.M13, mModelRot.M14));
            XMVector yBasis = XMVector3.Cross(XMVector.FromFloat(mModelRot.M31, mModelRot.M32, mModelRot.M33, mModelRot.M34), xBasis);
            yBasis = XMVector3.Normalize(yBasis);
            XMVector zBasis = XMVector3.Cross(xBasis, yBasis);

            mModelRot.M11 = xBasis.X;
            mModelRot.M12 = xBasis.Y;
            mModelRot.M13 = xBasis.Z;
            mModelRot.M21 = yBasis.X;
            mModelRot.M22 = yBasis.Y;
            mModelRot.M23 = yBasis.Z;
            mModelRot.M31 = zBasis.X;
            mModelRot.M32 = zBasis.Y;
            mModelRot.M33 = zBasis.Z;

            // Translate the rotation matrix to the same position as the lookAt position
            mModelRot.M41 = vLookAt.X;
            mModelRot.M42 = vLookAt.Y;
            mModelRot.M43 = vLookAt.Z;

            m_mModelRot = mModelRot;

            // Translate world matrix so its at the center of the model
            XMMatrix mTrans = XMMatrix.Translation(-m_vModelCenter.X, -m_vModelCenter.Y, -m_vModelCenter.Z);
            XMMatrix mWorld = mTrans * mModelRot;
            m_mWorld = mWorld;
        }

        public override void SetDragRect(XMInt4 rc)
        {
            base.SetDragRect(rc);

            m_WorldArcBall.SetOffset(rc.X, rc.Y);
            m_ViewArcBall.SetOffset(rc.X, rc.Y);

            SetWindow(rc.Z - rc.X, rc.W - rc.Y);
        }

        public override void Reset()
        {
            base.Reset();

            XMMatrix id = XMMatrix.Identity;
            m_mWorld = id;
            m_mModelRot = id;
            m_mModelLastRot = id;
            m_mCameraRotLast = id;

            m_fRadius = m_fDefaultRadius;
            m_WorldArcBall.Reset();
            m_ViewArcBall.Reset();
        }

        public override void SetViewParams(XMVector vEyePt, XMVector vLookatPt)
        {
            base.SetViewParams(vEyePt, vLookatPt);

            // Propogate changes to the member arcball
            XMMatrix mRotation = XMMatrix.LookAtLH(vEyePt, vLookatPt, XMVector.FromFloat(0.0f, 1.0f, 0.0f, 0.0f));
            XMVector quat = XMQuaternion.RotationMatrix(mRotation);
            m_ViewArcBall.SetQuatNow(quat);

            // Set the radius according to the distance
            XMVector vEyeToPoint = XMVector.Subtract(vLookatPt, vEyePt);
            float len = XMVector3.Length(vEyeToPoint).X;
            SetRadius(len);

            // View information changed. FrameMove should be called.
            m_bDragSinceLastUpdate = true;
        }

        public void SetButtonMasks()
        {
            SetButtonMasks(SdkCameraMouseKeys.LeftButton, SdkCameraMouseKeys.Wheel, SdkCameraMouseKeys.RightButton);
        }

        public void SetButtonMasks(SdkCameraMouseKeys nRotateModelButtonMask, SdkCameraMouseKeys nZoomButtonMask, SdkCameraMouseKeys nRotateCameraButtonMask)
        {
            m_nRotateModelButtonMask = nRotateModelButtonMask;
            m_nZoomButtonMask = nZoomButtonMask;
            m_nRotateCameraButtonMask = nRotateCameraButtonMask;
        }

        public void SetAttachCameraToModel()
        {
            SetAttachCameraToModel(false);
        }

        public void SetAttachCameraToModel(bool bEnable)
        {
            m_bAttachCameraToModel = bEnable;
        }

        public void SetWindow(int nWidth, int nHeight)
        {
            SetWindow(nWidth, nHeight, 0.9f);
        }

        public void SetWindow(int nWidth, int nHeight, float fArcballRadius)
        {
            m_WorldArcBall.SetWindow(nWidth, nHeight, fArcballRadius);
            m_ViewArcBall.SetWindow(nWidth, nHeight, fArcballRadius);
        }

        public void SetRadius()
        {
            SetRadius(5.0f, 1.0f, float.MaxValue);
        }

        public void SetRadius(float fDefaultRadius)
        {
            SetRadius(fDefaultRadius, 1.0f, float.MaxValue);
        }

        public void SetRadius(float fDefaultRadius, float fMinRadius, float fMaxRadius)
        {
            m_fDefaultRadius = fDefaultRadius;
            m_fRadius = fDefaultRadius;
            m_fMinRadius = fMinRadius;
            m_fMaxRadius = fMaxRadius;
            m_bDragSinceLastUpdate = true;
        }

        public void SetModelCenter(XMFloat3 vModelCenter)
        {
            m_vModelCenter = vModelCenter;
        }

        public void SetLimitPitch(bool bLimitPitch)
        {
            m_bLimitPitch = bLimitPitch;
        }

        public void SetViewQuat(XMVector q)
        {
            m_ViewArcBall.SetQuatNow(q);
            m_bDragSinceLastUpdate = true;
        }

        public void SetWorldQuat(XMVector q)
        {
            m_WorldArcBall.SetQuatNow(q);
            m_bDragSinceLastUpdate = true;
        }

        public XMMatrix GetWorldMatrix()
        {
            return m_mWorld;
        }

        public void SetWorldMatrix(XMMatrix mWorld)
        {
            m_mWorld = mWorld;
            m_bDragSinceLastUpdate = true;
        }
    }
}
