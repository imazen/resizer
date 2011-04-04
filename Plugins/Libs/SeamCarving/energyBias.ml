open Seamcarving

module Make(M : Seamcarving.ENERGY_COMPUTATION) : sig
  include Seamcarving.ENERGY_COMPUTATION
  val make : Seamcarving.image -> M.t -> t
end =
struct
  type t = {
    bias_matrix : int array array;
    processor : M.t;
  }

  type energy = {
    bias : int array array;
    unbiased : M.energy;
    biased : int array array;
  }

  let make img proc =
    if img.height = 0 || img.width = 0 then failwith "Empty energy bias map.";
    let bias = Array.make_matrix img.height img.width 0 in
    let src = img.rgb in
      for j = 0 to img.height - 1 do
        let bias_row = bias.(j) in
        let img_row = src.(j) in
          for i = 0 to img.width - 1 do
            let c = img_row.(i) in
              bias_row.(i) <- 20 * (c.Color.g - c.Color.r);
          done
      done;
      { bias_matrix = bias; processor = proc }

  let compute_energy t img =
    if img.height <> Array.length t.bias_matrix ||
       img.width <> Array.length t.bias_matrix.(0) then
      failwith
        ("The dimensions of the image and the energy bias map differ: " ^
         Printf.sprintf "%dx%d vs %dx%d" img.width img.height
           (Array.length t.bias_matrix.(0)) (Array.length t.bias_matrix));
    let unbiased = M.compute_energy t.processor img in
    let edata = M.extract_energy_matrix unbiased in
    let biased = Array.init img.height (fun i -> Array.copy edata.(i)) in
    let bias = t.bias_matrix in
      for j = 0 to img.height - 1 do
        for i = 0 to img.width - 1 do
          biased.(j).(i) <- biased.(j).(i) + bias.(j).(i)
        done
      done;
      { bias = bias; unbiased = unbiased; biased = biased; }

  let extract_energy_matrix t = t.biased

  let update_energy_h e img path =
    M.update_energy_h e.unbiased img path;
    (* remove from bias *)
    for j = 0 to img.height - 1 do
      let row = e.bias.(j) in
        for i = path.(j) to img.width - 2 do
          row.(i) <- row.(i+1)
        done
    done;
    (* rebias area around path *)
    let unbiased = M.extract_energy_matrix e.unbiased in
      for j = 0 to img.height - 1 do
        let unbiased_row = unbiased.(j) in
        let bias_row = e.bias.(j) in
        let dst = e.biased.(j) in
          for i = max 0 (path.(j) - 2) to min (img.width - 1) (path.(j) + 2) do
            dst.(i) <- unbiased_row.(i) + bias_row.(i)
          done
      done
end
