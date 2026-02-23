using System;
using System.IO;
using System.Linq;

namespace Netly
{
    public static class NHelper
    {
        /// <summary>
        ///     Returns a new <see cref="ArraySegment{T}" /> shifted by the specified number of elements.
        ///     If the shift is greater than or equal to the segment count, an empty segment at the end is returned.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array segment.</typeparam>
        /// <param name="segment">The original array segment.</param>
        /// <param name="shift">The number of elements to skip from the start.</param>
        /// <returns>A new array segment representing the shifted portion.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="shift" /> is negative.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the array in <paramref name="segment" /> is null.</exception>
        public static ArraySegment<T> SegmentShift<T>(ArraySegment<T> segment, int shift)
        {
            if (shift < 0)
                throw new ArgumentOutOfRangeException(nameof(shift));

            if (segment.Array == null)
                throw new ArgumentNullException(nameof(segment));

            return shift >= segment.Count
                ? new ArraySegment<T>(segment.Array, segment.Offset + segment.Count, 0)
                : new ArraySegment<T>(segment.Array, shift + segment.Offset, segment.Count - shift);
        }

        /// <summary>
        ///     Compares all elements in the given array to determine if they are equal.
        ///     For arrays with more than one element, returns true if all elements are the same.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array. Must support equality comparison.</typeparam>
        /// <param name="array">The array of elements to compare.</param>
        /// <returns>
        ///     True if all elements are equal or the array has zero or one element; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="array" /> is null.</exception>
        public static bool ArraySequenced<T>(params T[][] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length <= 1)
                return true;

            var reference = array[0];

            for (var index = 1; index < array.Length; index++)
                if (reference.Where((value, offset) => !value.Equals(array[index][offset])).Any())
                    return false;

            return true;
        }

        /// <summary>
        ///     Returns a string representation of an array in the format [elem1,elem2,...].
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="elements">The array to format.</param>
        /// <returns>A string representing the array elements.</returns>
        public static string Format<T>(T[] elements)
        {
            return $"[{string.Join(",", elements)}]";
        }

        /// <summary>
        ///     Concatenates all elements of the given array into a new array.
        ///     This is mainly useful if you want a copy of the array or to combine multiple arrays later.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="elements">The array of elements to concatenate.</param>
        /// <returns>A new array containing all elements from the input array in order.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elements" /> is null.</exception>
        public static T[] ArrayConcat<T>(params T[][] elements)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            // Creates a new array with the same elements
            var result = new T[elements.Sum(x => x.LongLength)];
            var offset = 0L;

            for (var i = 0; i < elements.LongLength; i++)
            {
                var element = elements[i];
                Array.Copy(element, 0, result, offset, element.LongLength);
                offset += element.LongLength;
            }

            return result;
        }

        /// <summary>
        ///     Creates a new MemoryStream with the requested size.
        ///     Throws <see cref="InternalBufferOverflowException" /> if the size exceeds the default maximum.
        /// </summary>
        /// <param name="size">The desired size of the stream.</param>
        /// <returns>A <see cref="Stream" /> with the specified size.</returns>
        public static Stream NewStream(long size)
        {
            if (size <= 1024 * 1024 * 20) // 20.00 MB
                return new MemoryStream((int)size);

            throw new InternalBufferOverflowException($"{nameof(NewStream)}, {nameof(size)}: {size}");
        }
    }
}