using System;
using System.IO;
using System.Reflection;

namespace RttiObfuscator.Properties
{
    public static class Resources
    {
        private static Byte[] GetResource(String Name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(String.Format("RttiObfuscator.{0}", Name)))
            using (var br = new BinaryReader(s))
            {
                return br.ReadBytes((int)(s.Length));
            }
        }

        public static Byte[] ItaniumBaseExp { get { return GetResource("itanium-base.exp"); } }
    }
}
