open System
open System.ComponentModel
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Markup
open System.Xml

open Calculator

let convert str : 'a = downcast TypeDescriptor.GetConverter(typeof<'a>).ConvertFromString(str)

let BuildCommand handler =
        { new ICommand with
            member c.Execute param = handler (convert (downcast param))
            member c.CanExecute param = true
            member c.add_CanExecuteChanged value = ()
            member c.remove_CanExecuteChanged value = () }

type Ops = OpAdd | OpSub | OpDiv | OpMul | OpEql

type CalcUIController() as self =
    inherit DependencyObject()

    let calculator = new Calculator()
    let display = DependencyProperty.Register("Display", typeof<string>, typeof<CalcUIController>)
    let mutable current_number = 0.0
    let mutable current_op = OpAdd
    let mutable is_decimal = false

    do self.Display <- "0"

    let (|IsInt|_|) (input:string) =
        let output = ref 0
        match Int32.TryParse(input, output) with
        | true -> Some(output.Value)
        | false -> None

    let applyOp() =
        match current_op with
        | OpAdd -> calculator.Processor.Post(Add current_number)
        | OpSub -> calculator.Processor.Post(Subtract current_number)
        | OpMul -> calculator.Processor.Post(Multiply current_number)
        | OpDiv -> calculator.Processor.Post(Divide current_number)
        | OpEql -> ()

    member x.Display with get() = x.GetValue(display) :?> string
                     and private set(value) = x.SetValue(display, value)
    member x.Input = BuildCommand (fun param ->
                        match param with
                        | "." -> is_decimal <- true
                        | IsInt(n) ->
                            current_number <-
                                match is_decimal with
                                | true ->
                                    let num_str = current_number.ToString("R")
                                    if num_str.Contains(".") then num_str else num_str.Insert(num_str.Length, ".")
                                    |> fun str -> str.Insert(str.Length, n.ToString("D"))
                                    |> float
                                | false -> current_number*10.0 + float(n)
                            x.Display <- current_number.ToString("G10")
                        | _ -> failwith "Invalid Input")
    member x.Operation = BuildCommand (fun param ->
                            match param with
                            | "+" | "-" | "*" | "/" | "=" -> applyOp()
                            | "C" -> calculator.Processor.Post(Reset)
                            | _ -> ()
                            current_op <- match param with
                                          | "+" | "C" -> OpAdd
                                          | "-" -> OpSub
                                          | "*" -> OpMul
                                          | "/" -> OpDiv
                                          | "=" -> OpEql
                                          | _ -> current_op // CE
                            if not (param = "=") then current_number <- 0.0
                            is_decimal <- current_number.ToString("R").Contains(".")
                            x.Display <- match param with
                                         | "CE" -> "0"
                                         | _ -> calculator.Processor.PostAndReply(fun r -> Get r).ToString("G10"))

let LoadWindow (path:string) =
    Application.LoadComponent(new Uri(path, UriKind.Relative)) :?> Window

let win = LoadWindow "/Calculator.xaml"
win.DataContext <- new CalcUIController()
win.Show()

#if COMPILED
[<STAThread()>]
do 
    let app = new Application() in
    app.Run() |> ignore
#endif
