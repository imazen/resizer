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

let sobel src =
  let dim1 a = Array.length a in
  let dim2 a = Array.length a.(0) in
  let hgrad_pass srow drow w c =
    for i = 1 to w - 2 do
      drow.(i) <- drow.(i) + c * (srow.(i+1) - srow.(i-1))
    done in

  let w = dim2 src in
  let h = dim1 src in
  let dst = Array.create_matrix h w 0 in
  let tmp = Array.create_matrix h w 0 in

    for j = 0 to h - 1 do
      let drow = dst.(j) in
        if j > 0 then
          hgrad_pass src.(j-1) drow w 1;

        hgrad_pass src.(j) drow w 1;

        if j < h - 1 then
          hgrad_pass src.(j+1) drow w 1;
    done;


    for j = 0 to h - 1 do
      let d = dst.(j) in
        if j > 0 then d.(0) <- d.(0) + src.(j-1).(1);
        d.(0) <- d.(0) + 2 * src.(j).(1);
        if j < h - 1 then d.(0) <- d.(0) + src.(j+1).(1);
    done;

    for j = 1 to h - 2 do
      let r1 = src.(j-1) in
      let r2 = src.(j+1) in
      let drow = tmp.(j) in
        for i = 1 to w - 2 do
          drow.(i) <- drow.(i) + ((r1.(i-1) + 2 * r1.(i) + r1.(i+1)) -
                                  (r2.(i-1) + 2 * r2.(i) + r2.(i+1)))
        done
    done;

    for j = 0 to h - 2 do
      let t = tmp.(j) in
        if j > 0 then t.(0) <- t.(0) + 2 * src.(j-1).(0) + src.(j-1).(1);
        if j < h - 1 then t.(0) <- t.(0) - 2 * src.(j+1).(0) - src.(j+1).(1);
    done;

    for j = 0 to h - 1 do
      let r1 = dst.(j) in
      let r2 = tmp.(j) in
        for i = 0 to w - 1 do
          let x1 = float r1.(i) and x2 = float r2.(i) in
            r1.(i) <- int_of_float (sqrt(x1 *. x1 +. x2 *. x2))
        done
    done;

    dst

module Energy : sig
  include Seamcarving.ENERGY_COMPUTATION
  val processor : t
end =
struct
  type energy = { edata : int array array; }
  type t = unit

  let processor = ()

  let extract_energy_matrix t = t.edata

  let dim1 a = Array.length a
  let dim2 a = Array.length a.(0)

  let compute_energy () img =
    let dst = Array.create_matrix img.Seamcarving.height img.Seamcarving.width 0 in
      for y = 0 to img.Seamcarving.height - 1 do
        let row = dst.(y) in
          for x = 0 to img.Seamcarving.width - 1 do
            row.(x) <- Color.brightness img.Seamcarving.rgb.(y).(x)
          done
      done;
      { edata = sobel dst; }

  let update_energy_h e img path =
    let hmatrix = [| [|-1; 0; 1|]; [|-2; 0; 2|]; [|-1; 0; 1|] |] in
    let vmatrix = [| [|1; 2; 1|]; [|0; 0; 0|]; [|-1; -2; -1|] |] in

    let sobel src i j w h =
      let hgrad = ref 0 in
      let vgrad = ref 0 in
        for p = -1 to 1 do
          let y = j + p in
            if y >= 0 && y < h then
              for q = -1 to 1 do
                let x = i + q in
                  if x >= 0 && x < w then
                    let b = Color.brightness src.(y).(x) in
                      hgrad := !hgrad + hmatrix.(p+1).(q+1) * b;
                      vgrad := !vgrad + vmatrix.(p+1).(q+1) * b;
              done
        done;
        let h = float !hgrad in
        let v = float !vgrad in
          int_of_float (sqrt (h *. h +. v *. v)) in

    let dst = e.edata in
    let w = img.Seamcarving.width in
    let h = img.Seamcarving.height in
    let src = img.Seamcarving.rgb in
      for j = 0 to h - 1 do
        for i = max (path.(j) - 1) 1 to min (path.(j) + 1) (w - 2) do
          let s = sobel src i j w h in
            dst.(j).(i) <- s;
        done;
        for i = path.(j) + 1 to w - 2 do
          dst.(j).(i) <- dst.(j).(i + 1)
        done
      done
end
