using System;
using System.IO;
using System.Linq;

namespace Netly
{
    /// <summary>
    ///     Small helpers for arrays, segments and streams used by Netly.
    /// </summary>
    public static class Helper
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
        ///     Biggest size (64 MiB) of a <see cref="NewStream" /> kept in memory, bigger goes to a temporary file.
        /// </summary>
        public const long MemoryLimit = 64L * 1024 * 1024;

        /// <summary>
        ///     Creates a new empty <see cref="Stream" /> to receive <paramref name="size" /> bytes.<br />
        ///     Up to <see cref="MemoryLimit" /> it is a <see cref="MemoryStream" />, otherwise a temporary file
        ///     deleted when the stream is closed. If the file can't be created it falls back to memory.
        /// </summary>
        /// <param name="size">The size of the data that will be written.</param>
        /// <returns>A <see cref="Stream" /> with position 0.</returns>
        /// <exception cref="InvalidDataException">Thrown if <paramref name="size" /> is negative.</exception>
        /// <exception cref="InternalBufferOverflowException">
        ///     Thrown if the file can't be created and <paramref name="size" /> doesn't fit in memory.
        /// </exception>
        public static Stream NewStream(long size)
        {
            if (size > MemoryLimit)
                CleanOnce();

            if (size < 0)
                throw new InvalidDataException($"{nameof(size)}: {size}");

            if (size <= MemoryLimit)
                return new MemoryStream((int)size);

            try
            {
                var directory = TemporaryDirectory;

                Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");

                return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920,
                    FileOptions.DeleteOnClose);
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }

            if (size <= int.MaxValue)
                return new MemoryStream();

            throw new InternalBufferOverflowException($"{nameof(NewStream)}, {nameof(size)}: {size}");
        }

        private static readonly string TemporaryDirectory = Path.Combine(Path.GetTempPath(), "Netly");

        private static int _cleaned;

        // a crash can leave temporary files, removed once per process when the first one is needed
        private static void CleanOnce()
        {
            if (System.Threading.Interlocked.Exchange(ref _cleaned, 1) == 0)
                CleanTemporaryFiles(TimeSpan.FromHours(1));
        }

        internal static void CleanTemporaryFiles(TimeSpan olderThan)
        {
            try
            {
                if (Directory.Exists(TemporaryDirectory) is false) return;

                var limit = DateTime.UtcNow - olderThan;

                foreach (var file in Directory.GetFiles(TemporaryDirectory, "*.tmp"))
                    try
                    {
                        // files in use by another process are locked and stay
                        if (File.GetLastWriteTimeUtc(file) < limit) File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        Logger.Singleton.Submit(e);
                    }
            }
            catch (Exception e)
            {
                Logger.Singleton.Submit(e);
            }
        }
    }
}
