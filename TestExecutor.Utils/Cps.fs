module TestExecutor.Utils.Cps

open TestExecutor.Utils.Collections

let rec foldlk f a xs k =
    match xs with
    | Seq.Empty -> k a
    | Seq.Cons(x, xs') -> f a x (fun a' -> foldlk f a' xs' k)