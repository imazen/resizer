// Copyright © 2007  by Libor Tinka
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ResampleTest {

  #region ResamplingService class

  /// <summary>
  /// A class for image resampling with custom filters.
  /// </summary>
  public class ResamplingService {

    #region Properties

    /// <summary>
    /// Gets or sets the resampling filter.
    /// </summary>
    public ResamplingFilters Filter {

      get {
        return this.filter;
      }
      set {
        this.filter = value;
      }
    }

    /// <summary>
    /// Gets or sets wheter the resampling process is stopping.
    /// </summary>
    public bool Aborting {

      get {
        return this.aborting;
      }
      set {
        this.aborting = value;
      }
    }

    /// <summary>
    /// Covered units. Progress can be computed in combination with ResamplingService.Total property.
    /// </summary>
    public int Covered {

      get {
        return this.covered;
      }
    }

    /// <summary>
    /// Total units. Progress can be computer in combination with ResamplingService.Covered property.
    /// </summary>
    public int Total {

      get {
        return this.total;
      }
    }

    #endregion

    #region Structures

    internal struct Contributor {

      public int pixel;
      public double weight;
    }

    internal struct ContributorEntry {

      public int n;
      public Contributor[] p;
      public double wsum;
    }

    #endregion

    #region Private Fields

    private bool aborting = false;
    private int covered = 0, total = 0;
    private ResamplingFilters filter = ResamplingFilters.Box;

    #endregion

    #region Public Methods

    /// <summary>
    /// Resamples input array to a new array using current resampling filter.
    /// </summary>
    /// <param name="input">Input array.</param>
    /// <param name="nWidth">Width of the output array.</param>
    /// <param name="nHeight">Height of the output array.</param>
    /// <returns>Output array.</returns>
    public ushort[][,] Resample(ushort[][,] input, int nWidth, int nHeight) {

      if (input == null || input.Length == 0 || nWidth <= 1 || nHeight <= 1)
        return null;

      ResamplingFilter filter = ResamplingFilter.Create(this.filter);

      int width = input[0].GetLength(0);
      int height = input[0].GetLength(1);
      int planes = input.Length;

      this.covered = 0;
      this.total = (nWidth + height);

      // create bitmaps
      ushort[][,] work = new ushort[planes][,];
      ushort[][,] output = new ushort[planes][,];
      int c = 0;

      for (c = 0; c < planes; c++) {

        work[c] = new ushort[nWidth, height];
        output[c] = new ushort[nWidth, nHeight];
      }

      double xScale = ((double)nWidth / width);
      double yScale = ((double)nHeight / height);

      ContributorEntry[] contrib = new ContributorEntry[nWidth];

      double wdth = 0, center = 0, weight = 0, intensity = 0;
      int left = 0, right = 0, i = 0, j = 0, k = 0;

      // horizontal downsampling
      if (xScale < 1.0) {

        // scales from bigger to smaller width
        wdth = (filter.defaultFilterRadius / xScale);

        for (i = 0; i < nWidth; i++) {

          contrib[i].n = 0;
          contrib[i].p = new Contributor[(int)Math.Floor(2 * wdth + 1)];
          contrib[i].wsum = 0;
          center = ((i + 0.5) / xScale);
          left = (int)(center - wdth);
          right = (int)(center + wdth);

          for (j = left; j <= right; j++) {

            weight = filter.GetValue((center - j - 0.5) * xScale);

            if ((weight == 0) || (j < 0) || (j >= width))
              continue;

            contrib[i].p[contrib[i].n].pixel = j;
            contrib[i].p[contrib[i].n].weight = weight;
            contrib[i].wsum += weight;
            contrib[i].n++;
          }

          if (aborting)
            goto End;
        }
      } else {

        // horizontal upsampling
        // scales from smaller to bigger width
        for (i = 0; i < nWidth; i++) {

          contrib[i].n = 0;
          contrib[i].p = new Contributor[(int)Math.Floor(2 * filter.defaultFilterRadius + 1)];
          contrib[i].wsum = 0;
          center = ((i + 0.5) / xScale);
          left = (int)Math.Floor(center - filter.defaultFilterRadius);
          right = (int)Math.Ceiling(center + filter.defaultFilterRadius);

          for (j = left; j <= right; j++) {

            weight = filter.GetValue(center - j - 0.5);

            if ((weight == 0) || (j < 0) || (j >= width))
              continue;

            contrib[i].p[contrib[i].n].pixel = j;
            contrib[i].p[contrib[i].n].weight = weight;
            contrib[i].wsum += weight;
            contrib[i].n++;          
          }

          if (aborting)
            goto End;
        }
      }

      // filter horizontally from input to work
      for (c = 0; c < planes; c++) {

        for (k = 0; k < height; k++) {

          for (i = 0; i < nWidth; i++) {

            intensity = 0;

            for (j = 0; j < contrib[i].n; j++) {

              weight = contrib[i].p[j].weight;

              if (weight == 0)
                continue;

              intensity += (input[c][contrib[i].p[j].pixel, k] * weight);
            }

            work[c][i, k] = (ushort)Math.Min(Math.Max(intensity / contrib[i].wsum, UInt16.MinValue), UInt16.MaxValue);
          }

          if (aborting)
            goto End;

          this.covered++;
        }
      }

      // pre-calculate filter contributions for a column
      contrib = new ContributorEntry[nHeight];

      // vertical downsampling
      if (yScale < 1.0) {

        // scales from bigger to smaller height
        wdth = (filter.defaultFilterRadius / yScale);

        for (i = 0; i < nHeight; i++) {

          contrib[i].n = 0;
          contrib[i].p = new Contributor[(int)Math.Floor(2 * wdth + 1)];
          contrib[i].wsum = 0;
          center = ((i + 0.5) / yScale);
          left = (int)(center - wdth);
          right = (int)(center + wdth);

          for (j = left; j <= right; j++) {

            weight = filter.GetValue((center - j - 0.5) * yScale);

            if ((weight == 0) || (j < 0) || (j >= height))
              continue;

            contrib[i].p[contrib[i].n].pixel = j;
            contrib[i].p[contrib[i].n].weight = weight;
            contrib[i].wsum += weight;
            contrib[i].n++;
          }

          if (aborting)
            goto End;
        }
      } else {

        // vertical upsampling
        // scales from smaller to bigger height
        for (i = 0; i < nHeight; i++) {

          contrib[i].n = 0;
          contrib[i].p = new Contributor[(int)Math.Floor(2 * filter.defaultFilterRadius + 1)];
          contrib[i].wsum = 0;
          center = ((i + 0.5) / yScale);
          left = (int)(center - filter.defaultFilterRadius);
          right = (int)(center + filter.defaultFilterRadius);

          for (j = left; j <= right; j++) {

            weight = filter.GetValue(center - j - 0.5);

            if ((weight == 0) || (j < 0) || (j >= height))
              continue;

            contrib[i].p[contrib[i].n].pixel = j;
            contrib[i].p[contrib[i].n].weight = weight;
            contrib[i].wsum += weight;
            contrib[i].n++;
          }

          if (aborting)
            goto End;
        }
      }

      // filter vertically from work to output
      for (c = 0; c < planes; c++) {

        for (k = 0; k < nWidth; k++) {

          for (i = 0; i < nHeight; i++) {

            intensity = 0;

            for (j = 0; j < contrib[i].n; j++) {

              weight = contrib[i].p[j].weight;

              if (weight == 0)
                continue;

              intensity += (work[c][k, contrib[i].p[j].pixel] * weight);
            }

            output[c][k, i] = (ushort)Math.Min(Math.Max(intensity, UInt16.MinValue), UInt16.MaxValue);
          }

          if (aborting)
            goto End;

          this.covered++;
        }
      }

      End:;

      work = null;

      return output;
    }

    #endregion
  }

  #endregion

  #region ResamplingFilters enum

  public enum ResamplingFilters {

    Box = 0,
    Triangle,
    Hermite,
    Bell,
    CubicBSpline,
    Lanczos3,
    Mitchell,
    Cosine,
    CatmullRom,
    Quadratic,
    QuadraticBSpline,
    CubicConvolution,
    Lanczos8
  }

  #endregion

  #region ResamplingFilter class

  public abstract class ResamplingFilter {

    public static ResamplingFilter Create(ResamplingFilters filter) {

      ResamplingFilter resamplingFilter = null;

      switch (filter) {
        case ResamplingFilters.Box:
          resamplingFilter = new BoxFilter();
          break;
        case ResamplingFilters.Triangle:
          resamplingFilter = new TriangleFilter();
          break;
        case ResamplingFilters.Hermite:
          resamplingFilter = new HermiteFilter();
          break;
        case ResamplingFilters.Bell:
          resamplingFilter = new BellFilter();
          break;
        case ResamplingFilters.CubicBSpline:
          resamplingFilter = new CubicBSplineFilter();
          break;
        case ResamplingFilters.Lanczos3:
          resamplingFilter = new Lanczos3Filter();
          break;
        case ResamplingFilters.Mitchell:
          resamplingFilter = new MitchellFilter();
          break;
        case ResamplingFilters.Cosine:
          resamplingFilter = new CosineFilter();
          break;
        case ResamplingFilters.CatmullRom:
          resamplingFilter = new CatmullRomFilter();
          break;
        case ResamplingFilters.Quadratic:
          resamplingFilter = new QuadraticFilter();
          break;
        case ResamplingFilters.QuadraticBSpline:
          resamplingFilter = new QuadraticBSplineFilter();
          break;
        case ResamplingFilters.CubicConvolution:
          resamplingFilter = new CubicConvolutionFilter();
          break;
        case ResamplingFilters.Lanczos8:
          resamplingFilter = new Lanczos8Filter();
          break;
      }

      return resamplingFilter;
    }

    public double defaultFilterRadius;
    public abstract double GetValue(double x);
  }

  internal class HermiteFilter : ResamplingFilter {

    public HermiteFilter() {

      defaultFilterRadius = 1;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x < 1) return ((2*x - 3)*x*x + 1);
      return 0;
    }
  }

  internal class BoxFilter : ResamplingFilter {

    public BoxFilter() {

      defaultFilterRadius = 0.5;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x <= 0.5) return 1;
      return 0;
    }
  }

  internal class TriangleFilter : ResamplingFilter {

    public TriangleFilter() {

      defaultFilterRadius = 1;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x < 1) return (1 - x);
      return 0;
    }
  }

  internal class BellFilter : ResamplingFilter {

    public BellFilter() {

      defaultFilterRadius = 1.5;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x < 0.5) return (0.75 - x*x);
      if (x < 1.5) return (0.5*Math.Pow(x - 1.5, 2));
      return 0;
    }
  }

  internal class CubicBSplineFilter : ResamplingFilter {

    double temp;

    public CubicBSplineFilter() {

      defaultFilterRadius = 2;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x < 1 ) {

        temp = x*x;
        return (0.5*temp*x - temp + 2f/3f);
      }
      if (x < 2) {

        x = 2f - x;
        return (Math.Pow(x, 3)/6f);
      }
      return 0;
    }
  }

  internal class Lanczos3Filter : ResamplingFilter {

    public Lanczos3Filter() {

      defaultFilterRadius = 3;
    }

    double SinC(double x) {

      if (x != 0) {

        x *= Math.PI;
        return (Math.Sin(x)/x);
      }
      return 1;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x < 3) return (SinC(x)*SinC(x/3f));
      return 0;
    }
  }

  internal class MitchellFilter : ResamplingFilter {

    const double C = 1/3;
    double temp;

    public MitchellFilter() {

      defaultFilterRadius = 2;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      temp = x*x;
      if (x < 1) {

        x = (((12 - 9*C - 6*C)*(x*temp)) + ((- 18 + 12*C + 6*C)*temp) + (6 - 2*C));
        return (x/6);
      }
      if (x < 2) {

        x = (((- C - 6*C)*(x*temp)) + ((6*C + 30*C)*temp) + ((- 12*C - 48*C)*x) + (8*C + 24*C));
        return (x/6);
      }
      return 0;
    }
  }

  internal class CosineFilter : ResamplingFilter {

    public CosineFilter() {

      defaultFilterRadius = 1;
    }

    public override double GetValue(double x) {

      if ((x >= -1) && (x <= 1)) return ((Math.Cos(x*Math.PI) + 1)/2f);
      return 0;
    }
  }

  internal class CatmullRomFilter : ResamplingFilter {

    const double C = 1/2;
    double temp;

    public CatmullRomFilter() {

      defaultFilterRadius = 2;
    }

    public override double GetValue(double x) {
      
      if (x < 0) x = - x;
      temp = x*x;
      if (x <= 1) return (1.5*temp*x - 2.5*temp + 1);
      if (x <= 2) return (- 0.5*temp*x + 2.5*temp - 4*x + 2);
      return 0;
    }
  }

  internal class QuadraticFilter : ResamplingFilter {

    public QuadraticFilter() {

      defaultFilterRadius = 1.5;
    }

    public override double GetValue(double x) {
      
      if (x < 0) x = - x;
      if (x <= 0.5) return (- 2*x*x + 1);
      if (x <= 1.5) return (x*x - 2.5*x + 1.5);
      return 0;
    }
  }

  internal class QuadraticBSplineFilter : ResamplingFilter {

    public QuadraticBSplineFilter() {

      defaultFilterRadius = 1.5;
    }

    public override double GetValue(double x) {
      
      if (x < 0) x = - x;
      if (x <= 0.5) return (- x*x + 0.75);
      if (x <= 1.5) return (0.5*x*x - 1.5*x + 1.125);
      return 0;
    }
  }

  internal class CubicConvolutionFilter : ResamplingFilter {

    double temp;

    public CubicConvolutionFilter() {

      defaultFilterRadius = 3;
    }

    public override double GetValue(double x) {
      
      if (x < 0) x = - x;
      temp = x*x;
      if (x <= 1) return ((4f/3f)*temp*x - (7f/3f)*temp + 1);
      if (x <= 2) return (- (7f/12f)*temp*x + 3*temp - (59f/12f)*x + 2.5);
      if (x <= 3) return ((1f/12f)*temp*x - (2f/3f)*temp + 1.75*x - 1.5);
      return 0;
    }
  }

  internal class Lanczos8Filter : ResamplingFilter {    

    public Lanczos8Filter() {

      defaultFilterRadius = 8;
    }

    double SinC(double x) {

      if (x != 0) {

        x *= Math.PI;
        return (Math.Sin(x)/x);
      }
      return 1;
    }

    public override double GetValue(double x) {

      if (x < 0) x = - x;
      if (x < 8) return (SinC(x)*SinC(x/8f));
      return 0;
    }
  }

  #endregion
}