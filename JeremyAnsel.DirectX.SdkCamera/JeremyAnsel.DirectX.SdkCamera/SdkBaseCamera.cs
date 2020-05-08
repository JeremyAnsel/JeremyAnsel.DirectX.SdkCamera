using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.Window;
using System;
using System.Diagnostics.CodeAnalysis;

namespace JeremyAnsel.DirectX.SdkCamera
{
    /// <summary>
    /// Simple base camera class that moves and rotates.
    /// The base class records mouse and keyboard input for use by a derived class, and keeps common state.
    /// </summary>
    public abstract class SdkBaseCamera
    {
        protected bool m_isActive;

        // View matrix
        protected XMMatrix m_mView;

        // Projection matrix
        protected XMMatrix m_mProj;

        // Number of camera keys that are down
        protected int m_cKeysDown;

        // State of input - KEY_WAS_DOWN_MASK|KEY_IS_DOWN_MASK
        protected readonly SdkCameraKeyStates[] m_aKeys = new SdkCameraKeyStates[(int)SdkCameraKey.MaxKeys];

        // Direction vector of keyboard input
        protected XMFloat3 m_vKeyboardDirection;

        // Last absolute position of mouse cursor
        protected XMInt2 m_ptLastMousePosition;

        // mask of which buttons are down
        protected MouseKeys m_nCurrentButtonMask;

        // Amount of middle wheel scroll (+/-)
        protected int m_nMouseWheelDelta;

        // Mouse relative delta smoothed over a few frames
        protected XMFloat2 m_vMouseDelta;

        // Number of frames to smooth mouse data over
        protected float m_fFramesToSmoothMouseData;

        // Default camera eye position
        protected XMFloat3 m_vDefaultEye;

        // Default LookAt position
        protected XMFloat3 m_vDefaultLookAt;

        // Camera eye position
        protected XMFloat3 m_vEye;

        // LookAt position
        protected XMFloat3 m_vLookAt;

        // Yaw angle of camera
        protected float m_fCameraYawAngle;

        // Pitch angle of camera
        protected float m_fCameraPitchAngle;

        // Rectangle within which a drag can be initiated.
        protected XMInt4 m_rcDrag;

        // Velocity of camera
        protected XMFloat3 m_vVelocity;

        // Velocity drag force
        protected XMFloat3 m_vVelocityDrag;

        // Countdown timer to apply drag
        protected double m_fDragTimer;

        // Time it takes for velocity to go from full to 0
        protected double m_fTotalDragTimeToZero;

        // Velocity of camera
        protected XMFloat2 m_vRotVelocity;

        // Field of view
        protected float m_fFOV;

        // Aspect ratio
        protected float m_fAspect;

        // Near plane
        protected float m_fNearPlane;

        // Far plane
        protected float m_fFarPlane;

        // Scaler for rotation
        protected float m_fRotationScaler;

        // Scaler for movement
        protected float m_fMoveScaler;

        // True if left button is down
        protected bool m_bMouseLButtonDown;

        // True if middle button is down
        protected bool m_bMouseMButtonDown;

        // True if right button is down
        protected bool m_bMouseRButtonDown;

        // If true, then camera movement will slow to a stop otherwise movement is instant
        protected bool m_bMovementDrag;

        // Invert the pitch axis
        protected bool m_bInvertPitch;

        // If true, then the user can translate the camera/model
        protected bool m_bEnablePositionMovement;

        // If true, then camera can move in the y-axis
        protected bool m_bEnableYAxisMovement;

        // If true, then the camera will be clipped to the boundary
        protected bool m_bClipToBoundary;

        // If true, the class will reset the cursor position so that the cursor always has space to move
        protected bool m_bResetCursorAfterMove;

        // Min point in clip boundary
        protected XMFloat3 m_vMinBoundary;

        // Max point in clip boundary
        protected XMFloat3 m_vMaxBoundary;

        [SuppressMessage("Usage", "CA2214:N'appelez pas de méthodes substituables dans les constructeurs", Justification = "Reviwed.")]
        public SdkBaseCamera()
        {
            m_isActive = true;

            m_fFramesToSmoothMouseData = 2.0f;
            m_fTotalDragTimeToZero = 0.25f;
            m_fNearPlane = 0.0f;
            m_fFarPlane = 1.0f;
            m_fRotationScaler = 0.01f;
            m_fMoveScaler = 5.0f;
            m_bEnablePositionMovement = true;
            m_bEnableYAxisMovement = true;
            m_vMinBoundary = new XMFloat3(-1.0f, -1.0f, -1.0f);
            m_vMaxBoundary = new XMFloat3(1.0f, 1.0f, 1.0f);

            SetViewParams(XMVector.Zero, XMVector.FromFloat(0.0f, 0.0f, 1.0f, 0.0f));
            SetProjParams(XMMath.PIDivFour, 1.0f, 1.0f, 1000.0f);

            NativeMethods.GetCursorPos(out m_ptLastMousePosition);

            m_rcDrag = new XMInt4(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
        }

        public virtual void HandleMessages(IntPtr hWnd, WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            // Current mouse position
            int iMouseX = (short)((ulong)lParam & 0xffffU);
            int iMouseY = (short)((ulong)lParam >> 16);

            switch (msg)
            {
                case WindowMessageType.ActivateApplication:
                    {
                        m_isActive = wParam.ToInt32() == 1;
                        break;
                    }

                case WindowMessageType.KeyDown:
                    {
                        // Map this key to a D3DUtil_CameraKeys enum and update the
                        // state of m_aKeys[] by adding the KEY_WAS_DOWN_MASK|KEY_IS_DOWN_MASK mask
                        // only if the key is not down
                        SdkCameraKey mappedKey = MapKey((VirtualKey)wParam);

                        if (mappedKey != SdkCameraKey.Unknown)
                        {
                            if (!IsKeyDown(m_aKeys[(int)mappedKey]))
                            {
                                m_aKeys[(int)mappedKey] = SdkCameraKeyStates.WasDownMask | SdkCameraKeyStates.IsDownMask;
                                m_cKeysDown++;
                            }
                        }

                        break;
                    }

                case WindowMessageType.KeyUp:
                    {
                        // Map this key to a D3DUtil_CameraKeys enum and update the
                        // state of m_aKeys[] by removing the KEY_IS_DOWN_MASK mask.
                        SdkCameraKey mappedKey = MapKey((VirtualKey)wParam);

                        if (mappedKey != SdkCameraKey.Unknown)
                        {
                            m_aKeys[(int)mappedKey] &= ~SdkCameraKeyStates.IsDownMask;
                            m_cKeysDown--;
                        }

                        break;
                    }

                case WindowMessageType.RightButtonDown:
                case WindowMessageType.MiddleButtonDown:
                case WindowMessageType.LeftButtonDown:
                case WindowMessageType.RightButtonDoubleClick:
                case WindowMessageType.MiddleButtonDoubleClick:
                case WindowMessageType.LeftButtonDoubleClick:
                    {
                        // Compute the drag rectangle in screen coord.
                        XMInt2 ptCursor = new XMInt2(iMouseX, iMouseY);

                        // Update member var state
                        if ((msg == WindowMessageType.LeftButtonDown || msg == WindowMessageType.LeftButtonDoubleClick) && NativeMethods.PtInRect(ref m_rcDrag, ptCursor))
                        {
                            m_bMouseLButtonDown = true;
                            m_nCurrentButtonMask |= MouseKeys.LeftButton;
                        }

                        if ((msg == WindowMessageType.MiddleButtonDown || msg == WindowMessageType.MiddleButtonDoubleClick) && NativeMethods.PtInRect(ref m_rcDrag, ptCursor))
                        {
                            m_bMouseMButtonDown = true;
                            m_nCurrentButtonMask |= MouseKeys.MiddleButton;
                        }

                        if ((msg == WindowMessageType.RightButtonDown || msg == WindowMessageType.RightButtonDoubleClick) && NativeMethods.PtInRect(ref m_rcDrag, ptCursor))
                        {
                            m_bMouseRButtonDown = true;
                            m_nCurrentButtonMask |= MouseKeys.RightButton;
                        }

                        // Capture the mouse, so if the mouse button is 
                        // released outside the window, we'll get the WM_LBUTTONUP message
                        NativeMethods.SetCapture(hWnd);
                        NativeMethods.GetCursorPos(out m_ptLastMousePosition);
                        break;
                    }

                case WindowMessageType.RightButtonUp:
                case WindowMessageType.MiddleButtonUp:
                case WindowMessageType.LeftButtonUp:
                    {
                        // Update member var state
                        if (msg == WindowMessageType.LeftButtonUp)
                        {
                            m_bMouseLButtonDown = false;
                            m_nCurrentButtonMask &= ~MouseKeys.LeftButton;
                        }

                        if (msg == WindowMessageType.MiddleButtonUp)
                        {
                            m_bMouseMButtonDown = false;
                            m_nCurrentButtonMask &= ~MouseKeys.MiddleButton;
                        }

                        if (msg == WindowMessageType.RightButtonUp)
                        {
                            m_bMouseRButtonDown = false;
                            m_nCurrentButtonMask &= ~MouseKeys.RightButton;
                        }

                        // Release the capture if no mouse buttons down
                        if (!m_bMouseLButtonDown &&
                            !m_bMouseRButtonDown &&
                            !m_bMouseMButtonDown)
                        {
                            NativeMethods.ReleaseCapture();
                        }

                        break;
                    }

                case WindowMessageType.CaptureChanged:
                    {
                        if (lParam != hWnd)
                        {
                            if ((m_nCurrentButtonMask & MouseKeys.LeftButton) != 0 ||
                                (m_nCurrentButtonMask & MouseKeys.MiddleButton) != 0 ||
                                (m_nCurrentButtonMask & MouseKeys.RightButton) != 0)
                            {
                                m_bMouseLButtonDown = false;
                                m_bMouseMButtonDown = false;
                                m_bMouseRButtonDown = false;
                                m_nCurrentButtonMask &= ~MouseKeys.LeftButton;
                                m_nCurrentButtonMask &= ~MouseKeys.MiddleButton;
                                m_nCurrentButtonMask &= ~MouseKeys.RightButton;
                                NativeMethods.ReleaseCapture();
                            }
                        }

                        break;
                    }

                case WindowMessageType.MouseWheel:
                    {
                        // Update member var state
                        m_nMouseWheelDelta += (short)((uint)wParam >> 16);
                        break;
                    }
            }
        }

        public abstract void FrameMove(double fElapsedTime);

        public virtual void Reset()
        {
            XMVector vDefaultEye = m_vDefaultEye;
            XMVector vDefaultLookAt = m_vDefaultLookAt;

            SetViewParams(vDefaultEye, vDefaultLookAt);
        }

        public virtual void SetViewParams(XMVector vEyePt, XMVector vLookatPt)
        {
            m_vEye = vEyePt;
            m_vDefaultEye = vEyePt;

            m_vLookAt = vLookatPt;
            m_vDefaultLookAt = vLookatPt;

            // Calc the view matrix
            XMMatrix mView = XMMatrix.LookAtLH(vEyePt, vLookatPt, XMVector.FromFloat(0.0f, 1.0f, 0.0f, 0.0f));
            m_mView = mView;

            XMMatrix mInvView = mView.Inverse();

            // The axis basis vectors and camera position are stored inside the 
            // position matrix in the 4 rows of the camera's world matrix.
            // To figure out the yaw/pitch of the camera, we just need the Z basis vector
            XMFloat3 zBasis = new XMFloat3(mInvView.M31, mInvView.M32, mInvView.M33);

            m_fCameraYawAngle = (float)Math.Atan2(zBasis.X, zBasis.Z);
            float fLen = (float)Math.Sqrt(zBasis.Z * zBasis.Z + zBasis.X * zBasis.X);
            m_fCameraPitchAngle = -(float)Math.Atan2(zBasis.Y, fLen);
        }

        public virtual void SetProjParams(float fFOV, float fAspect, float fNearPlane, float fFarPlane)
        {
            // Set attributes for the projection matrix
            m_fFOV = fFOV;
            m_fAspect = fAspect;
            m_fNearPlane = fNearPlane;
            m_fFarPlane = fFarPlane;

            XMMatrix mProj = XMMatrix.PerspectiveFovLH(fFOV, fAspect, fNearPlane, fFarPlane);
            m_mProj = mProj;
        }

        public virtual void SetDragRect(XMInt4 rc)
        {
            m_rcDrag = rc;
        }

        public void SetInvertPitch(bool bInvertPitch)
        {
            m_bInvertPitch = bInvertPitch;
        }

        public void SetDrag(bool bMovementDrag)
        {
            SetDrag(bMovementDrag, 0.25f);
        }

        public void SetDrag(bool bMovementDrag, float fTotalDragTimeToZero)
        {
            m_bMovementDrag = bMovementDrag;
            m_fTotalDragTimeToZero = fTotalDragTimeToZero;
        }

        public void SetEnableYAxisMovement(bool bEnableYAxisMovement)
        {
            m_bEnableYAxisMovement = bEnableYAxisMovement;
        }

        public void SetEnablePositionMovement(bool bEnablePositionMovement)
        {
            m_bEnablePositionMovement = bEnablePositionMovement;
        }

        public void SetClipToBoundary(bool bClipToBoundary)
        {
            SetClipToBoundary(bClipToBoundary, null, null);
        }

        public void SetClipToBoundary(bool bClipToBoundary, XMFloat3? pvMinBoundary, XMFloat3? pvMaxBoundary)
        {
            m_bClipToBoundary = bClipToBoundary;

            if (pvMinBoundary.HasValue)
            {
                m_vMinBoundary = pvMinBoundary.Value;
            }

            if (pvMaxBoundary.HasValue)
            {
                m_vMaxBoundary = pvMaxBoundary.Value;
            }
        }

        public void SetScalers()
        {
            SetScalers(0.01f, 5.0f);
        }

        public void SetScalers(float fRotationScaler, float fMoveScaler)
        {
            m_fRotationScaler = fRotationScaler;
            m_fMoveScaler = fMoveScaler;
        }

        public void SetNumberOfFramesToSmoothMouseData(int nFrames)
        {
            if (nFrames > 0)
            {
                m_fFramesToSmoothMouseData = nFrames;
            }
        }

        public void SetResetCursorAfterMove(bool bResetCursorAfterMove)
        {
            m_bResetCursorAfterMove = bResetCursorAfterMove;
        }

        public XMMatrix GetViewMatrix()
        {
            return m_mView;
        }

        public XMMatrix GetProjMatrix()
        {
            return m_mProj;
        }

        public XMVector GetEyePt()
        {
            return m_vEye;
        }

        public XMVector GetLookAtPt()
        {
            return m_vLookAt;
        }

        public float GetNearClip()
        {
            return m_fNearPlane;
        }

        public float GetFarClip()
        {
            return m_fFarPlane;
        }

        public bool IsBeingDragged()
        {
            return m_bMouseLButtonDown || m_bMouseMButtonDown || m_bMouseRButtonDown;
        }

        public bool IsMouseLButtonDown()
        {
            return m_bMouseLButtonDown;
        }

        public bool IsMouseMButtonDown()
        {
            return m_bMouseMButtonDown;
        }

        public bool IsMouseRButtonDown()
        {
            return m_bMouseRButtonDown;
        }

        protected virtual SdkCameraKey MapKey(VirtualKey nKey)
        {
            switch (nKey)
            {
                case VirtualKey.Control:
                    return SdkCameraKey.ControlDown;
                case VirtualKey.Left:
                    return SdkCameraKey.StrafeLeft;
                case VirtualKey.Right:
                    return SdkCameraKey.StrafeRight;
                case VirtualKey.Up:
                    return SdkCameraKey.MoveForward;
                case VirtualKey.Down:
                    return SdkCameraKey.MoveBackward;
                case VirtualKey.Prior:
                    return SdkCameraKey.MoveUp;
                case VirtualKey.Next:
                    return SdkCameraKey.MoveDown;

                case VirtualKey.A:
                    return SdkCameraKey.StrafeLeft;
                case VirtualKey.D:
                    return SdkCameraKey.StrafeRight;
                case VirtualKey.W:
                    return SdkCameraKey.MoveForward;
                case VirtualKey.S:
                    return SdkCameraKey.MoveBackward;
                case VirtualKey.Q:
                    return SdkCameraKey.MoveDown;
                case VirtualKey.E:
                    return SdkCameraKey.MoveUp;

                case VirtualKey.NumPad4:
                    return SdkCameraKey.StrafeLeft;
                case VirtualKey.NumPad6:
                    return SdkCameraKey.StrafeRight;
                case VirtualKey.NumPad8:
                    return SdkCameraKey.MoveForward;
                case VirtualKey.NumPad2:
                    return SdkCameraKey.MoveBackward;
                case VirtualKey.NumPad9:
                    return SdkCameraKey.MoveUp;
                case VirtualKey.NumPad3:
                    return SdkCameraKey.MoveDown;

                case VirtualKey.Home:
                    return SdkCameraKey.Reset;
            }

            return SdkCameraKey.Unknown;
        }

        protected static bool IsKeyDown(SdkCameraKeyStates key)
        {
            return (key & SdkCameraKeyStates.IsDownMask) != 0;
        }

        protected static bool WasKeyDown(SdkCameraKeyStates key)
        {
            return (key & SdkCameraKeyStates.WasDownMask) != 0;
        }

        protected XMVector ConstrainToBoundary(XMVector v)
        {
            XMVector vMin = m_vMinBoundary;
            XMVector vMax = m_vMaxBoundary;

            // Constrain vector to a bounding box 
            return v.Clamp(vMin, vMax);
        }

        protected void UpdateMouseDelta()
        {
            // Get current position of mouse
            NativeMethods.GetCursorPos(out XMInt2 ptCurMousePos);

            // Calc how far it's moved since last frame
            XMInt2 ptCurMouseDelta = new XMInt2(
                ptCurMousePos.X - m_ptLastMousePosition.X,
                ptCurMousePos.Y - m_ptLastMousePosition.Y);

            // Record current position for next time
            m_ptLastMousePosition = ptCurMousePos;

            if (m_bResetCursorAfterMove && m_isActive)
            {
                // Get the center of the current monitor
                NativeMethods.GetClipCursor(out XMInt4 lpRect);

                // Set position of camera to center of desktop, 
                // so it always has room to move.  This is very useful
                // if the cursor is hidden.  If this isn't done and cursor is hidden, 
                // then invisible cursor will hit the edge of the screen 
                // and the user can't tell what happened
                XMInt2 ptCenter = new XMInt2(
                    (lpRect.X + lpRect.Z) / 2,
                    (lpRect.Y + lpRect.W) / 2);

                NativeMethods.SetCursorPos(ptCenter.X, ptCenter.Y);
                m_ptLastMousePosition = ptCenter;
            }

            // Smooth the relative mouse data over a few frames so it isn't 
            // jerky when moving slowly at low frame rates.
            float fPercentOfNew = 1.0f / m_fFramesToSmoothMouseData;
            float fPercentOfOld = 1.0f - fPercentOfNew;
            m_vMouseDelta.X = m_vMouseDelta.X * fPercentOfOld + ptCurMouseDelta.X * fPercentOfNew;
            m_vMouseDelta.Y = m_vMouseDelta.Y * fPercentOfOld + ptCurMouseDelta.Y * fPercentOfNew;

            m_vRotVelocity.X = m_vMouseDelta.X * m_fRotationScaler;
            m_vRotVelocity.Y = m_vMouseDelta.Y * m_fRotationScaler;
        }

        protected void UpdateVelocity(double fElapsedTime)
        {
            XMVector vMouseDelta = m_vMouseDelta;
            XMVector vRotVelocity = vMouseDelta * m_fRotationScaler;

            m_vRotVelocity = vRotVelocity;

            XMVector vKeyboardDirection = m_vKeyboardDirection;
            XMVector vAccel = vKeyboardDirection;

            // Normalize vector so if moving 2 dirs (left & forward), 
            // the camera doesn't move faster than if moving in 1 dir
            vAccel = XMVector3.Normalize(vAccel);

            // Scale the acceleration vector
            vAccel *= m_fMoveScaler;

            if (m_bMovementDrag)
            {
                // Is there any acceleration this frame?
                if (XMVector3.LengthSquare(vAccel).X > 0)
                {
                    // If so, then this means the user has pressed a movement key
                    // so change the velocity immediately to acceleration 
                    // upon keyboard input.  This isn't normal physics
                    // but it will give a quick response to keyboard input
                    m_vVelocity = vAccel;

                    m_fDragTimer = m_fTotalDragTimeToZero;

                    m_vVelocityDrag = vAccel / (float)m_fDragTimer;
                }
                else
                {
                    // If no key being pressed, then slowly decrease velocity to 0
                    if (m_fDragTimer > 0)
                    {
                        // Drag until timer is <= 0
                        XMVector vVelocity = m_vVelocity;
                        XMVector vVelocityDrag = m_vVelocityDrag;

                        vVelocity -= vVelocityDrag * (float)fElapsedTime;

                        m_vVelocity = vVelocity;

                        m_fDragTimer -= fElapsedTime;
                    }
                    else
                    {
                        // Zero velocity
                        m_vVelocity = XMVector.Zero;
                    }
                }
            }
            else
            {
                // No drag, so immediately change the velocity
                m_vVelocity = vAccel;
            }
        }

        protected void GetInput(bool bGetKeyboardInput, bool bGetMouseInput)
        {
            m_vKeyboardDirection = XMVector.Zero;

            if (bGetKeyboardInput)
            {
                // Update acceleration vector based on keyboard state
                if (IsKeyDown(m_aKeys[(int)SdkCameraKey.MoveForward]))
                {
                    m_vKeyboardDirection.Z += 1.0f;
                }

                if (IsKeyDown(m_aKeys[(int)SdkCameraKey.MoveBackward]))
                {
                    m_vKeyboardDirection.Z -= 1.0f;
                }

                if (m_bEnableYAxisMovement)
                {
                    if (IsKeyDown(m_aKeys[(int)SdkCameraKey.MoveUp]))
                    {
                        m_vKeyboardDirection.Y += 1.0f;
                    }

                    if (IsKeyDown(m_aKeys[(int)SdkCameraKey.MoveDown]))
                    {
                        m_vKeyboardDirection.Y -= 1.0f;
                    }
                }

                if (IsKeyDown(m_aKeys[(int)SdkCameraKey.StrafeRight]))
                {
                    m_vKeyboardDirection.X += 1.0f;
                }

                if (IsKeyDown(m_aKeys[(int)SdkCameraKey.StrafeLeft]))
                {
                    m_vKeyboardDirection.X -= 1.0f;
                }
            }

            if (bGetMouseInput)
            {
                UpdateMouseDelta();
            }
        }
    }
}
