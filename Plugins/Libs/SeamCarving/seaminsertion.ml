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

module Make(Carving: Seamcarving.S) =
struct
  type t = {
    carver : Carving.t;
    image : image;
  }

  let make ecomputation img =
    let seam_carver = Carving.make ecomputation (Seamcarving.copy_image img) in
      { carver = seam_carver; image = img }

  let insert_seams t n =
    let rec loop t i acc =
      if i > 0 then begin
        let t, path = Carving.seam_carve_h' t in
          loop t (i - 1) (path :: acc)
      end else Array.of_list (List.rev acc) in

    let paths = loop t.carver n [] in
    let h = t.image.height in
    let w = Array.length paths in
    let m = Array.make_matrix h w 0 in
      (* path matrix, each path is a column *)
      Array.iteri (fun i p ->
                     for j = 0 to h - 1 do
                       m.(j).(i) <- p.(i)
                     done)
        paths;
      (* adjust paths depending on previous ones *)
      for j = 0 to h - 1 do
        let row = m.(j) in
          for i = 0 to w - 2 do
            let rx = row.(i) in
              for k = i + 1 to w - 1 do
                let v = row.(k) in
                  if v >= rx then row.(k) <- v + 2
              done
          done
      done;

      let insert_seam row pos imgwidth =
        Array.blit row pos row (pos+1) (imgwidth - pos);
        if pos > 0 then row.(pos) <- average_color row.(pos-1) row.(pos) in

      let imgwidth = t.image.width in
      let img2 = Seamcarving.enlarge_image t.image n in
        (* insert seams *)
        for j = 0 to h - 1 do
          let srow = m.(j) in
          let row = img2.rgb.(j) in
            for i = 0 to w - 1 do
              insert_seam row srow.(i) (imgwidth + i)
            done
        done;

        { img2 with width = imgwidth + n }
end

