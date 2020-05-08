using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.Window;
using System;

namespace JeremyAnsel.DirectX.SdkCamera
{
    public class SdkArcBall
    {
        // Matrix for arc ball's orientation
        protected XMMatrix m_mRotation;

        // Matrix for arc ball's position
        protected XMMatrix m_mTranslation;

        // Matrix for arc ball's position
        protected XMMatrix m_mTranslationDelta;

        // window offset, or upper-left corner of window
        protected XMInt2 m_Offset;

        // arc ball's window width
        protected int m_nWidth;

        // arc ball's window height
        protected int m_nHeight;

        // center of arc ball
        protected XMFloat2 m_vCenter;

        // arc ball's radius in screen coords
        protected float m_fRadius;

        // arc ball's radius for translating the target
        protected float m_fRadiusTranslation;

        // Quaternion before button down
        protected XMVector m_qDown;

        // Composite quaternion for current drag
        protected XMVector m_qNow;

        // Whether user is dragging arc ball
        protected bool m_bDrag;

        // position of last mouse point
        protected XMInt2 m_ptLastMouse;

        // starting point of rotation arc
        protected XMFloat3 m_vDownPt;

        // current point of rotation arc
        protected XMFloat3 m_vCurrentPt;

        public SdkArcBall()
        {
            Reset();

            NativeMethods.GetClientRect(NativeMethods.GetForegroundWindow(), out XMInt4 rect);
            SetWindow(rect.Z, rect.W);
        }

        public void Reset()
        {
            XMVector qid = XMQuaternion.Identity;
            m_qDown = qid;
            m_qNow = qid;

            XMMatrix id = XMMatrix.Identity;
            m_mRotation = id;
            m_mTranslation = id;
            m_mTranslationDelta = id;

            m_bDrag = false;
            m_fRadiusTranslation = 1.0f;
            m_fRadius = 1.0f;
        }

        public void SetTranslationRadius(float fRadiusTranslation)
        {
            m_fRadiusTranslation = fRadiusTranslation;
        }

        public void SetWindow(int nWidth, int nHeight)
        {
            SetWindow(nWidth, nHeight, 0.9f);
        }

        public void SetWindow(int nWidth, int nHeight, float fRadius)
        {
            m_nWidth = nWidth;
            m_nHeight = nHeight;
            m_fRadius = fRadius;
            m_vCenter.X = m_nWidth / 2.0f;
            m_vCenter.Y = m_nHeight / 2.0f;
        }

        public void SetOffset(int nX, int nY)
        {
            m_Offset.X = nX;
            m_Offset.Y = nY;
        }

        public void OnBegin(int nX, int nY)
        {
            // Only enter the drag state if the click falls
            // inside the click rectangle.
            if (nX >= m_Offset.X &&
                nX < m_Offset.X + m_nWidth &&
                nY >= m_Offset.Y &&
                nY < m_Offset.Y + m_nHeight)
            {
                m_bDrag = true;
                m_qDown = m_qNow;
                XMVector v = ScreenToVector(nX, nY);
                m_vDownPt = v;
            }
        }

        public void OnMove(int nX, int nY)
        {
            if (m_bDrag)
            {
                XMVector curr = ScreenToVector(nX, nY);
                m_vCurrentPt = curr;

                XMVector down = m_vDownPt;
                XMVector qdown = m_qDown;

                XMVector result = XMQuaternion.Multiply(qdown, QuatFromBallPoints(down, curr));
                m_qNow = result;
            }
        }

        public void OnEnd()
        {
            m_bDrag = false;
        }

        public void HandleMessages(IntPtr hWnd, WindowMessageType msg, IntPtr wParam, IntPtr lParam)
        {
            // Current mouse position
            int iMouseX = (short)((ulong)lParam & 0xffffU);
            int iMouseY = (short)((ulong)lParam >> 16);

            switch (msg)
            {
                case WindowMessageType.LeftButtonDown:
                case WindowMessageType.LeftButtonDoubleClick:
                    {
                        NativeMethods.SetCapture(hWnd);
                        OnBegin(iMouseX, iMouseY);
                        break;
                    }

                case WindowMessageType.LeftButtonUp:
                    {
                        NativeMethods.ReleaseCapture();
                        OnEnd();
                        break;
                    }

                case WindowMessageType.CaptureChanged:
                    {
                        if (lParam != hWnd)
                        {
                            NativeMethods.ReleaseCapture();
                            OnEnd();
                        }

                        break;
                    }

                case WindowMessageType.RightButtonDown:
                case WindowMessageType.RightButtonDoubleClick:
                case WindowMessageType.MiddleButtonDown:
                case WindowMessageType.MiddleButtonDoubleClick:
                    {
                        NativeMethods.SetCapture(hWnd);
                        // Store off the position of the cursor when the button is pressed
                        m_ptLastMouse.X = iMouseX;
                        m_ptLastMouse.Y = iMouseY;
                        break;
                    }

                case WindowMessageType.RightButtonUp:
                case WindowMessageType.MiddleButtonUp:
                    {
                        NativeMethods.ReleaseCapture();
                        break;
                    }

                case WindowMessageType.MouseMove:
                    {
                        MouseKeys key = (MouseKeys)wParam;

                        if ((key & MouseKeys.LeftButton) != 0)
                        {
                            OnMove(iMouseX, iMouseY);
                        }
                        else if ((key & (MouseKeys.RightButton | MouseKeys.MiddleButton)) != 0)
                        {
                            // Normalize based on size of window and bounding sphere radius
                            float fDeltaX = (m_ptLastMouse.X - iMouseX) * m_fRadiusTranslation / m_nWidth;
                            float fDeltaY = (m_ptLastMouse.Y - iMouseY) * m_fRadiusTranslation / m_nHeight;

                            XMMatrix mTranslationDelta;
                            XMMatrix mTranslation = m_mTranslation;

                            if ((key & MouseKeys.RightButton) != 0)
                            {
                                mTranslationDelta = XMMatrix.Translation(-2 * fDeltaX, 2 * fDeltaY, 0.0f);
                                mTranslation = XMMatrix.Multiply(mTranslation, mTranslationDelta);
                            }
                            // MouseKeys.MiddleButton
                            else
                            {
                                mTranslationDelta = XMMatrix.Translation(0.0f, 0.0f, 5 * fDeltaY);
                                mTranslation = XMMatrix.Multiply(mTranslation, mTranslationDelta);
                            }

                            m_mTranslationDelta = mTranslationDelta;
                            m_mTranslation = mTranslation;

                            // Store mouse coordinate
                            m_ptLastMouse.X = iMouseX;
                            m_ptLastMouse.Y = iMouseY;
                        }

                        break;
                    }
            }
        }

        public XMMatrix GetRotationMatrix()
        {
            return XMMatrix.RotationQuaternion(m_qNow);
        }

        public XMMatrix GetTranslationMatrix()
        {
            return m_mTranslation;
        }

        public XMMatrix GetTranslationDeltaMatrix()
        {
            return m_mTranslationDelta;
        }

        public bool IsBeingDragged()
        {
            return m_bDrag;
        }

        public XMVector GetQuatNow()
        {
            return m_qNow;
        }

        public void SetQuatNow(XMVector q)
        {
            m_qNow = q;
        }

        public static XMVector QuatFromBallPoints(XMVector vFrom, XMVector vTo)
        {
            XMVector dot = XMVector3.Dot(vFrom, vTo);
            XMVector vPart = XMVector3.Cross(vFrom, vTo);
            return XMVector.Select(dot, vPart, XMVector.SelectControl(1, 1, 1, 0));
        }

        protected XMVector ScreenToVector(float fScreenPtX, float fScreenPtY)
        {
            // Scale to screen
            float x = -(fScreenPtX - m_Offset.X - m_nWidth / 2) / (m_fRadius * m_nWidth / 2);
            float y = (fScreenPtY - m_Offset.Y - m_nHeight / 2) / (m_fRadius * m_nHeight / 2);

            float z = 0.0f;
            float mag = x * x + y * y;

            if (mag > 1.0f)
            {
                float scale = 1.0f / (float)Math.Sqrt(mag);
                x *= scale;
                y *= scale;
            }
            else
            {
                z = (float)Math.Sqrt(1.0f - mag);
            }

            return new XMVector(x, y, z, 0.0f);
        }
    }
}
