# C# Implementation of F5 Algorithm for JPEG Steganography

![](steganosaurus.png)

F5 is a steganography algorithm for hiding information in JPEG images.

![](screenshot-ui-web.png)

## Demo
[Steganosaurus](https://trevordarcyevans.github.io/f5-steganography/)

## Prerequisites
* .NET Core 6

## Getting started
```bash
git clone 
cd https://github.com/TrevorDArcyEvans/f5-steganography.git
dotnet restore
dotnet build
cd Steganosaurus/
dotnet run
```
open [Steganosaurus](http://localhost:5270)

## More Information:
* [F5 A Steganographic Algorithm](F5-A-Steganographic-Algorithm.pdf)

```text
F5 is a steganography algo for hiding information in JPEG images.  Unless other implementations
it really hides it inside the image itself (not in metadata/comment fields or appended to the
end of the file).

This project hosts the source code for a Java F5 steganography implementation as open source.

Credits for this go to Andreas Westfeld, it's base on his work.

Copy from original paper:

Abstract.
Many steganographic systems are weak against visual and statistical attacks. Systems without
these weaknesses offer only a relatively small capacity for steganographic messages. The
newly developed algorithm F5 withstands visual and statistical attacks, yet it still offers a
large steganographic capacity. F5 implements matrix encoding to improve the efficiency of
embedding. Thus it reduces the number of nec- essary changes. F5 employs permutative straddling
to uniformly spread out the changes over the whole steganogram.
```

## Java Implementation:
* https://code.google.com/p/f5-steganography/

## Python Implementation:
* https://github.com/jackfengji/f5-steganography/

## Dependencies:
* BouncyCastle
* log4net
* ImageSharp

## Changes from original project
* ported to .NET Core 6
* refactored
* added unit tests
* added web UI (WASM)
* used [ImageSharp](https://github.com/SixLabors/ImageSharp) for graphics operations

## Notes
* WASM performance is *very* slow

## Further Work
* ~~use [ImageSharp](https://github.com/SixLabors/ImageSharp) for graphics operations~~
  * ~~Image.FromStream is not implemented on non-Windows platforms~~
* ~~support Linux~~
* profile code & improve performance
