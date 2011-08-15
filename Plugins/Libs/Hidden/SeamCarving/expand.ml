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
  module INS = Seaminsertion.Make(Carving)
  let verbose = ref false

  let time = Cmdline_resizer.time verbose

  let resize_h eproc n dst src =
    let dst = match dst with None -> src ^ ".expanded_h.png" | Some x -> x in
    let img = Seamcarving.load_image src in
    let resizer = INS.make eproc img in
    let expanded = time "Horizontal seam insertion" (INS.insert_seams resizer) n
      in Seamcarving.save_image expanded dst

  let resize_v eproc n dst src =
    let img = Seamcarving.load_image src in
    let img = Seamcarving.rotate_image_cw img in
      let resizer = INS.make eproc img in
      let expanded = time "Vertical seam insertion "
                       (INS.insert_seams resizer) n in
      let expanded = Seamcarving.rotate_image_ccw expanded in
      let dst = match dst with None -> src ^ ".expanded_v.png" | Some x -> x in
        Seamcarving.save_image expanded dst
end

module Conf =
struct
  let desc = "Content-aware rescaling using seam insertion."
  let name = "expand"
end

let () =
  let module C = Cmdline_resizer.Make(Resizer)(Conf) in ()
