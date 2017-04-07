﻿using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ConvolutionalNeuralNetworkLibrary
{
    /// <summary>
    /// An helper class with methods to process fixed-size matrices
    /// </summary>
    public static class MatrixHelper
    {
        #region CNN

        /// <summary>
        /// Returns the normalized matrix with a max value of 1
        /// </summary>
        /// <param name="m">The input matrix to normalize</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Normalize([NotNull] this double[,] m)
        {
            // Prepare the result matrix
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] result = new double[h, w];

            // Pool the input matrix
            unsafe
            {
                fixed(double* p = m, r = result)
                {
                    // Get the max value
                    double max = 0;
                    for (int i = 0; i < m.Length; i++)
                        if (p[i] > max) max = p[i];

                    // Normalize the matrix content
                    for (int i = 0; i < m.Length; i++)
                        r[i] = p[i] / max;
                }
            }
            return result;
        }

        /// <summary>
        /// Pools the input matrix with a window of 2 and a stride of 2
        /// </summary>
        /// <param name="m">The input matrix to pool</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Pool2x2([NotNull] this double[,] m)
        {
            // Prepare the result matrix
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] result = new double[h / 2, w / 2];

            // Pool the input matrix
            int x = 0;
            for (int i = 0; i < h - 1; i += 2)
            {
                int y = 0;
                for (int j = 0; j < w - 1; j += 2)
                {
                    double
                        maxUp = m[i, j] > m[i, j + 1] ? m[i, j] : m[i, j + 1],
                        maxDown = m[i + 1, j] > m[i + 1, j + 1] ? m[i + 1, j] : m[i + 1, j + 1],
                        max = maxUp > maxDown ? maxUp : maxDown;
                    result[x, y++] = max;
                }
                x++;
            }
            return result;
        }

        /// <summary>
        /// Performs the Rectified Linear Units operation on the input matrix (applies a minimum value of 0)
        /// </summary>
        /// <param name="m">The input matrix to edit</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] ReLU([NotNull] this double[,] m)
        {
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] result = new double[h, w];
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                    result[i, j] = m[i, j] >= 0 ? m[i, j] : 0;
            return result;
        }

        /// <summary>
        /// Convolutes the input matrix with the given 3x3 kernel
        /// </summary>
        /// <param name="m">The input matrix</param>
        /// <param name="kernel">The 3x3 convolution kernel to use</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Convolute3x3([NotNull] this double[,] m, [NotNull] double[,] kernel)
        {
            // Prepare the output matrix
            if (kernel.Length != 9) throw new ArgumentOutOfRangeException("The input kernel must be 3x3");
            int h = m.GetLength(0), w = m.GetLength(1);
            if (h < 3 || w < 3) throw new ArgumentOutOfRangeException("The input matrix must be at least 3x3");
            double[,] result = new double[h - 2, w - 2];

            // Calculate the normalization factor
            double Abs(double value) => value >= 0 ? value : -value;
            double factor = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    factor += Abs(kernel[i, j]);

            // Process the convolution
            int x = 0;
            for (int i = 1; i < h - 1; i++)
            {
                int y = 0;
                for (int j = 1; j < w - 1; j++)
                {
                    double
                        partial =
                            m[i - 1, j - 1] * kernel[0, 0] +
                            m[i - 1, j] * kernel[0, 1] +
                            m[i - 1, j + 1] * kernel[0, 2] +
                            m[i, j - 1] * kernel[1, 0] +
                            m[i, j] * kernel[1, 1] +
                            m[i, j + 1] * kernel[1, 2] +
                            m[i + 1, j - 1] * kernel[2, 0] +
                            m[i + 1, j] * kernel[2, 1] +
                            m[i + 1, j + 1] * kernel[2, 2],
                        normalized = partial / factor;
                    result[x, y++] = normalized;
                }
                x++;
            }
            return result;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Performs the multiplication between a vector and a matrix
        /// </summary>
        /// <param name="v">The input vector</param>
        /// <param name="m">The matrix to multiply</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[] Multiply([NotNull] this double[] v, [NotNull] double[,] m)
        {
            // Check
            if (v.Length != m.GetLength(0)) throw new ArgumentOutOfRangeException("Invalid inputs sizes");

            // Initialize the parameters and the result vector
            int w = m.GetLength(1);
            double[] result = new double[w];

            // Loop in parallel
            ParallelLoopResult loopResult = Parallel.For(0, w, j =>
            {
                unsafe
                {
                    // Get the pointers and iterate fo each column
                    fixed (double* pm = result, p1 = v, p2 = m)
                    {
                        // Perform the multiplication
                        int j2 = j;
                        double res = 0;
                        for (int k = 0; k < v.Length; k++, j2 += w)
                        {
                            res += p1[k] * p2[j2];
                        }
                        pm[j] = res;
                    }
                }
            });
            if (!loopResult.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return result;
        }

        /// <summary>
        /// Performs the multiplication between two matrices
        /// </summary>
        /// <param name="m1">The first matrix to multiply</param>
        /// <param name="m2">The second matrix to multiply</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Multiply([NotNull] this double[,] m1, [NotNull] double[,] m2)
        {
            // Checks
            if (m1.GetLength(1) != m2.GetLength(0)) throw new ArgumentOutOfRangeException("Invalid matrices sizes");

            // Initialize the parameters and the result matrix
            int h = m1.GetLength(0);
            int w = m2.GetLength(1);
            int l = m1.GetLength(1);
            double[,] result = new double[h, w];

            // Execute the multiplication in parallel
            ParallelLoopResult loopResult = Parallel.For(0, h, i =>
            {
                unsafe
                {
                    // Get the pointers and iterate fo each row
                    fixed (double* pm = result, pm1 = m1, pm2 = m2)
                    {
                        // Save the index and iterate for each column
                        int i1 = i * l;
                        for (int j = 0; j < w; j++)
                        {
                            // Perform the multiplication
                            int i2 = j;
                            double res = 0;
                            for (int k = 0; k < l; k++, i2 += w)
                            {
                                res += pm1[i1 + k] * pm2[i2];
                            }
                            pm[i * w + j] = res;
                        }
                    }
                }
            });
            if (!loopResult.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return result;
        }

        /// <summary>
        /// Transposes the input matrix
        /// </summary>
        /// <param name="m">The matrix to transpose</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Transpose([NotNull] this double[,] m)
        {
            // Setup
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] result = new double[w, h];

            // Execute the transposition in parallel
            ParallelLoopResult loopResult = Parallel.For(0, h, i =>
            {
                unsafe
                {
                    fixed (double* pr = result, pm = m)
                    {
                        for (int j = 0; j < w; j++)
                            pr[j * h + i] = pm[i * w + j];
                    }
                }
            });
            if (!loopResult.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return result;
        }

        /// <summary>
        /// Returns the result of the input after the activation function has been applied
        /// </summary>
        /// <param name="v">The input to process</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[] Sigmoid([NotNull] this double[] v)
        {
            double[] result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = 1 / (1 + Math.Exp(-v[i]));
            return result;
        }

        /// <summary>
        /// Returns the result of the input after the activation function has been applied
        /// </summary>
        /// <param name="m">The input to process</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Sigmoid([NotNull] this double[,] m)
        {
            // Setup
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] result = new double[h, w];

            // Execute the sigmoid in parallel
            ParallelLoopResult loopResult = Parallel.For(0, h, i =>
            {
                unsafe
                {
                    fixed (double* pr = result, pm = m)
                    {
                        for (int j = 0; j < w; j++)
                            pr[i * w + j] = 1 / (1 + Math.Exp(-pm[i * w + j]));
                    }
                }
            });
            if (!loopResult.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return result;
        }

        /// <summary>
        /// Returns the result of the input after the activation function primed has been applied
        /// </summary>
        /// <param name="v">The input to process</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[] SigmoidPrime([NotNull] this double[] v)
        {
            double[] result = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                double
                    exp = Math.Exp(-v[i]),
                    sum = 1 + exp,
                    square = sum * sum,
                    div = exp / square;
                result[i] = div;
            }
            return result;
        }

        /// <summary>
        /// Returns the result of the input after the activation function primed has been applied
        /// </summary>
        /// <param name="m">The input to process</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] SigmoidPrime([NotNull] this double[,] m)
        {
            // Setup
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] result = new double[h, w];

            // Execute the sigmoid prime in parallel
            ParallelLoopResult loopResult = Parallel.For(0, h, i =>
            {
                unsafe
                {
                    fixed (double* pr = result, pm = m)
                    {
                        for (int j = 0; j < w; j++)
                        {
                            double
                                exp = Math.Exp(-pm[i * w + j]),
                                sum = 1 + exp,
                                square = sum * sum,
                                div = exp / square;
                            pr[i * w + j] = div;
                        }
                    }
                }
            });
            if (!loopResult.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return result;
        }

        /// <summary>
        /// Flattens the input volume in a linear array
        /// </summary>
        /// <param name="volume">The volume to flatten</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[] Flatten([NotNull] this double[][,] volume)
        {
            // Preliminary checks and declarations
            if (volume.Length == 0) throw new ArgumentOutOfRangeException("The input volume can't be empty");
            int
                depth = volume.Length,
                h = volume[0].GetLength(0),
                w = volume[0].GetLength(1);
            double[] result = new double[h * w * depth];

            // Execute the copy in parallel
            ParallelLoopResult loopResult = Parallel.For(0, depth, i =>
            {
                // Copy the volume data
                unsafe
                {
                    fixed (double* r = result, p = volume[i])
                    {
                        // Copy each 2D matrix
                        for (int j = 0; j < h; j++)
                            for (int z = 0; z < w; z++)
                                r[h * w * i + j * w + z] = p[j * w + z];
                    }
                }
            });
            if (!loopResult.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return result;
        }

        /// <summary>
        /// Randomizes part of the content of a matrix
        /// </summary>
        /// <param name="m">The matrix to randomize</param>
        /// <param name="probability">The probabiity of each matrix element to be randomized</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[,] Randomize([NotNull] this double[,] m, double probability)
        {
            if (probability < 0 || probability > 1) throw new ArgumentOutOfRangeException("The probability must be in the [0, 1] range");
            double inverse = 1.0 - probability;
            int h = m.GetLength(0), w = m.GetLength(1);
            double[,] randomized = new double[h, w];
            ParallelLoopResult result = Parallel.For(0, m.GetLength(0), i =>
            {
                // Get the random instance and fix the pointers
                Random random = new Random();
                unsafe
                {
                    fixed (double* r = randomized, pm = m)
                    {
                        // Populate the resulting matrix
                        for (int j = 0; j < w; j++)
                        {
                            if (random.NextDouble() >= inverse)
                                r[i * w + j] = random.NextDouble();
                            else r[i * w + j] = pm[i * w + j];
                        }
                    }
                }
            });
            if (!result.IsCompleted) throw new Exception("Error while runnig the parallel loop");
            return randomized;
        }

        /// <summary>
        /// Randomizes part of the content of a vector
        /// </summary>
        /// <param name="v">The vector to randomize</param>
        /// <param name="probability">The probabiity of each vector element to be randomized</param>
        [PublicAPI]
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        public static double[] Randomize([NotNull] this double[] v, double probability)
        {
            // Checks
            if (probability < 0 || probability > 1) throw new ArgumentOutOfRangeException("The probability must be in the [0, 1] range");
            double inverse = 1.0 - probability;
            double[] randomized = new double[v.Length];

            // Populate the resulting vector
            unsafe
            {
                fixed (double* r = randomized, pv = v)
                {
                    for (int i = 0; i < v.Length; i++)
                    {
                        Random random = new Random();
                        if (random.NextDouble() >= inverse)
                            r[i] = random.NextDouble();
                        else r[i] = pv[i];
                    }
                }
            }
            return randomized;
        }

        #endregion
    }
}
