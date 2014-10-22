using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public class CostlyBaseline
    {

        public void DoWork()
        {
            //Allocate 16MiB +4MiB of memory, fill 16MiB memory with a 32-byte pattern, then downscale
            var factor = 2;
            var sx = 4096;
            var sy = sx;
            var dx = sx/factor;
            var dy = dx;
            var fromBuffer = new byte[sx * sy];

            var toBuffer = new byte[dx * dy];

            var pattern = new byte[]{5,23,62,88,1,201,192,36,0,0,129,177,159,245,255,108,183,93,17,16,1,201,192,36,0,0,129,177,245,255,108,183 };
            for (var i = 0; i < sx * sy ; i+=32 )
            {
                Array.Copy(pattern, 0, fromBuffer,i, 32);
            }

            for (var y = 0; y < dy; y++)
            {
                for (var x = 0; x < dx; x++)
                {
                    var destIx = dx * y + x;

                    int total = 0;
                    for (var yf = 0; yf < factor; yf++){
                        for (var xf = 0; xf < factor; xf++){
                            total += fromBuffer[sx * (dy + yf) * factor + (x + xf) * factor];
                        }
                    }

                    toBuffer[destIx] = (byte)(total / (factor * factor));
                }
            }

        }

    }
}
