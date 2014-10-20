using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ImageResizer.Tests {
 
    public static class TestPolygonMath {
        [Theory]
        [InlineData(0d, 8d, 5d, 40d)]
        [InlineData(1d, 8d, 5d, 5d)]
        [InlineData(-1d, 8d, 5d, 80d)]
        public static void TestPercentConcurrent(double expected, double degree, double sequentialTime, double parallelTime) {

            var serialized = sequentialTime * degree;

            //var result = ((sequentialTime * degree) - parallelTime) / (sequentialTime * (degree-1));
            double result;
            //var result = ((sequentialTime * degree) - parallelTime) / (sequentialTime * (degree-1));
            result = (serialized - parallelTime) / (serialized - (parallelTime <= serialized ? sequentialTime : 0));

          
            
            Assert.InRange(result, expected * (expected > 0 ? 0.99 : 1.01), expected * (expected > 0 ? 1.01 : 0.99));
        }
    }
}
