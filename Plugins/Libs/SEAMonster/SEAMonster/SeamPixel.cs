using System;
using System.Collections.Generic;
using System.Text;

namespace SEAMonster
{
    public class SeamPixel
    {
        // Use boolean values for lighter payload
        public bool left = false;          // Turn left (negative direction)?
        public bool right = false;         // Turn right (positive direction)?
        public int pixelDiff = 0;          // Energy difference between last pixel and this one (always positive)
    }
}
