
Copyright (C) 2007 Mauricio Fernandez <mfp@acm.org>

This is a simple content-aware image resizer as described in Shai Avidan,
Ariel Shamir, "Seam Carving for Content-Aware Image Resizing" ACM Transactions
on Graphics, Volume 26, Number 3,  SIGGRAPH 2007.

There's nothing particularly impressive about this implementation, and
basically no claims to fame. There's however some value in it because it is
quite small, readable and fast. Indeed, it is over 6 times faster than the
GIMP LiquidRescale Plug-in on my machine.

Building
--------
You'll need the camlimages library; on Debian, 
  apt-get install libcamlimages-ocaml-dev
will do.

If you have ocamlbuild (shipped with ocaml 3.10), just run 
 sh build.sh

You can alternatively use omake, simply:
 omake

If for some reason the above fails, use something like

    ocamlopt -o carver -unsafe -I +camlimages \
    graphics.cmxa ci_core.cmxa ci_jpeg.cmxa ci_png.cmxa \ 
    seamcarving.ml sobel.ml gradientHist.ml carve.ml

to compile in one step, or compile the .ml files separately and link the
resulting .cmx files to generate the binary.

License
-------
The program is distributed under the terms of the GNU Library General
Public License version 2.1 (found in LICENSE).

As a special exception to the GNU Lesser General Public License, you may link,
statically or dynamically, a "work that uses the Library" with a publicly
distributed version of the Library to produce an executable file containing
portions of the Library, and distribute that executable file under terms of
your choice, without any of the additional requirements listed in clause 6 of
the GNU Lesser General Public License.  By "a publicly distributed version of
the Library", we mean either the unmodified Library as distributed by the
author, or a modified version of the Library that is distributed under the
conditions defined in clause 2 of the GNU Lesser General Public License.  This
exception does not however invalidate any other reasons why the executable
file might be covered by the GNU Lesser General Public License.

-- Mauricio Fernandez <mfp@acm.org>
