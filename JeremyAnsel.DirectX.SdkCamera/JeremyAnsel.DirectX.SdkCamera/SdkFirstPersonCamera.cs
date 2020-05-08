using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.Window;
using System;

namespace JeremyAnsel.DirectX.SdkCamera
{
    /// <summary>
    /// Simple first person camera class that moves and rotates.
    /// It allows yaw and pitch but not roll.  It uses WM_KEYDOWN and GetCursorPos() to respond to keyboard and mouse input and updates the view matrix based on input.
    /// </summary>
    public class SdkFirstPersonCamera : SdkBaseCamera
    {
        // World matrix of the camera (inverse of the view matrix)
        protected XMMatrix m_mCameraWorld;

        // Mask to determine which button to enable for rotation
        protected MouseKeys m_nActiveButtonMask;

        protected bool m_bRotateWithoutButtonDown;

        public SdkFirstPersonCamera()
        {
            m_nActiveButtonMask = MouseKeys.LeftButton | MouseKeys.RightButton | MouseKeys.Shift;
        }

        public override void FrameMove(double fElapsedTime)
        {
            if (IsKeyDown(m_aKeys[(int)SdkCameraKey.Reset]))
            {
                Reset();
            }

            // Get keyboard/mouse/gamepad input
            GetInput(m_bEnablePositionMovement, (m_nActiveButtonMask & m_nCurrentButtonMask) != 0 || m_bRotateWithoutButtonDown);

            //// Get the mouse movement (if any) if the mouse button are down
            //if( (m_nActiveButtonMask & m_nCurrentButtonMask) || m_bRotateWithoutButtonDown )
            //    UpdateMouseDelta( fElapsedTime );

            // Get amount of velocity based on the keyboard input and drag (if any)
            UpdateVelocity(fElapsedTime);

            // Simple euler method to calculate position delta
            XMVector vVelocity = m_vVelocity;
            XMVector vPosDelta = vVelocity * (float)fElapsedTime;

            // If rotating the camera 
            if ((m_nActiveButtonMask & m_nCurrentButtonMask) != 0 || m_bRotateWithoutButtonDown)
            {
                // Update the pitch & yaw angle based on mouse movement
                float fYawDelta = m_vRotVelocity.X;
                float fPitchDelta = m_vRotVelocity.Y;

                // Invert pitch if requested
                if (m_bInvertPitch)
                {
                    fPitchDelta = -fPitchDelta;
                }

                m_fCameraPitchAngle += fPitchDelta;
                m_fCameraYawAngle += fYawDelta;

                // Limit pitch to straight up or straight down
                m_fCameraPitchAngle = Math.Max(-XMMath.PIDivTwo, m_fCameraPitchAngle);
                m_fCameraPitchAngle = Math.Min(+XMMath.PIDivTwo, m_fCameraPitchAngle);
            }

            // Make a rotation matrix based on the camera's yaw & pitch
            XMMatrix mCameraRot = XMMatrix.RotationRollPitchYaw(m_fCameraPitchAngle, m_fCameraYawAngle, 0);

            // Transform vectors based on camera's rotation matrix
            XMVector vWorldUp = XMVector3.TransformCoord(XMVector.FromFloat(0.0f, 1.0f, 0.0f, 0.0f), mCameraRot);
            XMVector vWorldAhead = XMVector3.TransformCoord(XMVector.FromFloat(0.0f, 0.0f, 1.0f, 0.0f), mCameraRot);

            // Transform the position delta by the camera's rotation 
            if (!m_bEnableYAxisMovement)
            {
                // If restricting Y movement, do not include pitch
                // when transforming position delta vector.
                mCameraRot = XMMatrix.RotationRollPitchYaw(0.0f, m_fCameraYawAngle, 0.0f);
            }

            XMVector vPosDeltaWorld = XMVector3.TransformCoord(vPosDelta, mCameraRot);

            // Move the eye position 
            XMVector vEye = m_vEye;
            vEye += vPosDeltaWorld;

            if (m_bClipToBoundary)
            {
                vEye = ConstrainToBoundary(vEye);
            }

            m_vEye = vEye;

            // Update the lookAt position based on the eye position
            XMVector vLookAt = vEye + vWorldAhead;
            m_vLookAt = vLookAt;

            // Update the view matrix
            XMMatrix mView = XMMatrix.LookAtLH(vEye, vLookAt, vWorldUp);
            m_mView = mView;

            XMMatrix mCameraWorld = mView.Inverse();
            m_mCameraWorld = mCameraWorld;
        }

        public void SetRotateButtons(bool bLeft, bool bMiddle, bool bRight)
        {
            SetRotateButtons(bLeft, bMiddle, bRight, false);
        }

        public void SetRotateButtons(bool bLeft, bool bMiddle, bool bRight, bool bRotateWithoutButtonDown)
        {
            m_nActiveButtonMask = (bLeft ? MouseKeys.LeftButton : 0) |
                (bMiddle ? MouseKeys.MiddleButton : 0) |
                (bRight ? MouseKeys.RightButton : 0);

            m_bRotateWithoutButtonDown = bRotateWithoutButtonDown;
        }

        public XMMatrix GetWorldMatrix()
        {
            return m_mCameraWorld;
        }

        public XMVector GetWorldRight()
        {
            return XMVector.FromFloat(m_mCameraWorld.M11, m_mCameraWorld.M12, m_mCameraWorld.M13, 0.0f);
        }

        public XMVector GetWorldUp()
        {
            return XMVector.FromFloat(m_mCameraWorld.M21, m_mCameraWorld.M22, m_mCameraWorld.M23, 0.0f);
        }

        public XMVector GetWorldAheads()
        {
            return XMVector.FromFloat(m_mCameraWorld.M31, m_mCameraWorld.M32, m_mCameraWorld.M33, 0.0f);
        }

        public new XMVector GetEyePt()
        {
            return XMVector.FromFloat(m_mCameraWorld.M41, m_mCameraWorld.M42, m_mCameraWorld.M43, 0.0f);
        }
    }
}
