using System;
using System.Collections.Generic;

namespace SEAMonster
{
    public static class Common
    {
        /// <summary>
        /// Shifts a two-dimensional array given a specified direction.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">Two-dimensional array to shift</param>
        /// <param name="direction">Direction to shift values</param>
        /// <param name="offset">Array offset in the specified direction</param>
        /// <param name="shiftIndex">Index to begin shifting</param>
        /// <param name="toIndex">Index to stop shifting</param>
        /// <param name="emptyValue">Value to replace at toIndex</param>
        public static void ShiftArray<T>(T[,] array, Direction direction, int offset, int shiftIndex, int toIndex, object emptyValue)
        {
            int x = 0, y = 0;
            int xMax = int.MaxValue, yMax = int.MaxValue;
            int xInc = 0, yInc = 0;                            // x and y increments

            // Set initial values
            if (direction == Direction.Vertical)
            {
                x = shiftIndex;
                y = offset;
                xMax = toIndex - 1;
                xInc = 1;
            }
            else if (direction == Direction.Horizontal)
            {
                x = offset;
                y = shiftIndex;
                yMax = toIndex - 1;
                yInc = 1;
            }

            // Iterate
            while (x < xMax && y < yMax)
            {
                array[x,y] = array[x + xInc, y + yInc];

                // Increment
                x += xInc;
                y += yInc;
            }

            // Set value at edge of array
            array[x, y] = (T)emptyValue;
        }
    }
}
