(*
 * seamcarve, content-aware image resizing using seam carving
 * Copyright (C) 2007 Mauricio Fernandez <mfp@acm.org>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 *)

open Seamcarving

module Energy = Sobel.Energy
module Carving = Make(Energy)
module BiasedEnergy = EnergyBias.Make(Energy)
module BiasedCarving = Make(BiasedEnergy)

module type RESIZER =
  functor (Carving : Seamcarving.S) ->
sig
  val verbose : bool ref
  val resize_h : Carving.energy_computation -> int ->
    string option -> string -> unit
  val resize_v : Carving.energy_computation -> int ->
    string option -> string -> unit
end

let time verbose msg f x =
  let t0 = Sys.time() in
  let ret = f x in
    if !verbose then
      Printf.printf "%s executed in %.2fs." msg (Sys.time() -. t0);
    ret

module Make(Resizer : RESIZER)
  (Conf : sig
     val desc : string
     val name : string
   end) =
struct
  module UnbiasedResizer = Resizer(Carving)
  module BiasedResizer = Resizer(BiasedCarving)

  type mode = Vert of int | Horiz of int | Invalid

  let mode = ref Invalid
  let input_files = ref []
  let output_file = ref None
  let bias_file = ref None

  let verbose = ref false

  let options = [
    "-o", Arg.String (fun o -> output_file := Some o), "Set output file.";
    ("-vert", Arg.Int (fun i -> mode := Vert i),
     "Change height by given amount of pixels.");
    ("-horiz", Arg.Int (fun i -> mode := Horiz i),
     "Change width by given amount of pixels.");
    ("-bias", Arg.String (fun f -> bias_file := Some f),
     "Use energy bias from given file.");
    "-verbose", Arg.Set verbose, "Verbose mode.";
  ]

  let msg =
    Conf.desc ^ "\n" ^ "Usage: " ^ Conf.name ^
    " (-vert <n>| -horiz <n>) [options] <files>"

  let () =
    Arg.parse options (fun fname -> input_files := fname :: !input_files) msg;
    if List.length !input_files = 0 then begin
      Arg.usage options msg;
      exit 1
    end;
    UnbiasedResizer.verbose := !verbose;
    BiasedResizer.verbose := !verbose

  let op = match !mode with
      Vert i -> (match !bias_file with
                     None -> UnbiasedResizer.resize_v Energy.processor i
                   | Some f ->
                       let img = Seamcarving.rotate_image_cw
                                   (Seamcarving.load_image f) in
                       let eproc = BiasedEnergy.make img Energy.processor in
                         BiasedResizer.resize_v eproc i)
    | Horiz i -> (match !bias_file with
                      None -> UnbiasedResizer.resize_h Energy.processor i
                    | Some f ->
                        let img = Seamcarving.load_image f in
                        let eproc = BiasedEnergy.make img Energy.processor in
                          BiasedResizer.resize_h eproc i)
    | Invalid -> print_endline "Either -vert or -horiz needs to be specified.";
                 exit 1

  let () = List.iter (op !output_file) !input_files
end
