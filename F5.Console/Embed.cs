﻿namespace F5.Console
{
  using System;
  using System.Drawing;
  using System.IO;
  using F5.Core.James;

  public static class Embed
  {
    public static void Run(params string[] args)
    {
      if (args.Length < 1)
      {
        StandardUsage();
        return;
      }

      var haveInputImage = false;
      string inFileName = null;
      string outFileName = null;
      string embFileName = null;
      string password = null;
      var comment = string.Empty;
      var quality = 80;
      int i;

      for (i = 0; i < args.Length; i++)
      {
        if (!args[i].StartsWith("-"))
        {
          if (!haveInputImage)
            switch (Path.GetExtension(args[i]))
            {
              case ".jpg":
              case ".tif":
              case ".gif":
              case ".bmp":
              case ".png":
                inFileName = args[i];
                outFileName = Path.GetFileNameWithoutExtension(args[i]) + ".jpg";
                haveInputImage = true;
                break;
              default:
                StandardUsage();
                return;
            }
          else
            outFileName = Path.GetFileNameWithoutExtension(args[i]) + ".jpg";

          continue;
        }

        if (args.Length < i + 1)
        {
          System.Console.WriteLine("Missing parameter for switch " + args[i]);
          StandardUsage();
          return;
        }

        switch (args[i])
        {
          case "-e":
            embFileName = args[i + 1];
            break;
          case "-p":
            password = args[i + 1];
            break;
          case "-q":
            if (!int.TryParse(args[i + 1], out quality))
            {
              StandardUsage();
              return;
            }

            break;
          case "-c":
            comment = args[i + 1];
            break;
          default:
            System.Console.WriteLine("Unknown switch " + args[i] + " ignored.");
            break;
        }

        i++;
      }

      i = 1;
      while (File.Exists(outFileName))
      {
        outFileName = Path.GetFileNameWithoutExtension(outFileName) + i++ + ".jpg";
        if (i > 100) Environment.Exit(0);
      }

      if (!File.Exists(inFileName))
      {
        System.Console.WriteLine("I couldn't find " + inFileName + ". Is it in another directory?");
        return;
      }

      using (var image = Image.FromFile(inFileName))
      using (var jpg = new JpegEncoder(image, File.OpenWrite(outFileName), comment, quality))
      {
        if (embFileName == null)
          jpg.Compress();
        else
          jpg.Compress(File.OpenRead(embFileName), password);
      }
    }

    public static void StandardUsage()
    {
      System.Console.WriteLine("F5/JpegEncoder for .NET(tm)");
      System.Console.WriteLine("");
      System.Console.WriteLine("Program usage: Embed [Options] \"InputImage\".\"ext\" [\"OutputFile\"[.jpg]]");
      System.Console.WriteLine("");
      System.Console.WriteLine("You have the following options:");
      System.Console.WriteLine("-e <file to embed>\tdefault: embed nothing");
      System.Console.WriteLine("-p <password>\t\tdefault: \"abc123\", only used when -e is specified");
      System.Console.WriteLine("-q <quality 0 ... 100>\tdefault: 80");
      System.Console.WriteLine("-c <comment>\t\tdefault: \"JPEG Encoder Copyright 1998, James R. Weeks and BioElectroMech.  \"");
      System.Console.WriteLine("");
      System.Console.WriteLine("\"InputImage\" is the name of an existing image in the current directory.");
      System.Console.WriteLine("  (\"InputImage may specify a directory, too.) \"ext\" must be .tif, .gif,");
      System.Console.WriteLine("  or .jpg.");
      System.Console.WriteLine("Quality is an integer (0 to 100) that specifies how similar the compressed");
      System.Console.WriteLine("  image is to \"InputImage.\"  100 is almost exactly like \"InputImage\" and 0 is");
      System.Console.WriteLine("  most dissimilar.  In most cases, 70 - 80 gives very good results.");
      System.Console.WriteLine("\"OutputFile\" is an optional argument.  If \"OutputFile\" isn't specified, then");
      System.Console.WriteLine("  the input file name is adopted.  This program will NOT write over an existing");
      System.Console.WriteLine("  file.  If a directory is specified for the input image, then \"OutputFile\"");
      System.Console.WriteLine("  will be written in that directory.  The extension \".jpg\" may automatically be");
      System.Console.WriteLine("  added.");
      System.Console.WriteLine("");
      System.Console.WriteLine("Copyright 1998 BioElectroMech and James R. Weeks.  Portions copyright IJG and");
      System.Console.WriteLine("  Florian Raemy, LCAV.  See license.txt for details.");
      System.Console.WriteLine("Visit BioElectroMech at www.obrador.com.  Email James@obrador.com.");
      System.Console.WriteLine("Steganography added by Andreas Westfeld, westfeld@inf.tu-dresden.de");
    }
  }
}
