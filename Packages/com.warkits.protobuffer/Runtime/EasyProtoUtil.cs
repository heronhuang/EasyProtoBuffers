using System.IO;
using Google.Protobuf;

namespace Warkits.ToolBox.EasyProto
{
    public class Utility
    {
        public static byte[] Serialize<T>( int netcode, T t ) where T : IMessage<T>
        {
            using ( MemoryStream output = new MemoryStream() )
            {
                output.SetLength(0L);
                output.Seek(0L, SeekOrigin.Begin);
                output.WriteByte((byte) netcode);
                t.WriteTo((Stream) output);
                return output.ToArray();
            }
        }
    } 
}

