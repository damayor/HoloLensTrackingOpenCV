
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenCVForUnity
{

    // C++: class FacemarkKazemi
    //javadoc: FacemarkKazemi

    public class FacemarkKazemi : Facemark
    {

        protected override void Dispose (bool disposing)
        {
#if UNITY_PRO_LICENSE || ((UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR) || UNITY_5 || UNITY_5_3_OR_NEWER
try {
if (disposing) {
}
if (IsEnabledDispose) {
if (nativeObj != IntPtr.Zero)
face_FacemarkKazemi_delete(nativeObj);
nativeObj = IntPtr.Zero;
}
} finally {
base.Dispose (disposing);
}
#else
            return;
#endif
        }

        protected internal FacemarkKazemi (IntPtr addr) : base (addr) { }

        // internal usage only
        public static new FacemarkKazemi __fromPtr__ (IntPtr addr) { return new FacemarkKazemi (addr); }

#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        const string LIBNAME = "__Internal";
#else
        const string LIBNAME = "opencvforunity";
#endif



        // native support for java finalize()
        [DllImport (LIBNAME)]
        private static extern void face_FacemarkKazemi_delete (IntPtr nativeObj);

    }
}
