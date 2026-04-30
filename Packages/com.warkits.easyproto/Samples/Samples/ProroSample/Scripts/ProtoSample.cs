using Razor.Net;
using UnityEngine; 
using Warkits.ToolBox.EasyProto;
namespace Warkits.ToolBox.EasyProto
{
    public class NetCode
    { 
        public const byte SampleProto0         =   0;
        public const byte SampleProto1         =   3;
        public const byte SampleProto2         =   12; 
    }
    
    public class ProtoSample : MonoBehaviour
    {
        // Start is called before the first frame update
        byte[] Serilize()
        {
            TestMessage sampleMsg = new TestMessage()
            {
                Msg  = "Hello from Easy ProtoBuffers",
                Code = 23
            };
            return Utility.Serialize(NetCode.SampleProto1, sampleMsg); 
        }
        void DeSerilize( byte[] bytes )
        {
            var testMsg = TestMessage.Parser.ParseFrom(bytes, 1, bytes.Length - 1);
            
             Debug.Log($"From net  Msg: [<color=green>{testMsg.Msg}</color>] code:[<color=yellow>{testMsg.Code}</color>]"); 
        }
        
        void Start()
        {
            byte[] bytes = Serilize();
            //.. Send bytes by network......
            //..
            //
            //byte[] = recv();
            DeSerilize(bytes);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }

}
