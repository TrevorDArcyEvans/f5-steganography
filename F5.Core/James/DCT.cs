﻿namespace F5.Core.James;

using System;
using Util;

// Version 1.0a
// Copyright (C) 1998, James R. Weeks and BioElectroMech.
// Visit BioElectroMech at www.obrador.com. Email James@obrador.com.

// See license.txt for details about the allowed used of this software.
// This software is based in part on the work of the Independent JPEG Group.
// See IJGreadme.txt for details about the Independent JPEG Group's license.

// This encoder is inspired by the Java Jpeg encoder by Florian Raemy,
// studwww.eurecom.fr/~raemy.
// It borrows a great deal of code and structure from the Independent
// Jpeg Group's Jpeg 6a library, Copyright Thomas G. Lane.
// See license.txt for details.

/// <summary>
///   DCT - A .NET implementation of the Discreet Cosine Transform
/// </summary>
internal sealed class DCT
{
  // DCT Block Size - default 8
  public const int N = 8;
  internal double[][] Divisors;

  internal int[][] Quantum;

  /// <summary>
  ///   Constructs a new DCT object. Initializes the cosine transform matrix
  ///   these are used when computing the DCT and it's inverse. This also
  ///   initializes the run length counters and the ZigZag sequence. Note that
  ///   the image quality can be worse than 25 however the image will be extemely
  ///   pixelated, usually to a block size of N.
  /// </summary>
  /// <param name="quality">The quality of the image (0 worst - 100 best)</param>
  internal DCT(int quality = 80)
  {
    InitMatrix(quality);
  }

  /// <summary>
  ///   This method sets up the quantization matrix for luminance and chrominance using the Quality parameter.
  /// </summary>
  private void InitMatrix(int quality)
  {
    quality = quality switch
    {
      // converting quality setting to that specified in the jpeg_quality_scaling method in the IJG Jpeg-6a C libraries
      <= 0 => 1,
      > 100 => 100,
      < 50 => 5000 / quality,
      _ => 200 - quality * 2
    };

    // Quantitization Matrix for luminance.
    var quantum_luminance = new int[N * N]
    {
      16, 11, 10, 16, 24, 40, 51, 61,
      12, 12, 14, 19, 26, 58, 60, 55,
      14, 13, 16, 24, 40, 57, 69, 56,
      14, 17, 22, 29, 51, 87, 80, 62,
      18, 22, 37, 56, 68, 109, 103, 77,
      24, 35, 55, 64, 81, 104, 113, 92,
      49, 64, 78, 87, 103, 121, 120, 101,
      72, 92, 95, 98, 112, 100, 103, 99
    };

    // Quantitization Matrix for chrominance.
    var quantum_chrominance = new int[N * N]
    {
      17, 18, 24, 47, 99, 99, 99, 99,
      18, 21, 26, 66, 99, 99, 99, 99,
      24, 26, 56, 99, 99, 99, 99, 99,
      47, 66, 99, 99, 99, 99, 99, 99,
      99, 99, 99, 99, 99, 99, 99, 99,
      99, 99, 99, 99, 99, 99, 99, 99,
      99, 99, 99, 99, 99, 99, 99, 99,
      99, 99, 99, 99, 99, 99, 99, 99
    };

    var luminance = new Matrix(quantum_luminance, quality);
    var chrominance = new Matrix(quantum_chrominance, quality);

    // quantum and Divisors are objects used to hold the appropriate matrices
    Quantum = new[] { luminance.Quantum, chrominance.Quantum };
    Divisors = new[] { luminance.Divisor, chrominance.Divisor };
  }

  /// <summary>
  ///   This method quantitizes data and rounds it to the nearest integer.
  /// </summary>
  public int[] QuantizeBlock(double[][] inputData, int code)
  {
    var outputData = new int[N * N];
    var index = 0;
    for (var i = 0; i < N; i++)
    {
      for (var j = 0; j < N; j++)
      {
        // The second line results in significantly better compression.
        outputData[index] = (int)Math.Round(inputData[i][j] * Divisors[code][index]);
        // outputData[index] = (int)(((inputData[i, j] * (((double[])(Divisors[code]))[index])) + 16384.5) -16384);
        index++;
      }
    }

    return outputData;
  }

  /// <summary>
  ///   This is the method for quantizing a block DCT'ed with forwardDCTExtreme
  ///   This method quantitizes data and rounds it to the nearest integer.
  /// </summary>
  public int[] QuantizeBlockExtreme(double[][] inputData, int code)
  {
    var outputData = new int[N * N];
    var index = 0;
    for (var i = 0; i < N; i++)
    {
      for (var j = 0; j < N; j++)
      {
        outputData[index] = (int)Math.Round(inputData[i][j] / Quantum[code][index]);
        index++;
      }
    }

    return outputData;
  }

  /// <summary>
  ///   This method preforms a DCT on a block of image data using the AAN method as implemented in the IJG Jpeg-6a library.
  /// </summary>
  public static double[][] ForwardDCT(float[][] input)
  {
    var output = ArrayHelper.CreateJagged<double>(N, N);
    double tmp0, tmp1, tmp2, tmp3, tmp4, tmp5, tmp6, tmp7;
    double tmp10, tmp11, tmp12, tmp13;
    double z1, z2, z3, z4, z5, z11, z13;

    // Subtracts 128 from the input values
    for (var i = 0; i < N; i++)
    {
      for (var j = 0; j < N; j++)
      {
        output[i][j] = input[i][j] - 128.0;
      }
    }

    for (var i = 0; i < N; i++)
    {
      tmp0 = output[i][0] + output[i][7];
      tmp7 = output[i][0] - output[i][7];
      tmp1 = output[i][1] + output[i][6];
      tmp6 = output[i][1] - output[i][6];
      tmp2 = output[i][2] + output[i][5];
      tmp5 = output[i][2] - output[i][5];
      tmp3 = output[i][3] + output[i][4];
      tmp4 = output[i][3] - output[i][4];

      tmp10 = tmp0 + tmp3;
      tmp13 = tmp0 - tmp3;
      tmp11 = tmp1 + tmp2;
      tmp12 = tmp1 - tmp2;

      output[i][0] = tmp10 + tmp11;
      output[i][4] = tmp10 - tmp11;

      z1 = (tmp12 + tmp13) * 0.707106781;
      output[i][2] = tmp13 + z1;
      output[i][6] = tmp13 - z1;

      tmp10 = tmp4 + tmp5;
      tmp11 = tmp5 + tmp6;
      tmp12 = tmp6 + tmp7;

      z5 = (tmp10 - tmp12) * 0.382683433;
      z2 = 0.541196100 * tmp10 + z5;
      z4 = 1.306562965 * tmp12 + z5;
      z3 = tmp11 * 0.707106781;

      z11 = tmp7 + z3;
      z13 = tmp7 - z3;

      output[i][5] = z13 + z2;
      output[i][3] = z13 - z2;
      output[i][1] = z11 + z4;
      output[i][7] = z11 - z4;
    }

    for (var i = 0; i < N; i++)
    {
      tmp0 = output[0][i] + output[7][i];
      tmp7 = output[0][i] - output[7][i];
      tmp1 = output[1][i] + output[6][i];
      tmp6 = output[1][i] - output[6][i];
      tmp2 = output[2][i] + output[5][i];
      tmp5 = output[2][i] - output[5][i];
      tmp3 = output[3][i] + output[4][i];
      tmp4 = output[3][i] - output[4][i];

      tmp10 = tmp0 + tmp3;
      tmp13 = tmp0 - tmp3;
      tmp11 = tmp1 + tmp2;
      tmp12 = tmp1 - tmp2;

      output[0][i] = tmp10 + tmp11;
      output[4][i] = tmp10 - tmp11;

      z1 = (tmp12 + tmp13) * 0.707106781;
      output[2][i] = tmp13 + z1;
      output[6][i] = tmp13 - z1;

      tmp10 = tmp4 + tmp5;
      tmp11 = tmp5 + tmp6;
      tmp12 = tmp6 + tmp7;

      z5 = (tmp10 - tmp12) * 0.382683433;
      z2 = 0.541196100 * tmp10 + z5;
      z4 = 1.306562965 * tmp12 + z5;
      z3 = tmp11 * 0.707106781;

      z11 = tmp7 + z3;
      z13 = tmp7 - z3;

      output[5][i] = z13 + z2;
      output[3][i] = z13 - z2;
      output[1][i] = z11 + z4;
      output[7][i] = z11 - z4;
    }

    return output;
  }

  /// <summary>
  ///   This method preforms forward DCT on a block of image data using the
  ///   literal method specified for a 2-D Discrete Cosine Transform. It is
  ///   included as a curiosity and can give you an idea of the difference in the
  ///   compression result (the resulting image quality) by comparing its output
  ///   to the output of the AAN method below. It is ridiculously inefficient.
  ///   For now the final output is unusable. The associated quantization step
  ///   needs some tweaking. If you get this part working, please let me know.
  /// </summary>
  public static double[][] ForwardDCTExtreme(float[][] input)
  {
    var output = ArrayHelper.CreateJagged<double>(N, N);
    for (var v = 0; v < N; v++)
    {
      for (var u = 0; u < N; u++)
      {
        for (var x = 0; x < N; x++)
        {
          for (var y = 0; y < N; y++)
          {
            output[v][u] += input[x][y] * Math.Cos((2 * x + 1) * (double)u * Math.PI / 16)
                                        * Math.Cos((2 * y + 1) * (double)v * Math.PI / 16);
          }
        }

        output[v][u] *= 0.25 * (u == 0 ? 1.0 / Math.Sqrt(2) : 1.0)
                             * (v == 0 ? 1.0 / Math.Sqrt(2) : 1.0);
      }
    }

    return output;
  }

  private class Matrix
  {
    private static readonly double[] AANscaleFactor =
    {
      1.0, 1.387039845, 1.306562965, 1.175875602,
      1.0, 0.785694958, 0.541196100, 0.275899379
    };

    internal readonly double[] Divisor;
    internal readonly int[] Quantum;

    internal Matrix(int[] quantum, int quality)
    {
      Quantum = quantum;
      Divisor = new double[N * N];

      var index = 0;
      for (var i = 0; i < N; i++)
      {
        for (var j = 0; j < N; j++)
        {
          var temp = (Quantum[index] * quality + 50) / 100;
          temp = temp switch
          {
            <= 0 => 1,
            > 255 => 255,
            _ => temp
          };
          Quantum[index] = temp;

          // The divisors for the LL&M method (the slow integer method used in jpeg 6a library). 
          // This method is currently (04/04/98) incompletely implemented.
          // DivisorsLuminance[index] = ((double)quantum_luminance[index]) << 3;
          // The divisors for the AAN method (the float method used in jpeg 6a library.

          // The divisors for the LL&M method (the slow integer method used in jpeg 6a library). 
          // This method is currently (04/04/98) incompletely implemented.
          // DivisorsChrominance[index] = ((double) quantum_chrominance[index]) << 3;
          // The divisors for the AAN method (the float method used in jpeg 6a library.
          Divisor[index] = 1.0 / (Quantum[index] * AANscaleFactor[i] * AANscaleFactor[j] * 8.0);
          index++;
        }
      }
    }
  }
}
