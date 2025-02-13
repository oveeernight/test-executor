module TestExecutor.Utils.Collections

open System.Collections.Generic

module public Seq =
    let public (|Cons|Empty|) s =
        if Seq.isEmpty s then Empty
        else Cons (Seq.head s, Seq.tail s)
        
module public Dict =
    let public getValueOrUpdate (dict : IDictionary<'a, 'b>) key fallback =
        if dict.ContainsKey(key) then dict[key]
        else
            let newVal = fallback()
            // NOTE: 'fallback' action may add 'key' to 'dict'
            if dict.ContainsKey(key) then dict[key]
                else
                    dict.Add(key, newVal)
                    newVal
