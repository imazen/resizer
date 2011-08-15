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

let grad_coef = 0.3

module Energy : sig
  include Seamcarving.ENERGY_COMPUTATION
  val processor : t
end =
struct
  type energy = {
    sobel : Sobel.Energy.energy;
    hist : int array array;
  }

  type t = unit

  let processor = ()

  let compute_hog grad i j w h =
    let max_grad = ref min_int in
      for p = -1 to 1 do
        let y = j + p in
          if y >= 0 && y < h then
            for q = -1 to 1 do
              let x = i + q in
                if x >= 0 && x < w then
                  let c = grad.(y).(x) in
                    if c > !max_grad then max_grad := c
            done
      done;
      grad.(j).(i) / (int_of_float ((float !max_grad) ** grad_coef) + 1)

  let compute_hog_matrix grad =
    let h = Array.length grad in
    let w = Array.length grad.(0) in
    let dst = Array.create_matrix h w 0 in
      for j = 0 to h - 1 do
        for i = 0 to w - 1 do
          dst.(j).(i) <- compute_hog grad i j w h
        done
      done;
      dst

  let compute_energy () img =
    let sobel = Sobel.Energy.compute_energy Sobel.Energy.processor img in
    let hist = compute_hog_matrix (Sobel.Energy.extract_energy_matrix sobel) in
      { sobel = sobel; hist = hist }

  let extract_energy_matrix e = e.hist

  let update_energy_h e img path =
    let w = img.Seamcarving.width in
    let h = img.Seamcarving.height in
    let dst = e.hist in
      Sobel.Energy.update_energy_h e.sobel img path;
      let grad = Sobel.Energy.extract_energy_matrix e.sobel in
        for j = 0 to h - 1 do
          for i = max (path.(j) - 2) 1 to min (path.(j) + 2) (w - 2) do
            dst.(j).(i) <- compute_hog grad i j w h;
          done;
          for i = path.(j) + 2 to w - 2 do
            dst.(j).(i) <- dst.(j).(i + 1)
          done
        done
end
