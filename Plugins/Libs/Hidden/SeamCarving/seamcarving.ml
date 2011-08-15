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

type image = { width : int; height : int; rgb : Images.rgb array array; }
type path = int array

let load_image file =
  let img = OImages.rgb24 (OImages.load file []) in
  let rgb = Array.init img#height
              (fun _ -> Array.make img#width {Color.r = 0; g = 0; b = 0}) in
    for y = 0 to img#height - 1 do
      for x = 0 to img#width - 1 do
        rgb.(y).(x) <- img#get x y
      done
    done;
    { width = img#width; height = img#height; rgb = rgb }

let copy_image img =
  let rgb' = Array.init img.height (fun i -> Array.copy img.rgb.(i)) in
    { img with rgb = rgb' }

let enlarge_image img n =
  if n < 0 then invalid_arg "enlarge_image: negative increase";
  let oldw = img.width in
  let w = oldw + n in
  let px = {Color.r = 0; g = 0; b = 0} in
  let rgb' = Array.init img.height
               (fun i ->
                  let a = Array.make w px in
                    Array.blit img.rgb.(i) 0 a 0 oldw; a) in
    { img with rgb = rgb' }

let save_image image filename =
  let canvas = Rgb24.create image.width image.height in
  let src = image.rgb in
    for j = 0 to image.height - 1 do
      let row = src.(j) in
        for i = 0 to image.width - 1 do
          Rgb24.set canvas i j row.(i)
        done
    done;
    Images.save filename None [] (Images.Rgb24 canvas)

let rotate_image_cw img =
  let src = img.rgb in
  let w = img.width and h = img.height in
  let rgb = Array.init w
              (fun y -> Array.init h (fun x -> src.(h - x - 1).(y)))
  in { width = img.height; height = img.width; rgb = rgb }

let rotate_image_ccw img =
  let src = img.rgb in
  let w = img.width and h = img.height in
  let rgb = Array.init w
              (fun y -> Array.init h
                          (fun x -> src.(x).(w - y - 1)))
  in { width = img.height; height = img.width; rgb = rgb }

let average_color c1 c2 =
  { Color.r = (c1.Color.r + c2.Color.r) / 2;
    Color.g = (c1.Color.g + c2.Color.g) / 2;
    Color.b = (c1.Color.b + c2.Color.b) / 2; }

module type ENERGY_COMPUTATION =
sig
  type energy
  type t
  val compute_energy : t -> image -> energy
  val extract_energy_matrix : energy -> int array array
  val update_energy_h : energy -> image -> path -> unit
end

module type S =
sig
  type t
  type energy_computation
  val make : energy_computation -> image -> t
  val image : t -> image
  val save_energy : t -> string -> unit
  val seam_carve_h : t -> t
  val seam_carve_h' : t -> t * int array
end

module Make(M : ENERGY_COMPUTATION) : S with type energy_computation = M.t =
struct
  type mono_bitmap = int array array
  type mono_vector = int array

  type t = {
    cost : mono_bitmap;
    energy : M.energy;
    image : image;
  }

  type energy_computation = M.t

  let image t = t.image

  let dim2 b = Array.length b.(0)
  let dim1 b = Array.length b
  let dim b = Array.length b

  let create_mono_bitmap w h = Array.init h (fun _ -> Array.make w 0)

  let matrix_maximum src =
    let rec vect_max (v : mono_vector) i limit max =
      if i < limit then
        vect_max v (i+1) limit (let c = v.(i) in if c > max then c else max)
      else max
    in
      Array.fold_left (fun s x -> vect_max x 0 (Array.length x) s) min_int src

  let normalize_matrix src =
    let max = matrix_maximum src in
      for j = 0 to dim1 src - 1 do
        let row = src.(j) in
          for i = 0 to dim2 src - 1 do
            row.(i) <- 255 * row.(i) / max;
          done
      done

  let blit w h src dst =
    for j = 0 to h - 1 do
      Array.blit src.(j) 0 dst.(j) 0 w
    done

  let save_energy t filename =
    let h = t.image.height in
    let w = t.image.width in
    let e = create_mono_bitmap w h in
    let canvas = Rgb24.create w h in
      blit w h (M.extract_energy_matrix t.energy) e;
      normalize_matrix e;
      for j = 0 to h - 1 do
        let row = e.(j) in
          for i = 0 to w - 1 do
            let c = row.(i) in
              Rgb24.set canvas i j {Color.r = c; g = c; b = c}
          done
      done;
      Images.save filename None [] (Images.Rgb24 canvas)

  let min_index (arr : mono_vector) w =
    let rec loop arr i max x v =
      if i < max then begin
        let v' = arr.(i) in
          if v' < v then loop arr (i+1) max i v'
          else loop arr (i+1) max x v
      end else x
    in
      loop arr 0 w 0 arr.(0)

  let print_path path =
    print_endline
      (String.concat "; "
         (Array.to_list (Array.map string_of_int path)))

  let shortest_path cost w h =
    let int_max (a : int) b = if a > b then a else b in
    let int_min (a : int) b = if a < b then a else b in

    let path = Array.make h 0 in
    let x = ref (min_index cost.(h - 1) w) in
      path.(h-1) <- !x;
      for j = h-2 downto 0 do
        let best = ref max_int in
          for i = int_max 0 (!x - 1) to int_min (w - 1) (!x + 1) do
            let c = cost.(j).(i) in
              if c < !best then begin
                best := c;
                x := i
              end
          done;
          path.(j) <- !x;
      done;
      path

  let make ecomputation img =
    let energy = M.compute_energy ecomputation img in
    let cost = create_mono_bitmap img.width img.height in
      { cost = cost; energy = energy; image = img }

  let update_cost (cost : mono_bitmap) (e : mono_bitmap) w h =
    let int_min (c1 : int) c2 = if c1 < c2 then c1 else c2 in

    let int_min3 (c1 : int) c2 c3 =
      if c1 < c2 then
        if c1 < c3 then c1 else c3
      else
        if c2 < c3 then c2 else c3 in

    let src = e.(0) in
    let dst = cost.(0) in
      Array.blit src 0 dst 0 (Array.length src);
      for y = 1 to h - 1 do
        let prev = cost.(y-1) in
        let cur = cost.(y) in
        let cur_e = e.(y) in
          cur.(0) <- int_min prev.(0) prev.(1) + cur_e.(0);
          for x = 1 to w - 2 do
            let best = int_min3 prev.(x-1) prev.(x) prev.(x+1) in
              cur.(x) <- best + cur_e.(x)
          done;
          cur.(w-1) <- int_min prev.(w-2) prev.(w-1) + cur_e.(w-1);
      done

  let seam_carve_h_aux f t =
    let energy = t.energy in
    let img = t.image in
    let cost = t.cost in
    let w = img.width in
    let h = img.height in
    let e = M.extract_energy_matrix energy in
      if w < 10 then failwith "The image is too small to carve any further seams.";
      update_cost cost e w h;
      let path = shortest_path cost img.width img.height in
        for j = 0 to h - 1 do
          let row = img.rgb.(j) in
          let rx = path.(j) in
          (*  (* slower routine which uses the avg color for the mid pixel *)
            if rx < w - 1 then
              row.(rx) <- average_color row.(rx) row.(rx+1);
            for i = rx + 1 to w - 2 do (* - 2 since one pixel has been removed *)
              row.(i) <- row.(i+1)
            done
           *)
          (* hack to avoid caml_modify: pretend row is an int array so
          * caml_modify isn't used. This is safe as long as no new objects are
          * inserted in row (notably, the average_color thing above would crash
          * this). If anybody else modifies rgb it will bomb. *)
          let unsafe_row = (Obj.magic row : int array) in
            for i = rx to w - 2 do (* - 2 since one pixel has been removed *)
              (* avoid caml_modify *)
              unsafe_row.(i) <- unsafe_row.(i+1)
            done
        done;
        let img' = { img with width = img.width - 1 } in
          M.update_energy_h energy img' path;
          f { t with image = img'; } path

  let seam_carve_h t = seam_carve_h_aux (fun t _ -> t) t

  let seam_carve_h' t = seam_carve_h_aux (fun t path -> (t, path)) t
end
