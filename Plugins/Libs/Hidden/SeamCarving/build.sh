#!/bin/sh

ocamlbuild -lflags -I,+camlimages -cflags -I,+camlimages,-unsafe \
	   -libs graphics,ci_core,ci_jpeg,ci_png carve.native expand.native
