open System
open System.Text.RegularExpressions
open System.Threading.Tasks

open Mscc.GenerativeAI

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
// GeminiAgent: интеграция с Mscc.GenerativeAI
// ---------------------------
module GeminiAgent =
    open System
    open System.Text
    open System.Text.RegularExpressions
    open System.Threading.Tasks
    open Mscc.GenerativeAI
    open Types

    // Тип-обёртка для модели
    type Agent = { Model: GenerativeModel }

    /// Инициализация клиента/модели.
    /// apiKey: если null -> библиотека может подхватить из GEMINI_API_KEY
    let initModel (apiKey: string option) (modelName: string) =
        let googleAi =
            match apiKey with
            | Some key -> GoogleAI(apiKey = key)
            | None -> GoogleAI() // библиотека сама читает GEMINI_API_KEY

        let model = googleAi.GenerativeModel(model = modelName)
        { Model = model }

    // Вспомогательная: парсим первую строку на предмет [NN/100]
    let private tryParseScoreFromText (text: string) : Option<int> =
        if String.IsNullOrWhiteSpace text then None
        else
            let firstLine =
                text.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
                |> Array.tryFind (fun s -> not (String.IsNullOrWhiteSpace s))

            match firstLine with
            | None -> None
            | Some ln ->
                let m = Regex(@"^\s*\[(\d{1,3})/100\]").Match(ln)
                if m.Success then
                    match Int32.TryParse m.Groups.[1].Value with
                    | true, v -> Some v
                    | _ -> None
                else None

    // Прямой вызов GenerateContentAsync вместо рефлексии
    let private sendChatMessageAsync (agent: Agent) (message: string) =
        async {
            try
                // Используем прямой вызов асинхронного метода
                let! response = agent.Model.GenerateContent(message) |> Async.AwaitTask
                // Свойство для получения текста ответа называется "Text"
                let responseText = response.Text
                return (responseText, agent) // Возвращаем текст и неизменённый агент
            with ex ->
                // Возвращаем диагностическое сообщение в случае ошибки API
                let errorMsg = sprintf "__API_ERROR__: %s" ex.Message
                return (errorMsg, agent)
        }


    /// Протокол подтверждения — интерактивный с отладочным выводом
    let ensureConfidence (agent: Agent) (systemInstruction: string) (data: string) =
        async {
            Console.WriteLine("⌛ Решаем судьбу...")
            // helper: формируем полный prompt для отправки на модель
            let makePrompt (prevAssistantOpt: Option<string>) (userJustificationOpt: Option<string>) =
                let sb = System.Text.StringBuilder()
                sb.AppendLine(systemInstruction) |> ignore
                sb.AppendLine() |> ignore
                match prevAssistantOpt with
                | Some txt -> sb.AppendLine("Previous evaluation reply:") |> ignore; sb.AppendLine(txt) |> ignore
                | None -> ()
                match userJustificationOpt with
                | Some uj when not (String.IsNullOrWhiteSpace uj) ->
                    sb.AppendLine() |> ignore; sb.AppendLine("User justification:") |> ignore; sb.AppendLine(uj) |> ignore
                | _ -> ()
                sb.AppendLine() |> ignore
                sb.AppendLine(data) |> ignore
                sb.ToString()

            // helper: парсим и возвращаем (option<int>, строка для вывода)
            let parseScoreForDisplay (txt: string) =
                match tryParseScoreFromText txt with
                | Some v -> (Some v, sprintf "%d" v)
                | None -> (None, "?")

            // 1) первая (автоматическая) попытка
            let initialPrompt = makePrompt None None
            let! (text1, agent1) = sendChatMessageAsync agent initialPrompt

            // печатаем ответ нейросети
            Console.WriteLine("\n----- Ответ нейросети -----\n")
            Console.WriteLine(text1)
            Console.WriteLine("\n----- Конец ответа -----\n")

            // парсим счёт
            let (score1Opt, score1Str) = parseScoreForDisplay text1

            match score1Opt with
            | Some 100 ->
                Console.WriteLine(sprintf "😎🤙 Нейросеть убеждена: [%s/100] -> вычисление разрешено. ✅✅✅\n\n\n" score1Str)
                return (true, agent1, text1)
            | _ ->
                Console.WriteLine(sprintf "😢 Нейросеть дала балл: [%s/100]\n\n\n" score1Str)
                let mutable mutableAgent = agent1
                let mutable mutablePrevAssistant = Some text1

                let rec aux attemptsLeft =
                    async {
                        if attemptsLeft <= 0 then
                            Console.WriteLine("🤣🤣🤣 Истекло количество попыток.")
                            return (false, mutableAgent, "max attempts exhausted")
                        else
                            Console.Write(sprintf "❗🗣️🔥 Введите своё объяснение/аргументы для пересмотра (пустая строка чтобы сбежать): ")
                            let userLine = Console.ReadLine()
                            if String.IsNullOrWhiteSpace userLine then
                                Console.WriteLine("💀 Пользователь отменил ввод.")
                                return (false, mutableAgent, "user cancelled")
                            else
                                Console.WriteLine("⌛ Надеемся на твою убедительность...")
                                let prompt = makePrompt mutablePrevAssistant (Some userLine)
                                let! (txt, updatedAgent) = sendChatMessageAsync mutableAgent prompt
                                mutableAgent <- updatedAgent
                                mutablePrevAssistant <- Some txt

                                Console.WriteLine("\n----- Ответ нейросети -----\n")
                                Console.WriteLine(txt)
                                Console.WriteLine("\n----- Конец ответа -----\n")

                                let (scoreOpt, scoreStr) = parseScoreForDisplay txt
                                match scoreOpt with
                                | Some 100 ->
                                    Console.WriteLine(sprintf "😮 Нейросеть удалось убедить: [%s/100] -> разрешено. ✅✅✅\n\n\n" scoreStr)
                                    return (true, mutableAgent, txt)
                                | _ ->
                                    Console.WriteLine(sprintf "🤣🤣🤣🙏 Нейросеть всё ещё не убеждена: [%s/100]\n\n\n" scoreStr)
                                    return! aux (attemptsLeft - 1)
                    }
                return! aux 2
        }


// ---------------------------
// UI интеграция: обновлённый REPL с контролем нейросети
// ---------------------------
module UI2 =
    open Types
    open SafeMath
    open ResultExt
    open GeminiAgent

    // Состояние приложения: модель и chat (пригодится для сохранения истории)
    type AppState =
        { Agent : Agent
          EcoMode : bool }

    // Вспомогательная: готовим системный промпт и попытки
    let private buildPromptsFor (cmd: Command) =
        let systemInstruction =
            "System: Твоя задача это определять, насколько рационально и необходимо пользователю выполнять вычисление калькулятором. " +
            "в ПЕРВОЙ строке каждого ответа ты ДОЛЖЕН вывести уверенность в именно таком формате: [<число>/100] где <число> это целое число 0..100. Вычисление происходит только при значении 100." +
            "После первой строки, ты можешь объяснить причину и написать комментарий к запросу. Только первая строка будет парситься." +
            "Guidelines: В вычислении оценки и написании причины и комментария ты должен соблюдать определённый характер - саркастическая, наглая и довольно упёртая программа, которая любит высмеивать действия пользователя и не стесняется шутить про него. Будь предвзятым, не обязательно сразу же разрешать, даже если это действительно сложное вычисление, которое пользователь вероятно не может выполнить самостоятельно. Третья оценка - заключительная и ты выдаёшь ответ, после которого взаимодействие с пользователем прекращается. Активно используй смайлики, никогда не пиши формально и будь всегда на 'ты'. НИКОГДА в своём ответе не пиши результат или шаги вычисления. Не форматируй ответ помимо новых строк или кавычек."

        let data =
            match cmd with
            | Binary(op, a, b) ->
                let opname =
                    match op with
                    | Add -> "add"
                    | Subtract -> "subtract"
                    | Multiply -> "multiply"
                    | Divide -> "divide"
                    | Power -> "power"
                sprintf "Action: %s\nInputs: a = %g, b = %g" opname a b
            | Unary(op, a) ->
                match op with
                | Sqrt -> sprintf "Action: sqrt\nInputs: a = %g" a
                | Sin mode -> sprintf "Action: sin (degrees=%b)\nInputs: a = %g" mode a
                | Cos mode -> sprintf "Action: cos (degrees=%b)\nInputs: a = %g" mode a
                | Tan mode -> sprintf "Action: tan (degrees=%b)\nInputs: a = %g" mode a

        (systemInstruction, data)


    // Основная функция: выполняет политику eco и проверку нейросети
    let ensureConfidenceThenCompute (state: AppState) (cmd: Command) =
        async {
            // сначала (если eco=true) проверяем локальную возможность вычисления.
            // чтобы избежать несогласованных типов (return в одной ветке if), формируем ранний результат как option
            let earlyResult =
                if state.EcoMode then
                    match UI.compute cmd with
                    | Error e -> Some (Choice1Of2 (Error e, state))
                    | Ok _ -> None
                else
                    None

            match earlyResult with
            | Some res -> return res
            | None ->
                // продолжаем: запускаем протокол Gemini
                let (systemInstruction, data) = buildPromptsFor cmd
                let! (allowed, updatedAgent, diagnostic) = ensureConfidence state.Agent systemInstruction data
                // Просто используй updatedAgent. Состояние не меняется, если только не обновить модель.
                let newState = { state with Agent = updatedAgent }

                if allowed then
                    // у нас разрешение — теперь безопасно вычисляем (чисто)
                    match UI.compute cmd with
                    | Ok v -> return Choice2Of2 (Ok v, newState) // разрешено + результат
                    | Error e ->
                        // Теоретически маловероятно: нейросеть дала 100, но локальная функция вернула ошибку
                        return Choice1Of2 (Error (sprintf "ошибка вычисления после одобрения нейросетью: %s" e), newState)
                else
                    // не разрешили — сообщение об ошибке и diagnostic (для отладки)
                    return Choice1Of2 (Error (sprintf "FAIL: нейросеть не одобрила вычисление: %s" diagnostic), newState)
        }

    // Пример: адаптация handleBinary и handleUnary (только ключевые места)
    // Ниже показан пример для одного двоичного режима. Аналогично можно заменить все вызовы в оригинале.
    let handleBinaryWithAgent (state: AppState) (op: Types.BinaryOp) =
        // читаем числа (используем твой promptForFloat из UI)
        let a = UI.promptForFloat "❓ Введите a: "
        let b = UI.promptForFloat "❓ Введите b: "
        // собираем команду
        let cmd = Command.Binary(op, a, b)
        // Теперь запускаем политику: ensureConfidenceThenCompute
        let res = ensureConfidenceThenCompute state cmd |> Async.RunSynchronously
        match res with
        | Choice2Of2 (Ok v, newState) ->
            printfn "✅ Результат: %g" v
            newState
        | Choice2Of2 (Error e, newState) ->
            // не должно случиться, но на всякий случай
            printfn "❌ Ошибка вычисления: %s" e
            newState
        | Choice1Of2 (Error e, newState) ->
            printfn "❌ Операция отменена: %s" e
            newState

    // Рекурсивный repl, теперь с state, и заменой вызовов handleBinary/Unary на версии, принимающие state
    let rec repl (state: AppState) =
        UI.printMenu ()
        let choice = Console.ReadLine()
        match choice with
        | null -> ()
        | ch when ch.Trim() = "0" -> printfn "👋 Наконец-то..."
        | ch ->
            let trimmed = ch.Trim()
            let newState =
                match trimmed with
                | "1" ->
                    handleBinaryWithAgent state Types.Add
                | "2" ->
                    handleBinaryWithAgent state Types.Subtract
                | "3" ->
                    handleBinaryWithAgent state Types.Multiply
                | "4" ->
                    handleBinaryWithAgent state Types.Divide
                | "5" ->
                    handleBinaryWithAgent state Types.Power
                | "6" ->
                    // унарный sqrt
                    let a = UI.promptForFloat "❓ Введите число: "
                    let cmd = Command.Unary(Types.Sqrt, a)
                    let res = ensureConfidenceThenCompute state cmd |> Async.RunSynchronously
                    match res with
                    | Choice2Of2 (Ok v, ns) -> printfn "✅ Результат: %g" v; ns
                    | Choice1Of2 (Error e, ns) -> printfn "❌ Операция отменена: %s" e; ns
                    | Choice2Of2 (Error e, ns) -> printfn "❌ Ошибка: %s" e; ns
                | "7" ->
                    // sin
                    let a = UI.promptForFloat "❓ Введите число: "
                    // спросим режим
                    let rec ask () =
                        Console.Write("❓ Введите режим (d - градусы, r - радианы): ")
                        match Console.ReadLine() with
                        | null -> ask ()
                        | s ->
                            match Input.parseAngleMode s with
                            | Ok b -> b
                            | Error e -> printfn "%s" e; ask ()
                    let mode = ask ()
                    let cmd = Command.Unary(Types.Sin mode, a)
                    let res = ensureConfidenceThenCompute state cmd |> Async.RunSynchronously
                    match res with
                    | Choice2Of2 (Ok v, ns) -> printfn "✅ Результат: %g" v; ns
                    | Choice1Of2 (Error e, ns) -> printfn "❌ Операция отменена: %s" e; ns
                    | Choice2Of2 (Error e, ns) -> printfn "❌ Ошибка: %s" e; ns
                | "8" ->
                    // cos
                    let a = UI.promptForFloat "❓ Введите число: "
                    let rec ask () =
                        Console.Write("❓ Введите режим (d - градусы, r - радианы): ")
                        match Console.ReadLine() with
                        | null -> ask ()
                        | s ->
                            match Input.parseAngleMode s with
                            | Ok b -> b
                            | Error e -> printfn "%s" e; ask ()
                    let mode = ask ()
                    let cmd = Command.Unary(Types.Cos mode, a)
                    let res = ensureConfidenceThenCompute state cmd |> Async.RunSynchronously
                    match res with
                    | Choice2Of2 (Ok v, ns) -> printfn "✅ Результат: %g" v; ns
                    | Choice1Of2 (Error e, ns) -> printfn "❌ Операция отменена: %s" e; ns
                    | Choice2Of2 (Error e, ns) -> printfn "❌ Ошибка: %s" e; ns
                | "9" ->
                    // tan
                    let a = UI.promptForFloat "❓ Введите число: "
                    let rec ask () =
                        Console.Write("❓ Введите режим (d - градусы, r - радианы): ")
                        match Console.ReadLine() with
                        | null -> ask ()
                        | s ->
                            match Input.parseAngleMode s with
                            | Ok b -> b
                            | Error e -> printfn "%s" e; ask ()
                    let mode = ask ()
                    let cmd = Command.Unary(Types.Tan mode, a)
                    let res = ensureConfidenceThenCompute state cmd |> Async.RunSynchronously
                    match res with
                    | Choice2Of2 (Ok v, ns) -> printfn "✅ Результат: %g" v; ns
                    | Choice1Of2 (Error e, ns) -> printfn "❌ Операция отменена: %s" e; ns
                    | Choice2Of2 (Error e, ns) -> printfn "❌ Ошибка: %s" e; ns
                | "" ->
                    printfn "❌ Сложно определиться? А че зашёл сюда тогда? 🤡🤡🤡"
                    state
                | _ ->
                    printfn "❌ А ещё че? 🖕"
                    state

            repl newState

// ---------------------------
// Program entry: создаём агент и запускаем REPL с eco-флагом
// ---------------------------
module Program =
    open GeminiAgent
    open UI2

    [<EntryPoint>]
    let main _argv =
        try
            // Инициализация: используем строковое имя модели
            let modelName = "gemini-2.0-flash" // Или "gemini-flash-latest"
            let agent = initModel None modelName
            let ecoFlag = true
            let state = { Agent = agent; EcoMode = ecoFlag }
            repl state
            0
        with ex ->
            Console.Error.WriteLine("Критическая ошибка приложения: " + ex.Message)
            1