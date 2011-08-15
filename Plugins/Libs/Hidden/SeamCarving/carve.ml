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

module Resizer(Carving : Seamcarving.S) =
struct
  let verbose = ref false

  let rec do_carve_h i carve_data =
    if i > 0 then do_carve_h (i-1) (Carving.seam_carve_h carve_data)
    else carve_data

  let time = Cmdline_resizer.time verbose

  let resize_h eproc n dst src =
    let dst = match dst with None -> src ^ ".carved_h.png" | Some x -> x in
    let img = Seamcarving.load_image src in
      if n >= img.width - 10 then failwith "Excessive horizontal downsizing.";
      let carved =
        Carving.image
          (time "Horizontal carving" (do_carve_h n) (Carving.make eproc img))
      in Seamcarving.save_image carved dst

  let resize_v eproc n dst src =
    let img = Seamcarving.load_image src in
    let img = Seamcarving.rotate_image_cw img in
      if n >= img.width - 10 then failwith "Excessive vertical downsizing.";
      let carve_data = Carving.make eproc img in
      let carve_data = time "Vertical carving" (do_carve_h n) carve_data in
      let dst = match dst with None -> src ^ ".carved_v.png" | Some x -> x in
      let carved = Seamcarving.rotate_image_ccw (Carving.image carve_data) in
        Seamcarving.save_image carved dst
end

module Conf =
struct
  let desc = "Content-aware rescaling using seam carving."
  let name = "carve"
end

let () =
  let module C = Cmdline_resizer.Make(Resizer)(Conf) in ()
