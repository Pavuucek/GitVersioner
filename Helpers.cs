using System;

namespace GitVersioner
{
    /// <summary>
    ///     Class extension helpers
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        ///     Tries to convert a string to an int.
        /// </summary>
        /// <param name="stringNumber">a string</param>
        /// <returns>zero if failed</returns>
        public static int TryToInt32(this string stringNumber)
        {
            int result;
            try
            {
                result = Convert.ToInt32(stringNumber);
            }
            catch
            {
                result = 0;
            }

            return result;
        }
    }
}