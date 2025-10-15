let add x y = x + y

let subtract x y = x - y

let multiply x y = x * y

let divide x y =
    if y = 0.0 then
        failwith "Деление на ноль недопустимо"
    else
        x / y

let rec factorial n =
    match n with
    | 0 -> 1
    | 1 -> 1
    | _ when n > 1 -> n * factorial (n - 1)
    | _ -> failwith "Факториал определён только для неотрицательных чисел"

let add10 = add 10

let multiplyBy5 = multiply 5



let main argv =
    printfn "Сложение: 5 + 3 = %d" (add 5 3)
    printfn "Вычитание: 5 - 3 = %d" (subtract 5 3)
    printfn "Умножение: 5 * 3 = %d" (multiply 5 3)
    printfn "Деление: 10.0 / 2.0 = %f" (divide 10.0 2.0)
    printfn "Факториал: 5! = %d" (factorial 5)
    printfn "add10 7 = %d" (add10 7)
    printfn "multiplyBy5 4 = %d" (multiplyBy5 4)
    0

main [||]
