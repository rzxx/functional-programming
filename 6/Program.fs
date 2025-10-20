open System

// ---------------------------
// Types
// ---------------------------
module Types =

    /// Тип результата вычислений: либо успех с float, либо ошибка с сообщением
    type CalcResult = Result<float, string>

    /// Простые арифметические операции
    type BinaryOp =
        | Add
        | Subtract
        | Multiply
        | Divide
        | Power

    /// Унарные операции
    type UnaryOp =
        | Sqrt
        | Sin of bool // bool = true -> degrees, false -> radians
        | Cos of bool
        | Tan of bool

    /// Общая команда: бинарная или унарная
    type Command =
        | Binary of BinaryOp * float * float
        | Unary of UnaryOp * float

// ---------------------------
// Result helpers (functional combinators)
// ---------------------------
module ResultExt =
    open Types

    let bind (f: 'a -> Result<'b, string>) (r: Result<'a, string>) : Result<'b, string> =
        match r with
        | Ok v -> f v
        | Error e -> Error e

    let map (f: 'a -> 'b) (r: Result<'a, string>) : Result<'b, string> =
        match r with
        | Ok v -> Ok(f v)
        | Error e -> Error e

    let apply (rf: Result<('a -> 'b), string>) (ra: Result<'a, string>) : Result<'b, string> =
        match rf, ra with
        | Ok f, Ok a -> Ok(f a)
        | Error e, _ -> Error e
        | _, Error e -> Error e

    // Map2 для комбинирования двух результатов
    let map2 (f: 'a -> 'b -> 'c) (ra: Result<'a, string>) (rb: Result<'b, string>) : Result<'c, string> =
        match ra, rb with
        | Ok a, Ok b -> Ok(f a b)
        | Error e, _ -> Error e
        | _, Error e -> Error e

// ---------------------------
// Safe Math Module
// ---------------------------
module SafeMath =
    open Types

    let private isFinite (x: float) =
        not (Double.IsNaN x || Double.IsInfinity x)

    // Проверка числового результата — если NaN или Infinity, считаем ошибкой
    let private wrap (x: float) : CalcResult =
        if Double.IsNaN x then
            Error "не знаю как это считать (NaN) 🤔"
        elif Double.IsInfinity x then
            Error "результат слишком велик (8) 🤯"
        else
            Ok x

    // Безопасное сложение
    let add (a: float) (b: float) : CalcResult = wrap (a + b)

    // Безопасное вычитание
    let subtract (a: float) (b: float) : CalcResult = wrap (a - b)

    // Безопасное умножение
    let multiply (a: float) (b: float) : CalcResult = wrap (a * b)

    // Безопасное деление
    let divide (a: float) (b: float) : CalcResult =
        if not (isFinite a) then
            Error "входной делитель некорректен (a не является конченым числом) 🤔"
        elif not (isFinite b) then
            Error "входной делитель некорректен (b не является конченым числом) 🤔"
        elif abs b < Double.Epsilon then
            Error "сам сиди на ноль дели. ❌❌❌🖕🖕🖕"
        else
            wrap (a / b)

    // Безопасное извлечение квадратного корня
    let sqrt (x: float) : CalcResult =
        if not (isFinite x) then
            Error "входное значение не является конечным числом 🤔"
        elif x < 0.0 then
            Error "я не умею... ❌❌❌😭😭😭"
        else
            wrap (Math.Sqrt x)

    // Возведение в степень с проверкой для негативных оснований и дробных показателей
    let pow (baseV: float) (expV: float) : CalcResult =
        if not (isFinite baseV) then
            Error "основание не является конечным числом 🤔"
        elif not (isFinite expV) then
            Error "показатель степени не является конечным числом 🤔"
        else
            // если основание отрицательное и показатель нецелый -> комплексный результат (ошибка для нашей задачи)
            let rounded = Math.Round(expV)
            let isInteger = abs (rounded - expV) < 1e-12

            if baseV < 0.0 && not isInteger then
                Error "отрицательное основание и дробный показатель дают комплексное число. Я не знаю что это. 😮😮😮"
            else if
                // Для целых показателей можно вычислять быстрее и точнее
                isInteger && abs rounded <= 1000000.0
            then
                // если показатель — целое (и не слишком большое по модулю), используем Math.Pow
                wrap (Math.Pow(baseV, expV))
            else
                // общий случай
                let res = Math.Pow(baseV, expV)
                wrap res

    // Преобразователь градусов -> радианы
    let degreesToRadians (deg: float) : float = deg * Math.PI / 180.0

    // Синус/косинус/тангенс с режимом градусов/радиан и проверкой на корректность
    let sin (inDegrees: bool) (x: float) : CalcResult =
        if not (isFinite x) then
            Error "вход не является конечным числом 🤔"
        else
            let r = if inDegrees then degreesToRadians x else x
            wrap (Math.Sin r)

    let cos (inDegrees: bool) (x: float) : CalcResult =
        if not (isFinite x) then
            Error "вход не является конечным числом 🤔"
        else
            let r = if inDegrees then degreesToRadians x else x
            wrap (Math.Cos r)

    let tan (inDegrees: bool) (x: float) : CalcResult =
        if not (isFinite x) then
            Error "вход не является конечным числом 🤔"
        else
            let r = if inDegrees then degreesToRadians x else x
            let c = Math.Cos r
            // если косинус близок к 0 — тангенс не определён (или огромен)
            if abs c < 1e-14 then
                Error "косинус угла слишком близок к нулю — тангенс не определён. хз ответ где-нибудь от 6 до 7 🥱🙄"
            else
                wrap (Math.Tan r)

// ---------------------------
// Parser и валидация ввода
// ---------------------------
module Input =
    open Types
    open ResultExt

    let parseFloat (s: string) : Result<float, string> =
        let sTrim = if isNull s then "" else s.Trim()

        match Double.TryParse(sTrim) with
        | true, v when not (Double.IsNaN v) && not (Double.IsInfinity v) -> Ok v
        | true, _ -> Error(sprintf "число '%s' не является конечным числом 🤔" sTrim)
        | false, _ -> Error(sprintf "вот сам и считай '%s' 🖕🖕🖕" sTrim)

    // Утилита для запроса числа от пользователя (чистая логика отдельно от Console.ReadLine)
    let readNumber (prompt: string) : unit -> Result<float, string> =
        fun () ->
            Console.Write(prompt)
            let line = Console.ReadLine()
            parseFloat line

    // Разбор выбора режима градусов/радиан
    let parseAngleMode (s: string) : Result<bool, string> =
        match if isNull s then "" else s.Trim().ToLowerInvariant() with
        | "d"
        | "deg"
        | "град"
        | "г"
        | "я тупой" -> Ok true
        | "r"
        | "rad"
        | "рад" -> Ok false
        | other when other = "" -> Error "❌ Тебе помочь? Напиши 'я тупой' и я выберу сам."
        | other ->
            Error(
                sprintf
                    "❌ Я не знаю, что такое '%s'. С первого раза не понятно? Пиши 'd' для градусов или 'r' для радиан."
                    other
            )

// ---------------------------
// UI и взаимодействие с пользователем
// ---------------------------
module UI =
    open Types
    open SafeMath
    open ResultExt

    let printMenu () =
        printfn ""
        printfn "==== Калькулятор (функциональный, надёжный 😁👍👍👍) ===="
        printfn "Выберите операцию: 🤔"
        printfn " 1) Сложение (a + b) 😮"
        printfn " 2) Вычитание (a - b) 🥱"
        printfn " 3) Умножение (a * b) 🤑"
        printfn " 4) Деление (a / b) 🫤"
        printfn " 5) Возведение в степень (a ^ b) 🤯"
        printfn " 6) Квадратный корень (sqrt a) 😨"
        printfn " 7) Синус (sin a) 🤑"
        printfn " 8) Косинус (cos a) 💀"
        printfn " 9) Тангенс (tan a) 👽"
        printfn " 0) Выход 👋😭"
        printfn "=============================================="
        Console.Write("❓ Выберите пункт: ")

    // Чистая функция: по команде вычислить результат
    let compute (cmd: Command) : CalcResult =
        match cmd with
        | Binary(Add, a, b) -> add a b
        | Binary(Subtract, a, b) -> subtract a b
        | Binary(Multiply, a, b) -> multiply a b
        | Binary(Divide, a, b) -> divide a b
        | Binary(Power, a, b) -> pow a b
        | Unary(Sqrt, a) -> sqrt a
        | Unary(Sin mode, a) -> sin mode a
        | Unary(Cos mode, a) -> cos mode a
        | Unary(Tan mode, a) -> tan mode a

    // Вспомогательная функция: безопасно запрашивает значение, повторяя до корректного ввода
    let rec promptForFloat (prompt: string) : float =
        Console.Write(prompt)
        let line = Console.ReadLine()

        match Input.parseFloat line with
        | Ok v -> v
        | Error e ->
            printfn "❌ Ошибка ввода: %s" e
            promptForFloat prompt

    // Рекурсивный цикл взаимодействия
    let rec repl () =
        printMenu ()
        let choice = Console.ReadLine()

        match choice with
        | null -> ()
        | ch when ch.Trim() = "0" -> printfn "👋 Наконец-то..."
        | ch ->
            let trimmed = ch.Trim()

            let handleBinary op =
                let a = promptForFloat "❓ Введите a: "
                let b = promptForFloat "❓ Введите b: "
                let res = compute (Binary(op, a, b))

                match res with
                | Ok v -> printfn "✅ Результат: %g" v
                | Error e -> printfn "❌ Ошибка вычисления: %s" e

            let handleUnary op needAngle =
                let a = promptForFloat "❓ Введите число: "

                let mode =
                    if needAngle then
                        // спрашиваем режим
                        let rec ask () =
                            Console.Write("❓ Введите режим (d - градусы, r - радианы): ")

                            match Console.ReadLine() with
                            | null -> ask ()
                            | s ->
                                match Input.parseAngleMode s with
                                | Ok b -> b
                                | Error e ->
                                    printfn "%s" e
                                    ask ()

                        ask ()
                    else
                        false

                let res = compute (Unary(op mode, a))

                match res with
                | Ok v -> printfn "✅ Результат: %g" v
                | Error e -> printfn "❌ Ошибка вычисления: %s" e

            match trimmed with
            | "1" ->
                handleBinary Add
                repl ()
            | "2" ->
                handleBinary Subtract
                repl ()
            | "3" ->
                handleBinary Multiply
                repl ()
            | "4" ->
                handleBinary Divide
                repl ()
            | "5" ->
                handleBinary Power
                repl ()
            | "6" ->
                let a = promptForFloat "❓ Введите число: "
                let res = compute (Unary(Sqrt, a))

                match res with
                | Ok v -> printfn "✅ Результат: %g" v
                | Error e -> printfn "❌ Ошибка вычисления: %s" e

                repl ()
            | "7" ->
                handleUnary (fun b -> Sin b) true
                repl ()
            | "8" ->
                handleUnary (fun b -> Cos b) true
                repl ()
            | "9" ->
                handleUnary (fun b -> Tan b) true
                repl ()
            | "" ->
                printfn "❌ Сложно определиться? А че зашёл сюда тогда? 🤡🤡🤡"
                repl ()
            | _ ->
                printfn "❌ А ещё че? 🖕"
                repl ()

// ---------------------------
// Program entry
// ---------------------------
module Program =
    open UI

    [<EntryPoint>]
    let main _argv =
        // Программа минимально мутирует: только запускает REPL и завершает приложение
        try
            repl ()
            0
        with ex ->
            // На всякий случай ловим непредвидённые исключения и печатаем сообщение — это крайняя защита.
            // Основная логика так устроена, что исключения не должны возникать.
            Console.Error.WriteLine("Критическая ошибка приложения: " + ex.Message)
            1
